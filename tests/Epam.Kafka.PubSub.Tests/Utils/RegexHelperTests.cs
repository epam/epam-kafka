// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Utils;
using Shouldly;
using Xunit;

namespace Epam.Kafka.PubSub.Tests.Utils
{
    public class RegexHelperTests
    {
        [Theory]
        [InlineData("topic1", true)]
        [InlineData("topic-1", true)]
        [InlineData("topic.1", true)]
        [InlineData("topic_1", true)]
        [InlineData("topic@1", false)]
        [InlineData("topic#1", false)]
        public void TopicNameRegex_ShouldMatchExpectedPatterns(string input, bool isMatch)
        {
            var result = RegexHelper.TopicNameRegex.IsMatch(input);
            result.ShouldBe(isMatch);
        }

        [Theory]
        [InlineData("topic1 [0,1]", true)]
        [InlineData("topic-1 [0,1,2]", true)]
        [InlineData("topic.1 [0]", true)]
        [InlineData("topic_1 [0,1]", true)]
        [InlineData("topic1 [ 0 , 100 ]", true)]

        [InlineData("topic@1 [0,1]", false)]
        [InlineData("topic#1 [0,1]", false)]
        [InlineData("topic1 [0,1,]", false)]
        [InlineData("topic1 [0,1,2,]", false)]
        public void TopicPartitionsRegex_ShouldMatchExpectedPatterns(string input, bool isMatch)
        {
            var result = RegexHelper.TopicPartitionsRegex.IsMatch(input);
            result.ShouldBe(isMatch);
        }

        [Theory]
        [InlineData("PubSubName1", true)]
        [InlineData("pubsubname1", true)]
        [InlineData("PubSubName", true)]
        [InlineData("pubsubname", true)]
        [InlineData("1PubSubName", false)]
        [InlineData("PubSubName@", false)]
        [InlineData("PubSubName#", false)]
        public void PunSubNameRegex_ShouldMatchExpectedPatterns(string input, bool isMatch)
        {
            var result = RegexHelper.PunSubNameRegex.IsMatch(input);
            result.ShouldBe(isMatch);
        }
    }
}
