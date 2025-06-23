// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.Tests.Common;

using Microsoft.Extensions.Logging;

using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.PubSub.IntegrationTests;

[Collection(SubscribeTests.Name)]
public class StaticGroupTests : TestWithServices, IClassFixture<MockCluster>
{
    private readonly MockCluster _mockCluster;

    public StaticGroupTests(ITestOutputHelper output, MockCluster mockCluster) : base(output)
    {
        this._mockCluster = mockCluster ?? throw new ArgumentNullException(nameof(mockCluster));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreateConsumer(bool groupInstance)
    {
        this._mockCluster.LaunchMockCluster(this);

        var tp = new TopicPartition(this.AnyTopicName, 0);

        await MockCluster.SeedKafka(this);

        const int millisecondsTimeout = 1000;
        string group = Guid.NewGuid().ToString("N");

        IConsumer<Ignore, Ignore> c1 = this.KafkaFactory.CreateConsumer<Ignore, Ignore>(new ConsumerConfig
        {
            EnablePartitionEof = true,
            ClientId = "c1",
            GroupId = group,
            GroupInstanceId = groupInstance ? "1" : null
        }, MockCluster.ClusterName, this.ConfigureConsumer);

        using IConsumer<Ignore, Ignore> c2 = this.KafkaFactory.CreateConsumer<Ignore, Ignore>(new ConsumerConfig
        {
            EnablePartitionEof = true,
            ClientId = "c2",
            GroupId = group,
            GroupInstanceId = groupInstance ? "2" : null
        }, MockCluster.ClusterName, this.ConfigureConsumer);

        c1.Subscribe(tp.Topic);
        c2.Subscribe(tp.Topic);

        for (int i = 0; i < 10; i++)
        {
            ConsumeResult<Ignore, Ignore> r1 = c1.Consume(millisecondsTimeout);
            ConsumeResult<Ignore, Ignore> r2 = c2.Consume(millisecondsTimeout);

            if (r1 is { IsPartitionEOF: true } && r2 is { IsPartitionEOF: true })
            {
                break;
            }
        }

        c1.Close();
        c1.Dispose();

        using IConsumer<Ignore, Ignore> c1New = this.KafkaFactory.CreateConsumer<Ignore, Ignore>(new ConsumerConfig
        {
            EnablePartitionEof = true,
            ClientId = "c1new",
            GroupId = group,
            GroupInstanceId = groupInstance ? "1" : null
        }, MockCluster.ClusterName, this.ConfigureConsumer);

        c1New.Subscribe(tp.Topic);

        for (int i = 0; i < 10; i++)
        {
            ConsumeResult<Ignore, Ignore> r1 = c1New.Consume(millisecondsTimeout);
            ConsumeResult<Ignore, Ignore> r2 = c2.Consume(millisecondsTimeout);

            if (r1 is { IsPartitionEOF: true } && r2 is { IsPartitionEOF: true })
            {
                break;
            }
        }
    }

    private void ConfigureConsumer(ConsumerBuilder<Ignore, Ignore> builder)
    {
        builder.SetPartitionsAssignedHandler((c, v) =>
            this.Logger.LogInformation("Assigned {Name} {Tp}", c.Name, v));

        builder.SetPartitionsRevokedHandler((c, v) =>
            this.Logger.LogInformation("Revoked {Name} {Tp}", c.Name, v));

        builder.SetPartitionsLostHandler((c, v) =>
            this.Logger.LogInformation("Lost {Name} {Tp}", c.Name, v));
    }
}