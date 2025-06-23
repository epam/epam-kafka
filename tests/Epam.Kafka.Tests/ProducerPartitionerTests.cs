// Copyright © 2024 EPAM Systems

using Xunit;

namespace Epam.Kafka.Tests
{
    public class ProducerPartitionerTests
    {
        [Fact]
        public void Apply_ShouldThrowArgumentNullException_WhenProducerBuilderIsNull()
        {
            var partitioner = new ProducerPartitioner();
            Assert.Throws<ArgumentNullException>(() => partitioner.Apply<string, string>(null));
        }
    }
}
