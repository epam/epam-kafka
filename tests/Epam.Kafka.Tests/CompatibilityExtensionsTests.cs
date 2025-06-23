// Copyright © 2024 EPAM Systems

#if !NET6_0_OR_GREATER

using Epam.Kafka.Internals;
using Shouldly;
using System;
using Xunit;

namespace Epam.Kafka.Tests
{
    public class CompatibilityExtensionsTests
    {
        [Fact]
        public void Replace_ShouldThrowArgumentNullException_WhenStrIsNull()
        {
            string str = null;
            Assert.Throws<ArgumentNullException>(() => str.Replace("oldValue", "newValue", StringComparison.Ordinal));
        }

        [Fact]
        public void Replace_ShouldThrowArgumentNullException_WhenOldValueIsNull()
        {
            string str = "test";
            Assert.Throws<ArgumentNullException>(() => str.Replace(null, "newValue", StringComparison.Ordinal));
        }

        [Fact]
        public void Replace_ShouldThrowArgumentException_WhenOldValueIsEmpty()
        {
            string str = "test";
            Assert.Throws<ArgumentException>(() => str.Replace(string.Empty, "newValue", StringComparison.Ordinal));
        }

        [Fact]
        public void Replace_ShouldReturnOriginalString_WhenStrIsEmpty()
        {
            string str = string.Empty;
            var result = str.Replace("oldValue", "newValue", StringComparison.Ordinal);
            result.ShouldBe(str);
        }

        [Theory]
        [InlineData("Hello World", "World", "Universe", "Hello Universe", StringComparison.Ordinal)]
        [InlineData("Hello World", "world", "Universe", "Hello Universe", StringComparison.OrdinalIgnoreCase)]
        [InlineData("Hello World", "world", "Universe", "Hello World", StringComparison.Ordinal)]
        [InlineData("Hello World World", "World", "Universe", "Hello Universe Universe", StringComparison.Ordinal)]
        [InlineData("Hello World", "World", null, "Hello ", StringComparison.Ordinal)]
        [InlineData("Hello World", "World", "", "Hello ", StringComparison.Ordinal)]
        public void Replace_ShouldReplaceOccurrencesCorrectly(string input, string oldValue, string newValue, string expected, StringComparison comparisonType)
        {
            var result = input.Replace(oldValue, newValue, comparisonType);
            result.ShouldBe(expected);
        }
    }
}
#endif


