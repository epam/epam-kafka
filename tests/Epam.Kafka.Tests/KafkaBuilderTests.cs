// Copyright © 2024 EPAM Systems

using Epam.Kafka.Options;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

using Xunit;

namespace Epam.Kafka.Tests;

public class KafkaBuilderTests
{
    private readonly IServiceCollection _services;
    private readonly KafkaBuilder _builder;

    public KafkaBuilderTests()
    {
        this._services = new ServiceCollection();
        this._builder = new KafkaBuilder(this._services, useConfiguration: false);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenServicesIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new KafkaBuilder(null, true));
    }

    [Fact]
    public void WithClusterConfig_ShouldThrowArgumentNullException_WhenNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => this._builder.WithClusterConfig(null));
    }

    [Fact]
    public void WithClusterConfig_ShouldAddOptions_WhenNameIsValid()
    {
        OptionsBuilder<KafkaClusterOptions> optionsBuilder = this._builder.WithClusterConfig("testCluster");
        optionsBuilder.ShouldNotBeNull();
    }

    [Fact]
    public void WithTestMockCluster_ShouldThrowArgumentNullException_WhenNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => this._builder.WithTestMockCluster(null));
    }

    [Fact]
    public void WithTestMockCluster_ShouldAddOptions_WhenNameIsValid()
    {
        OptionsBuilder<KafkaClusterOptions> optionsBuilder = this._builder.WithTestMockCluster("testCluster");
        optionsBuilder.ShouldNotBeNull();
    }

    [Fact]
    public void WithConsumerConfig_ShouldThrowArgumentNullException_WhenNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => this._builder.WithConsumerConfig(null));
    }

    [Fact]
    public void WithConsumerConfig_ShouldAddOptions_WhenNameIsValid()
    {
        OptionsBuilder<KafkaConsumerOptions> optionsBuilder = this._builder.WithConsumerConfig("testConsumer");
        optionsBuilder.ShouldNotBeNull();
    }

    [Fact]
    public void WithProducerConfig_ShouldThrowArgumentNullException_WhenNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => this._builder.WithProducerConfig(null));
    }

    [Fact]
    public void WithProducerConfig_ShouldAddOptions_WhenNameIsValid()
    {
        OptionsBuilder<KafkaProducerOptions> optionsBuilder = this._builder.WithProducerConfig("testProducer");
        optionsBuilder.ShouldNotBeNull();
    }

    [Fact]
    public void WithDefaults_ShouldConfigureFactoryOptions()
    {
        var configureAction = new Action<KafkaFactoryOptions>(options => options.Cluster = "testCluster");
        this._builder.WithDefaults(configureAction);

        ServiceProvider serviceProvider = this._services.BuildServiceProvider();
        KafkaFactoryOptions options = serviceProvider.GetService<IOptions<KafkaFactoryOptions>>().Value;

        options.Cluster.ShouldBe("testCluster");
    }

    [Theory]
    [InlineData("<qwe", "any", "Placeholder key")]
    [InlineData("qwe>", "any", "Placeholder key")]
    [InlineData("<qwe>", "<any>", "Placeholder value")]
    [InlineData("<qwe>", null, "Placeholder value")]
    [InlineData("<d>", "any", "Duplicate")]
    [InlineData("<D>", "any", "Duplicate")]
    public void PlaceholderArgumentExceptions(string key, string? value, string msg)
    {
        var kafkaBuilder = new KafkaBuilder(new ServiceCollection(), true);

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            kafkaBuilder.WithConfigPlaceholders("<d>", "v").WithConfigPlaceholders(key, value!));
        exception.Message.ShouldContain(msg);
    }
}
