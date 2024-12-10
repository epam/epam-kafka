// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Epam.Kafka.PubSub.Common;
using Epam.Kafka.PubSub.Publication;
using Epam.Kafka.PubSub.Subscription.Pipeline;
using Epam.Kafka.PubSub.Utils;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Epam.Kafka.PubSub.Publication.Topics;
using System.Diagnostics;

namespace Epam.Kafka.PubSub.Bridge;

internal sealed class BridgeHandler<TSubKey, TSubValue, TPubKey, TPubValue, TConverter> : ISubscriptionForPublicationHandler<TSubKey, TSubValue>, IDisposable
where TConverter : IConvertHandler<TPubKey, TPubValue, ConsumeResult<TSubKey, TSubValue>>
{
    private readonly Dictionary<string, IPublicationTopicWrapper<TPubKey, TPubValue>> _topics = new();
    public void Execute(IReadOnlyCollection<ConsumeResult<TSubKey, TSubValue>> items,
        IServiceProvider sp,
        SubscriptionMonitor monitor,
        ActivityWrapper activitySpan,
        IConsumerGroupMetadata? metadata,
        CancellationToken cancellationToken)
    {
        PublicationBackgroundService<TPubKey, TPubValue> service = sp.GetServices<IHostedService>()
            .OfType<PublicationBackgroundService<TPubKey, TPubValue>>().Single(x => x.Monitor.Name == monitor.Name);

        IConvertHandler<TPubKey, TPubValue, ConsumeResult<TSubKey, TSubValue>> convertHandler =
            sp.ResolveRequiredService<TConverter>(typeof(TConverter));

        //var handler = new MyHandler(items, convertHandler);
        IPublicationTopicWrapper<TPubKey, TPubValue> topic;
        lock (this._topics)
        {
            if (!this._topics.TryGetValue(monitor.Name, out topic!))
            {
                topic = service.CreateTopicWrapper();
                this._topics.Add(monitor.Name, topic);
            }
        }

        var stopwatch = Stopwatch.StartNew();

        List<TopicMessage<TPubKey, TPubValue>> converted = new();
        foreach (ConsumeResult<TSubKey, TSubValue> item in items)
        {
            converted.AddRange(convertHandler.Convert(item));
        }

        try
        {
            IDictionary<TopicMessage<TPubKey, TPubValue>, DeliveryReport> reports =
                topic.Produce(converted, activitySpan, stopwatch, cancellationToken);

            foreach (DeliveryReport report in reports.Values)
            {
                if (report.Status != PersistenceStatus.Persisted)
                {
                    if (report.Error != null)
                    {
                        throw new KafkaException(report.Error);
                    }

                    throw new KafkaException(new Error(ErrorCode.Local_Fail,
                        $"Report with {report.Status:G} status."));
                }
            }

            topic.CommitTransactionIfNeeded(activitySpan);

        }
#pragma warning disable CA1031
        catch (Exception e1)
        {
            var exceptions = new List<Exception>(3) { e1 };

            e1.DoNotRetryBatch();

            try
            {
                topic.AbortTransactionIfNeeded(activitySpan);
            }
            catch (Exception e2)
            {
                exceptions.Add(e2);
            }

            try
            {
                topic.Dispose();
                lock (this._topics)
                {
                    this._topics.Remove(monitor.Name);
                }
            }
            catch (Exception e3)
            {
                exceptions.Add(e3);
            }

            if (exceptions.Count == 1)
            {
                throw;
            }

            var exception = new AggregateException(exceptions);
            exception.DoNotRetryBatch();

            throw exception;
        }
#pragma warning restore CA1031
    }

    public void Execute(IReadOnlyCollection<ConsumeResult<TSubKey, TSubValue>> items, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    
    public void Dispose()
    {
        foreach (KeyValuePair<string, IPublicationTopicWrapper<TPubKey, TPubValue>> topic in this._topics)
        {
            topic.Value.Dispose();
        }

        this._topics.Clear();
    }
}