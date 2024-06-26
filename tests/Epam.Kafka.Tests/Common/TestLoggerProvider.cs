// Copyright © 2024 EPAM Systems

using Microsoft.Extensions.Logging;

using Xunit.Abstractions;

namespace Epam.Kafka.Tests.Common;

public sealed class TestLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _output;

    public TestLoggerProvider(ITestOutputHelper output)
    {
        this._output = output ?? throw new ArgumentNullException(nameof(output));
    }

    public void Dispose()
    {

    }

    public ILogger CreateLogger(string categoryName)
    {
        return new TestLogger(this._output, categoryName);
    }

    private class TestLogger : ILogger
    {
        private readonly ITestOutputHelper _output;
        private readonly string? _category;

        public TestLogger(ITestOutputHelper output, string? category)
        {
            this._output = output ?? throw new ArgumentNullException(nameof(output));
            this._category = category;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            this._output.WriteLine($"[{DateTime.Now:T}] {this._category} {eventId.Name} ({logLevel:G})");
            this._output.WriteLine($"    {formatter(state, exception)}");

            if (exception != null)
            {
                this._output.WriteLine($"    {exception.GetType()} {exception.Message}");
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }
    }
}