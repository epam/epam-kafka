// Copyright © 2024 EPAM Systems

namespace Epam.Kafka.PubSub.Common.Pipeline;

/// <summary>
///     Representation of status and it's last change time.
/// </summary>
/// <typeparam name="TValue"></typeparam>
public class StatusDetails<TValue> where TValue : Enum
{
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

    internal void Update(TValue value)
    {
        if (!value.Equals(this.Value))
        {
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