// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.Tests.Common;

using Shouldly;

using System.Linq;
using System.Xml.Linq;

using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.Tests;

public class KafkaFactoryTests : TestWithServices
{
    public KafkaFactoryTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData(null, MockCluster.DefaultConsumerGroup)]
    [InlineData(MockCluster.DefaultConsumer, MockCluster.DefaultConsumerGroup)]
    [InlineData("c1", "g1")]
    [InlineData("c2", "g2override")]
    [InlineData("c3", "g3")]
    public void CreateConsumerConfig(string? name, string expectedGroup)
    {
        KafkaBuilder kafkaBuilder = MockCluster.AddMockCluster(this);

        // override config using fluent api
        kafkaBuilder.WithConsumerConfig("c2").Configure(x => { x.ConsumerConfig.GroupId = "g2override"; });

        // new config added using fluent api
        kafkaBuilder.WithConsumerConfig("c3").Configure(x => { x.ConsumerConfig.GroupId = "aaa"; });

        //  allow to add multiple times
        kafkaBuilder.WithConsumerConfig("c3").Configure(x => { x.ConsumerConfig.GroupId = "g3"; });

        ConsumerConfig config1 = this.KafkaFactory.CreateConsumerConfig(name);
        Assert.Equal(expectedGroup, config1.GroupId);
        Assert.Single(config1);

        // Each call is guaranteed to return a new instance. 
        // Callers are also free to mutate the returned instance's public properties  as desired.
        config1.GroupId = Guid.NewGuid().ToString("N");

        ConsumerConfig config2 = this.KafkaFactory.CreateConsumerConfig(name);
        Assert.Equal(expectedGroup, config2.GroupId);
        Assert.Single(config2);
    }

    [Theory]
    [InlineData("c4", "group.id is null or whitespace.")]
    [InlineData("c5", "group.id is null or whitespace.")]
    [InlineData(null, "Unable to create consumer with null or whitespace logical name.")]
    [InlineData("", "Unable to create consumer with null or whitespace logical name.")]
    [InlineData(" ", "Unable to create consumer with null or whitespace logical name.")]
    public void CreateConsumerConfigError(string? name, string expectedError)
    {
        KafkaBuilder kafkaBuilder = MockCluster.AddMockCluster(this);

        if (name == null)
        {
            kafkaBuilder.WithDefaults(x => x.Consumer = null);
        }

        // without group id
        kafkaBuilder.WithConsumerConfig("c5");

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() => this.KafkaFactory.CreateConsumerConfig(name));

        (exception.InnerException?.Message ?? exception.Message).ShouldBe(expectedError);
    }

    [Theory]
    [InlineData(null, "t1")]
    [InlineData("p1", "t1")]
    [InlineData("p2", "t2override")]
    [InlineData("p3", "t3")]
    public void CreateProducerConfig(string? name, string expectedGroup)
    {
        KafkaBuilder kafkaBuilder = MockCluster.AddMockCluster(this).WithDefaults(x => x.Producer = "p1");

        // override config using fluent api
        kafkaBuilder.WithProducerConfig("p2").Configure(x => { x.ProducerConfig.TransactionalId = "t2override"; });

        // new config added using fluent api
        kafkaBuilder.WithProducerConfig("p3").Configure(x => { x.ProducerConfig.TransactionalId = "t3"; });

        //  allow to add multiple times
        kafkaBuilder.WithProducerConfig("p3").Configure(x => { x.ProducerConfig.TransactionalId = "t3"; });

        ProducerConfig config1 = this.KafkaFactory.CreateProducerConfig(name);
        Assert.Equal(expectedGroup, config1.TransactionalId);
        Assert.Single(config1);

        // Each call is guaranteed to return a new instance. 
        // Callers are also free to mutate the returned instance's public properties  as desired.
        config1.TransactionalId = Guid.NewGuid().ToString("N");

        ProducerConfig config2 = this.KafkaFactory.CreateProducerConfig(name);
        Assert.Equal(expectedGroup, config2.TransactionalId);
        Assert.Single(config2);
    }

    [Fact]
    public void CreateConfigsWithPlaceholders()
    {
        KafkaBuilder kafkaBuilder = MockCluster.AddMockCluster(this).WithDefaults(x =>
            {
                x.Producer = "placeholder";
                x.Consumer = "placeholder";
            }).WithConfigPlaceholders("<k123>", "qwe");

        ProducerConfig p = this.KafkaFactory.CreateProducerConfig();
        Assert.Equal("qwe qwe <MachineName2>", p.TransactionalId);
        Assert.Single(p);

        ConsumerConfig c = this.KafkaFactory.CreateConsumerConfig();
        Assert.Equal("qwe qwe <machineName2>", c.GroupId);
        Assert.Single(c);
    }

    [Theory]
    [InlineData(null, "Unable to create producer with null or whitespace logical name.")]
    [InlineData("", "Unable to create producer with null or whitespace logical name.")]
    [InlineData(" ", "Unable to create producer with null or whitespace logical name.")]
    public void CreateProducerConfigError(string? name, string expectedError)
    {
        KafkaBuilder kafkaBuilder = MockCluster.AddMockCluster(this);

        if (name == null)
        {
            kafkaBuilder.WithDefaults(x => x.Producer = null);
        }

        // without group id
        kafkaBuilder.WithProducerConfig("c5");

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() => this.KafkaFactory.CreateProducerConfig(name));

        (exception.InnerException?.Message ?? exception.Message).ShouldBe(expectedError);
    }

    [Fact]
    public void CreateDefaultRegistryClientWithCache()
    {
        MockCluster.AddMockCluster(this);

        Confluent.SchemaRegistry.ISchemaRegistryClient sr1 = this.KafkaFactory.GetOrCreateSchemaRegistryClient();
        Confluent.SchemaRegistry.ISchemaRegistryClient sr2 = this.KafkaFactory.GetOrCreateSchemaRegistryClient();

        Assert.Same(sr1, sr2);
    }

    [Fact]
    public void CreateDefaultProducer()
    {
        MockCluster.AddMockCluster(this);

        IProducer<string, string> producer =
            this.KafkaFactory.CreateProducer<string, string>(this.KafkaFactory.CreateProducerConfig());

        Assert.NotNull(producer);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(MockCluster.ClusterName)]
    public void CreateConsumer(string? cluster)
    {
        MockCluster.AddMockCluster(this).WithClusterConfig(MockCluster.ClusterName).Configure(x =>
        {
            x.ClientConfig.SaslMechanism = SaslMechanism.OAuthBearer;
            x.WithOAuthHandler(_ => new("value", DateTime.UtcNow.AddHours(1), string.Empty));
        });

        IConsumer<string, string> consumer = this.KafkaFactory.CreateConsumer<string, string>(
            new ConsumerConfig { GroupId = "any" },
            cluster,
            b => { b.SetOAuthBearerTokenRefreshHandler((_, _) => { }); });

        Assert.NotNull(consumer);
    }

    [Theory]
    [InlineData("", "Unable to create cluster with null or whitespace logical name.")]
    [InlineData("not existing", "bootstrap.servers is null or whitespace.")]
    [InlineData("notValid", "bootstrap.servers is null or whitespace.")]
    public void CreateConsumerError(string cluster, string expectedMessage)
    {
        MockCluster.AddMockCluster(this).WithClusterConfig("notValid");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            this.KafkaFactory.CreateConsumer<string, string>(new ConsumerConfig { GroupId = "any" }, cluster));

        (exception.InnerException?.Message ?? exception.Message).ShouldBe(expectedMessage);
    }

    [Fact]
    public void CreateDefaultClients()
    {
        MockCluster.AddMockCluster(this);

        IClient c1 = this.KafkaFactory.GetOrCreateClient();
        Assert.NotNull(c1);

        IClient c2 = this.KafkaFactory.GetOrCreateClient();
        Assert.NotNull(c2);

        Assert.Same(c1, c2);

        IProducer<string, string> producer = c1.CreateDependentProducer<string, string>();
        Assert.NotNull(producer);

        IAdminClient dc = c1.CreateDependentAdminClient();
        Assert.NotNull(dc);
    }
}