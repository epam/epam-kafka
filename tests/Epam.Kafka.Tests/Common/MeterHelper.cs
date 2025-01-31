// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;
using Xunit.Abstractions;

namespace Epam.Kafka.Tests.Common;

public sealed class MeterHelper : IDisposable
{
    private readonly MeterListener _listener = new ();

    public IDictionary<string, long> Results { get; } = new Dictionary<string, long>();

    public MeterHelper(string meterName)
    {
        this._listener.InstrumentPublished = (instrument, listener) => { listener.EnableMeasurementEvents(instrument); };

        this._listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            if (instrument.Meter.Name != meterName)
            {
                return;
            }

            KeyValuePair<string, object?>[] t = tags.ToArray();

            if (instrument.Meter.Tags != null)
            {
                t = instrument.Meter.Tags.Concat(t).ToArray();
            }

            string ts = string.Join("-", t.Select(x => $"{x.Key}:{x.Value}"));

            string key = $"{instrument.Name}_{ts}";

            this.Results[key] = measurement;
        });

        this._listener.Start();
    }

    public void RecordObservableInstruments(ITestOutputHelper? output = null)
    {
        this.Results.Clear();

        this._listener.RecordObservableInstruments();

        if (output != null)
        {
            this.Print(output);
        }
    }

    public void Print(ITestOutputHelper output)
    {
        if (output == null) throw new ArgumentNullException(nameof(output));

        output.WriteLine(this.Results.Count.ToString("D"));
        foreach (var kvp in this.Results)
        {
            output.WriteLine($"{kvp.Key}: {kvp.Value}");
        }
    }
    public void Dispose()
    {
        this._listener.Dispose();
    }
}