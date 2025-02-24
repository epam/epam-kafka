// Copyright © 2024 EPAM Systems

using Epam.Kafka.Internals;
using Shouldly;
using System.Text.RegularExpressions;
using Xunit;

namespace Epam.Kafka.Tests
{
    public class RegexHelperTests
    {
        [Theory]
        [InlineData("<DomainName>", true)]
        [InlineData("<MachineName>", true)]
        [InlineData("<123>", true)]
        [InlineData("<>", false)]
        [InlineData("<Invalid-Name>", false)]
        [InlineData("NoBrackets", false)]
        public void ConfigPlaceholderRegex_ShouldMatchExpectedPatterns(string input, bool isMatch)
        {
            var result = RegexHelper.ConfigPlaceholderRegex.IsMatch(input);
            result.ShouldBe(isMatch);
        }
    }
}

