// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Publication.Pipeline;
using Epam.Kafka.PubSub.Subscription.Pipeline;
using Epam.Kafka.Tests.Common;

using Shouldly;

using System.Diagnostics;

namespace Epam.Kafka.PubSub.Tests.Helpers;

public sealed class TestObserver : IObserver<KeyValuePair<string, object?>>, IDisposable,
    IObserver<DiagnosticListener>
{
    private readonly LinkedList<KeyValuePair<string, object?>> _activities = new();
    private readonly IDisposable _listeners;

    private int _assertActivityIndex;
    private IDisposable? _src;

    public TestObserver(TestWithServices test, string name, byte maxBatchIterations)
    {
        this.Test = test ?? throw new ArgumentNullException(nameof(test));
        this.Name = name ?? throw new ArgumentNullException(nameof(name));
        this.MaxBatchIterations = maxBatchIterations;
        this._listeners = DiagnosticListener.AllListeners.Subscribe(this);
    }

    public TestObserver(TestWithServices test, byte maxBatchIterations) : this(test,
        "PS" + Guid.NewGuid().ToString("N").Substring(0, 10), maxBatchIterations)
    {
    }

    public TestWithServices Test { get; }

    public string Name { get; }

    public byte MaxBatchIterations { get; }

    public int BatchIteration { get; private set; }

    public void Dispose()
    {
        this._src?.Dispose();
        this._listeners.Dispose();

        this.PrintActivities();
    }

    public void OnNext(DiagnosticListener value)
    {
        if (value.Name == SubscriptionMonitor.BuildFullName(this.Name) ||
            value.Name == PublicationMonitor.BuildFullName(this.Name))
        {
            this._src = value.Subscribe(this);
        }
    }

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(KeyValuePair<string, object?> value)
    {
        this._activities.AddLast(new KeyValuePair<string, object?>(value.Key, value.Value));

        if (value.Key.StartsWith(PublicationMonitor.Prefix, StringComparison.Ordinal) ||
            value.Key.StartsWith(SubscriptionMonitor.Prefix, StringComparison.Ordinal))
        {
            if (value.Key.EndsWith(".Start", StringComparison.Ordinal))
            {
                this.BatchIteration++;
            }

            if (value.Key.EndsWith(".Stop", StringComparison.Ordinal))
            {
                if (this.BatchIteration >= this.MaxBatchIterations)
                {
                    this.Test.Ctc.Cancel();
                }
            }
        }
    }

    private void PrintActivities()
    {
        this.Test.Output.WriteLine(string.Empty);

        foreach (KeyValuePair<string, object?> pair in this._activities)
        {
            if (pair.Value is Exception exception)
            {
                this.Test.Output.WriteLine($"{pair.Key}: {exception.Message}");
            }
            else
            {
                this.Test.Output.WriteLine($"{pair.Key}: {pair.Value}");
            }
        }
    }

    public void AssertStart()
    {
        this._activities.ElementAt(this._assertActivityIndex).Key.ShouldContain("Epam.Kafka.");
        this._activities.ElementAt(this._assertActivityIndex).Key.ShouldContain($".{this.Name}.Start");

        this._assertActivityIndex++;
    }

    public object? AssertStop(object result)
    {
        this._activities.ElementAt(this._assertActivityIndex).Key.ShouldContain("Epam.Kafka.");
        this._activities.ElementAt(this._assertActivityIndex).Key.ShouldContain($".{this.Name}.Stop");

        object? value = this._activities.ElementAt(this._assertActivityIndex).Value;

        if (value is Exception && result is Type type)
        {
            value.ShouldBeOfType(type);
        }
        else
        {
            value.ShouldBe(result);
        }

        this._assertActivityIndex++;

        return value;
    }

    public void AssertNextActivity(string expectedKey)
    {
        this._activities.ElementAt(this._assertActivityIndex).Key.ShouldBe(expectedKey);

        this._assertActivityIndex++;
    }

    public void AssertNextActivity(string expectedKey, object expectedValue)
    {
        this._activities.ElementAt(this._assertActivityIndex).Key.ShouldBe(expectedKey);
        this._activities.ElementAt(this._assertActivityIndex).Value.ShouldBe(expectedValue);

        this._assertActivityIndex++;
    }
}