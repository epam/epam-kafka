// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Epam.Kafka.Options;
using Epam.Kafka.PubSub.Subscription;
using Epam.Kafka.PubSub.Tests.Helpers;
using Epam.Kafka.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Epam.Kafka.PubSub.IntegrationTests;

public static class IntegrationTestsExtensions
{
    public static SubscriptionBuilder<string, TestEntityKafka> CreateDefaultSubscription(
        this TestObserver observer, MockCluster mockCluster, 
        AutoOffsetReset autoOffsetReset = AutoOffsetReset.Earliest, 
        PartitionAssignmentStrategy assignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky)
    {
        if (observer == null) throw new ArgumentNullException(nameof(observer));
        if (mockCluster == null) throw new ArgumentNullException(nameof(mockCluster));

        KafkaBuilder kafkaBuilder = mockCluster.LaunchMockCluster(observer.Test);

        // dedicated unique group for each test to avoid Group re-balance in progress exception on parallel test runs.
        kafkaBuilder.WithDefaultConsumer(observer, autoOffsetReset, assignmentStrategy);

        return kafkaBuilder
            .AddSubscription<string, TestEntityKafka, TestSubscriptionHandler>(observer.Name, ServiceLifetime.Scoped)
            .WithOptions(options =>
            {
                options.ExternalStateCommitToKafka = true;
                options.Topics = observer.Test.AnyTopicName;
                options.BatchNotAssignedTimeout = TimeSpan.FromSeconds(1);
                options.BatchEmptyTimeout = TimeSpan.Zero;
                options.PipelineRetryTimeout = TimeSpan.Zero;
                options.BatchPausedTimeout = TimeSpan.Zero;
                options.BatchRetryMaxTimeout = TimeSpan.Zero;
            });
    }

    public static OptionsBuilder<KafkaConsumerOptions> WithDefaultConsumer(
        this KafkaBuilder builder, TestObserver observer, 
        AutoOffsetReset autoOffsetReset = AutoOffsetReset.Earliest,
        PartitionAssignmentStrategy assignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (observer == null) throw new ArgumentNullException(nameof(observer));

        return builder.WithConsumerConfig(MockCluster.DefaultConsumer)
            .Configure(x =>
            {
                x.ConsumerConfig.GroupId = observer.Name;
                x.ConsumerConfig.SessionTimeoutMs = 10_000;
                x.ConsumerConfig.AutoOffsetReset = autoOffsetReset;
                x.ConsumerConfig.SetCancellationDelayMaxMs(2000);
                x.ConsumerConfig.PartitionAssignmentStrategy = assignmentStrategy;
            });
    }
}