// Copyright © 2024 EPAM Systems

using System.Collections;
using System.Diagnostics;
using System.Text;

namespace Epam.Kafka.PubSub.Utils;
#pragma warning disable CA2000 // StartActivity returns same activity object, so it will be disposed.
internal sealed class ActivityWrapper : IDisposable
{
    private const string Result = "Result";

    private readonly Activity _activity;

    private readonly DiagnosticListener _listener;

    public ActivityWrapper(DiagnosticListener listener, string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        this._listener = listener ?? throw new ArgumentNullException(nameof(listener));

        this._activity = this._listener.StartActivity(new Activity(name), null);
    }

    public void Dispose()
    {
        this._listener.StopActivity(this._activity, this._activity.GetCustomProperty(Result));

        this._activity.Dispose();
    }

    public ActivityWrapper CreateSpan(string name)
    {
        return new ActivityWrapper(this._listener, name);
    }

    public void SetResult(object? value)
    {
        if (this._listener.IsEnabled() && value is IEnumerable enumerable and not string)
        {
            var sb = new StringBuilder(128);

            bool separator = false;

            foreach (object obj in enumerable)
            {
                if (separator)
                {
                    sb.Append(';');
                }

                sb.Append(obj);

                separator = true;
            }

            value = sb.ToString();
        }

        this._activity.SetCustomProperty(Result, value);

        if (value is Exception exception)
        {
            this._activity.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
        }
        else
        {
            string? description = null;

            if (value is Enum en)
            {
                description = en.ToString("G");

                this._activity.SetTag(Result, description);
            }

            this._activity.SetStatus(ActivityStatusCode.Ok, description);
        }
    }
}

#pragma warning restore CA2000