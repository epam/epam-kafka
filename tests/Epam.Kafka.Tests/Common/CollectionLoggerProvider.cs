// Copyright © 2024 EPAM Systems

using Microsoft.Extensions.Logging;

namespace Epam.Kafka.Tests.Common;

public sealed class CollectionLoggerProvider : ILoggerProvider
{
    public Dictionary<string, List<string>> Entries { get; } = new();

    public void Dispose()
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        if (!this.Entries.TryGetValue(categoryName, out List<string>? list))
        {
            list = new List<string>();
            this.Entries[categoryName] = list;
        }

        return new Logger(list);
    }

    private class Logger : ILogger
    {
        private readonly List<string> _entries;

        public Logger(List<string> entries)
        {
            this._entries = entries;
        }
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            this._entries.Add(formatter(state, exception));
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