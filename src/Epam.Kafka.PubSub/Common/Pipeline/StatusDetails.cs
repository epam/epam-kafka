// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;

namespace Epam.Kafka.PubSub.Common.Pipeline;

/// <summary>
///     Representation of status and it's last change time.
/// </summary>
/// <typeparam name="TValue"></typeparam>
public class StatusDetails<TValue> where TValue : Enum
{
    private const string StatusTagName = "Status";

    private Counter<long>? _histogram;
    internal StatusDetails(TValue value)
    {
        this.Value = value;
    }

    /// <summary>
    ///     UTC <see cref="DateTime" /> of last value change.
    /// </summary>
    public DateTime TimestampUtc { get; private set; } = DateTime.UtcNow;

    /// <summary>
    ///     Status value.
    /// </summary>
    public TValue Value { get; private set; }

    internal void SetTimingCounter(Counter<long> value)
    {
        this._histogram = value;
    }

    internal void Update(TValue value)
    {
        if (!value.Equals(this.Value))
        {
            this._histogram?.Add((long)(DateTime.UtcNow - this.TimestampUtc).TotalMilliseconds,
                new KeyValuePair<string, object?>(StatusTagName, this.Value.ToString("G")));

            this.TimestampUtc = DateTime.UtcNow;
        }

        this.Value = value;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{this.Value:G} from {this.TimestampUtc:u}";
    }
}