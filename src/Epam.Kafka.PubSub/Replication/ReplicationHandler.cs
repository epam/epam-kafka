// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Epam.Kafka.PubSub.Publication.Topics;
using Epam.Kafka.PubSub.Publication;
using Epam.Kafka.PubSub.Utils;
using System.Diagnostics;
using Epam.Kafka.PubSub.Subscription;
using Epam.Kafka.PubSub.Subscription.Topics;

namespace Epam.Kafka.PubSub.Replication;

internal class ReplicationHandler<TSubKey, TSubValue, TPubKey, TPubValue> : ISubscriptionHandler<TSubKey, TSubValue>
{
    private readonly ActivityWrapper _activitySpan;
    private readonly SubscriptionTopicWrapper<TSubKey, TSubValue> _subTopic;
    private readonly IPublicationTopicWrapper<TPubKey, TPubValue> _pubTopic;
    private readonly IConvertHandler<TPubKey, TPubValue, ConsumeResult<TSubKey, TSubValue>> _convertHandler;

    public ReplicationHandler(
        ActivityWrapper activitySpan,
        SubscriptionTopicWrapper<TSubKey, TSubValue> subTopic,
        IPublicationTopicWrapper<TPubKey, TPubValue> pubTopic,
        IConvertHandler<TPubKey, TPubValue, ConsumeResult<TSubKey, TSubValue>> convertHandler)
    {
        this._activitySpan = activitySpan ?? throw new ArgumentNullException(nameof(activitySpan));
        this._subTopic = subTopic ?? throw new ArgumentNullException(nameof(subTopic));
        this._pubTopic = pubTopic ?? throw new ArgumentNullException(nameof(pubTopic));
        this._convertHandler = convertHandler ?? throw new ArgumentNullException(nameof(convertHandler));
    }
    public void Execute(
        IReadOnlyCollection<ConsumeResult<TSubKey, TSubValue>> items,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        IReadOnlyCollection<TopicMessage<TPubKey, TPubValue>> converted = this._convertHandler.Convert(items);

        try
        {
            IDictionary<TopicMessage<TPubKey, TPubValue>, DeliveryReport> reports =
                this._pubTopic.Produce(converted, this._activitySpan, stopwatch, this._subTopic.Options.HandlerTimeout, cancellationToken);

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

            this._pubTopic.CommitTransactionIfNeeded(this._activitySpan);

        }
#pragma warning disable CA1031
        catch (Exception e1)
        {
            var exceptions = new List<Exception>(3) { e1 };

            e1.DoNotRetryBatch();

            try
            {
                this._pubTopic.AbortTransactionIfNeeded(this._activitySpan);
            }
            catch (Exception e2)
            {
                exceptions.Add(e2);
            }

            try
            {
                this._pubTopic.Dispose();
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
}