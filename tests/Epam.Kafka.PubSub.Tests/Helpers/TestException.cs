// Copyright © 2024 EPAM Systems

namespace Epam.Kafka.PubSub.Tests.Helpers;

public class TestException : Exception
{
    public TestException()
    {
    }

    public TestException(string message) : base(message)
    {
    }

    public TestException(string message, Exception inner) : base(message, inner)
    {
    }
}