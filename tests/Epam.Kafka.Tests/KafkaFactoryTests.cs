// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.Tests.Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moq;

using Shouldly;

using Xunit;
using Xunit.Abstractions;

using static Confluent.Kafka.ConfigPropertyNames;

namespace Epam.Kafka.Tests;

public class KafkaFactoryTests : TestWithServices
{
    public KafkaFactoryTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void TestMockCluster()
    {
        this.Services.AddKafka().WithTestMockCluster("qwe");

        TestMockCluster mockCluster = this.ServiceProvider.GetRequiredKeyedService<TestMockCluster>("qwe");

        this.Output.WriteLine(mockCluster.BootstrapServers);

        // create empty topic
        mockCluster.SeedTopic("anyName1");

        // create topic and produce message
        mockCluster.SeedTopic("anyName2", new Message<byte[], byte[]?> { Key = Guid.NewGuid().ToByteArray() });

        using IAdminClient adminClient = mockCluster.CreateDependentAdminClient();

        foreach (var t in adminClient.GetMetadata(TimeSpan.FromSeconds(1)).Topics)
        {
            this.Output.WriteLine(t.Topic);
        }
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
    public void CreateOauthConsumerCustom()
    {
        bool invoked = false;

        var kafkaBuilder = MockCluster.AddMockCluster(this, oauth: true);
        kafkaBuilder.WithConsumerConfig("any").Configure(x =>
        {
            x.ConsumerConfig.GroupId = "any";
            x.ConsumerConfig.StatisticsIntervalMs = 5;
        });

        ConsumerConfig config = this.KafkaFactory.CreateConsumerConfig("any");

        var consumer =
            this.KafkaFactory.CreateConsumer<string, string>(config,
                configure: b =>
                    b.SetOAuthBearerTokenRefreshHandler(
                        (_, _) => { invoked = true; }));

        Assert.NotNull(consumer);

        consumer.Consume(1000);

        Assert.True(invoked);
    }

    [Fact]
    public void CreateOauthConsumerDefault()
    {
        bool invoked = false;

        var kafkaBuilder = MockCluster.AddMockCluster(this, oauth: true);
        kafkaBuilder.WithConsumerConfig("any").Configure(x =>
        {
            x.ConsumerConfig.GroupId = "any";
            x.ConsumerConfig.StatisticsIntervalMs = 5;
        });
        kafkaBuilder.WithClusterConfig(MockCluster.ClusterName).Configure(x => x.WithOAuthHandler(_ =>
        {
            invoked = true;
            throw new ArithmeticException();
        }));

        ConsumerConfig config = this.KafkaFactory.CreateConsumerConfig("any");

        using var consumer =
            this.KafkaFactory.CreateConsumer<string, string>(config);

        Assert.NotNull(consumer);

        consumer.Consume(1000);

        Assert.True(invoked);
    }

    [Fact]
    public void CreateOauthConsumerThrow()
    {
        var kafkaBuilder = MockCluster.AddMockCluster(this, oauth: true);
        kafkaBuilder.WithConsumerConfig("any").Configure(x =>
        {
            x.ConsumerConfig.GroupId = "any";
            x.ConsumerConfig.StatisticsIntervalMs = 5;
        });
        kafkaBuilder.WithClusterConfig(MockCluster.ClusterName)
            .Configure(x => x.WithOAuthHandler(_ => throw new ArithmeticException(), true));

        ConsumerConfig config = this.KafkaFactory.CreateConsumerConfig("any");

        Assert.Throws<InvalidOperationException>(() => this.KafkaFactory.CreateConsumer<string, string>(config,
            configure: b =>
                b.SetOAuthBearerTokenRefreshHandler(
                    (_, _) => { })));
    }

    [Fact]
    public void CreateConfigsWithPlaceholders()
    {
        MockCluster.AddMockCluster(this)
            .WithDefaults(x =>
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

    [Fact]
    public void CreateObservableProducer()
    {
        MockCluster.AddMockCluster(this);

        ProducerConfig config = this.KafkaFactory.CreateProducerConfig();

        config.StatisticsIntervalMs = 5;

        using IProducer<string, string> producer =
            this.KafkaFactory.CreateProducer<string, string>(config);

        Assert.NotNull(producer);

        var errorObs = new Mock<IObserver<Error>>();
        var statsObs = new Mock<IObserver<string>>();

        producer.ShouldBeAssignableTo<IObservable<Error>>()!.Subscribe(errorObs.Object);
        producer.ShouldBeAssignableTo<IObservable<string>>()!.Subscribe(statsObs.Object);
    }

    [Fact]
    public void CreateObservableConsumer()
    {
        MockCluster.AddMockCluster(this).WithConsumerConfig("any").Configure(x =>
        {
            x.ConsumerConfig.GroupId = "any";
            x.ConsumerConfig.StatisticsIntervalMs = 5;
        });

        ConsumerConfig config = this.KafkaFactory.CreateConsumerConfig("any");

        using IConsumer<string, string> consumer =
            this.KafkaFactory.CreateConsumer<string, string>(config);

        Assert.NotNull(consumer);

        var errorObs = new Mock<IObserver<Error>>();
        var statsObs = new Mock<IObserver<string>>();

        consumer.ShouldBeAssignableTo<IObservable<Error>>()!.Subscribe(errorObs.Object);
        consumer.ShouldBeAssignableTo<IObservable<string>>()!.Subscribe(statsObs.Object);
    }

    [Fact]
    public void ObservableConsumerErrors()
    {
        MockCluster.AddMockCluster(this).WithConsumerConfig("any").Configure(x =>
        {
            x.ConsumerConfig.GroupId = "any";
            x.ConsumerConfig.StatisticsIntervalMs = 5;
        });

        ConsumerConfig config = this.KafkaFactory.CreateConsumerConfig("any");

        IConsumer<string, string> consumer =
            this.KafkaFactory.CreateConsumer<string, string>(config, null, builder =>
            {
                builder.SetErrorHandler((_, _) => { });
                builder.SetStatisticsHandler((_, _) => { });
            });

        Assert.NotNull(consumer);

        var errorObs = new Mock<IObserver<Error>>();
        var statsObs = new Mock<IObserver<string>>();
        var parsedObs = new Mock<IObserver<Statistics>>();

        Assert.Throws<InvalidOperationException>(() =>
                consumer.ShouldBeAssignableTo<IObservable<Error>>()!.Subscribe(errorObs.Object))
            .Message.ShouldContain("Cannot subscribe to errors because handler was explicitly set");
        Assert.Throws<InvalidOperationException>(() =>
                consumer.ShouldBeAssignableTo<IObservable<string>>()!.Subscribe(statsObs.Object))
            .Message.ShouldContain("Cannot subscribe to statistics because handler was explicitly set");

        consumer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => consumer.ShouldBeAssignableTo<IObservable<Error>>()!.Subscribe(errorObs.Object));
        Assert.Throws<ObjectDisposedException>(() => consumer.ShouldBeAssignableTo<IObservable<string>>()!.Subscribe(statsObs.Object));
        Assert.Throws<ObjectDisposedException>(() => consumer.ShouldBeAssignableTo<IObservable<Statistics>>()!.Subscribe(parsedObs.Object));

        List<TopicPartition> tp = new List<TopicPartition> { new(string.Empty, 0) };
        List<TopicPartitionOffset> tpo = new List<TopicPartitionOffset> { new(tp[0], 0) };

        Assert.Throws<ObjectDisposedException>(() => consumer.Subscription);
        Assert.Throws<ObjectDisposedException>(() => consumer.ConsumerGroupMetadata);
        Assert.Throws<ObjectDisposedException>(() => consumer.Assignment);
        Assert.Throws<ObjectDisposedException>(() => consumer.MemberId);
        Assert.Throws<ObjectDisposedException>(() => consumer.Assign(tp[0]));
        Assert.Throws<ObjectDisposedException>(() => consumer.Assign(tpo[0]));
        Assert.Throws<ObjectDisposedException>(() => consumer.Assign(tp));
        Assert.Throws<ObjectDisposedException>(() => consumer.Assign(tpo));
        Assert.Throws<ObjectDisposedException>(() => consumer.Close());
        Assert.Throws<ObjectDisposedException>(() => consumer.Commit());
        Assert.Throws<ObjectDisposedException>(() => consumer.Commit(tpo));
        Assert.Throws<ObjectDisposedException>(() => consumer.Commit(new ConsumeResult<string, string>()));
        Assert.Throws<ObjectDisposedException>(() => consumer.Committed(TimeSpan.Zero));
        Assert.Throws<ObjectDisposedException>(() => consumer.Committed(tp, TimeSpan.Zero));
        Assert.Throws<ObjectDisposedException>(() => consumer.Consume());
        Assert.Throws<ObjectDisposedException>(() => consumer.Consume(TimeSpan.Zero));
        Assert.Throws<ObjectDisposedException>(() => consumer.Consume(0));
        Assert.Throws<ObjectDisposedException>(() => consumer.GetWatermarkOffsets(tp[0]));
        Assert.Throws<ObjectDisposedException>(() => consumer.IncrementalAssign(tp));
        Assert.Throws<ObjectDisposedException>(() => consumer.IncrementalAssign(tpo));
        Assert.Throws<ObjectDisposedException>(() => consumer.IncrementalUnassign(tp));
        Assert.Throws<ObjectDisposedException>(() => consumer.OffsetsForTimes(null, TimeSpan.Zero));
        Assert.Throws<ObjectDisposedException>(() => consumer.Pause(tp));
        Assert.Throws<ObjectDisposedException>(() => consumer.Position(tp[0]));
        Assert.Throws<ObjectDisposedException>(() => consumer.Resume(tp));
        Assert.Throws<ObjectDisposedException>(() => consumer.Seek(tpo[0]));
        Assert.Throws<ObjectDisposedException>(() => consumer.StoreOffset(tpo[0]));
        Assert.Throws<ObjectDisposedException>(() => consumer.StoreOffset(new ConsumeResult<string, string>()));
    }

    [Fact]
    public void ObservableProducerErrors()
    {
        MockCluster.AddMockCluster(this);

        ProducerConfig config = this.KafkaFactory.CreateProducerConfig();

        config.StatisticsIntervalMs = 5;

        IProducer<string, string> producer =
            this.KafkaFactory.CreateProducer<string, string>(config, null, builder =>
            {
                builder.SetErrorHandler((_, _) => { });
                builder.SetStatisticsHandler((_, _) => { });
            });

        Assert.NotNull(producer);

        var errorObs = new Mock<IObserver<Error>>();
        var statsObs = new Mock<IObserver<string>>();
        var parsedObs = new Mock<IObserver<Statistics>>();

        Assert.Throws<InvalidOperationException>(() =>
                producer.ShouldBeAssignableTo<IObservable<Error>>()!.Subscribe(errorObs.Object))
            .Message.ShouldContain("Cannot subscribe to errors because handler was explicitly set");

        Assert.Throws<InvalidOperationException>(() =>
                producer.ShouldBeAssignableTo<IObservable<string>>()!.Subscribe(statsObs.Object))
            .Message.ShouldContain("Cannot subscribe to statistics because handler was explicitly set");

        producer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => producer.ShouldBeAssignableTo<IObservable<Error>>()!.Subscribe(errorObs.Object));
        Assert.Throws<ObjectDisposedException>(() => producer.ShouldBeAssignableTo<IObservable<string>>()!.Subscribe(statsObs.Object));
        Assert.Throws<ObjectDisposedException>(() => producer.ShouldBeAssignableTo<IObservable<Statistics>>()!.Subscribe(parsedObs.Object));

        Assert.Throws<ObjectDisposedException>(() => producer.Name);
        Assert.Throws<ObjectDisposedException>(() => producer.Handle);
        Assert.Throws<ObjectDisposedException>(() => producer.Poll(TimeSpan.Zero));
        Assert.Throws<ObjectDisposedException>(() => producer.Produce(string.Empty, null));
        Assert.Throws<ObjectDisposedException>(() => producer.Produce(new TopicPartition(string.Empty, 0), null));
        Assert.Throws<ObjectDisposedException>(() => producer.AbortTransaction());
        Assert.Throws<ObjectDisposedException>(() => producer.AbortTransaction(TimeSpan.Zero));
        Assert.Throws<ObjectDisposedException>(() => producer.CommitTransaction());
        Assert.Throws<ObjectDisposedException>(() => producer.CommitTransaction(TimeSpan.Zero));
        Assert.Throws<ObjectDisposedException>(() => producer.BeginTransaction());
        Assert.Throws<ObjectDisposedException>(() => producer.Flush());
        Assert.Throws<ObjectDisposedException>(() => producer.Flush(TimeSpan.Zero));
        Assert.Throws<ObjectDisposedException>(() => producer.SendOffsetsToTransaction(null!, null!, TimeSpan.Zero));
        Assert.Throws<ObjectDisposedException>(() => producer.AddBrokers(null));
        Assert.Throws<ObjectDisposedException>(() => producer.SetSaslCredentials(null, null));
        Assert.Throws<ObjectDisposedException>(() => producer.OAuthBearerSetToken(null, 0, null));
        Assert.Throws<ObjectDisposedException>(() => producer.OAuthBearerSetTokenFailure(null));
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

    [Fact]
    public void CreateDefaultClientWithMetrics()
    {
        MockCluster.AddMockCluster(this).WithClusterConfig(MockCluster.ClusterName)
            .Configure(x => x.ClientConfig.StatisticsIntervalMs = 100);

        using MeterHelper ml = new(Statistics.TopLevelMeterName);
        ml.RecordObservableInstruments();
        ml.RecordObservableInstruments();
        ml.Results.Count.ShouldBe(0);

        using IClient c1 = this.KafkaFactory.GetOrCreateClient();
        Assert.NotNull(c1);
        Task.Delay(200).Wait();
        ml.RecordObservableInstruments(this.Output);

        ml.Results.Count.ShouldBe(2);
    }

    [Fact]
    public void CreateDefaultClientsError()
    {
        MockCluster.AddMockCluster(this).WithProducerConfig("Shared").Configure(x => x.ProducerConfig.TransactionalId = "any");

        Assert.Throws<InvalidOperationException>(() => this.KafkaFactory.GetOrCreateClient()).Message.ShouldContain("Producer config 'Shared' in corrupted state");
    }

    [Fact]
    public void ConfigSecretsInLogError()
    {
        var logger = new CollectionLoggerProvider();
        this.LoggingBuilder.AddProvider(logger);

        MockCluster.AddMockCluster(this);

        var config = new ProducerConfig();
        config.Set("Sasl.OAuthBearer.Client.Secret", "anyValue");

        Assert.Throws<InvalidOperationException>(() => this.KafkaFactory.CreateProducer<string, string>(config));

        logger.Entries.ShouldHaveSingleItem().Value.ShouldHaveSingleItem().ShouldContain("[Sasl.OAuthBearer.Client.Secret, *******]");
    }

    [Fact]
    public void ConfigSecretsInLog()
    {
        var logger = new CollectionLoggerProvider();
        this.LoggingBuilder.AddProvider(logger);

        MockCluster.AddMockCluster(this);

        var config = new ProducerConfig();
        config.SetDotnetLoggerCategory("Qwe");
        config.SaslOauthbearerClientSecret = "anyValue";
        config.Debug = "all";

        using IProducer<string, string> producer = this.KafkaFactory.CreateProducer<string, string>(config);

        logger.Entries["Epam.Kafka.Factory"].ShouldHaveSingleItem().ShouldContain("[sasl.oauthBearer.client.secret, *******]");
        logger.Entries["Qwe"].ShouldNotBeEmpty();
    }
}