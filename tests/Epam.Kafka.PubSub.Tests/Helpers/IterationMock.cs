// Copyright © 2024 EPAM Systems

using Moq;

namespace Epam.Kafka.PubSub.Tests.Helpers;

public abstract class IterationMock<T>
    where T : class
{
    private readonly TestObserver _observer;
    private readonly Dictionary<int, Mock<T>> _iterations = new();

    protected IterationMock(TestObserver observer)
    {
        this._observer = observer ?? throw new ArgumentNullException(nameof(observer));
    }

    protected Mock<T> Mock => this._iterations[this._observer.BatchIteration];

    protected Mock<T> SetupForIteration(int iteration)
    {
        if (iteration > this._observer.MaxBatchIterations)
        {
            throw new ArgumentOutOfRangeException(nameof(iteration), iteration,
                $"Greater than max iteration {this._observer.MaxBatchIterations} from observer");
        }

        if (iteration < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(iteration), iteration, "Less than 1");
        }

        if (!this._iterations.TryGetValue(iteration, out Mock<T>? mock))
        {
            mock = new Mock<T>(MockBehavior.Strict);
            this._iterations.Add(iteration, mock);
        }

        return mock;
    }

    public void Verify(bool noOther = true)
    {
        foreach (Mock<T> mock in this._iterations.Values)
        {
            mock.VerifyAll();
            if (noOther)
            {
                mock.VerifyNoOtherCalls();
            }
        }
    }
}