// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Publication.Pipeline;
using Epam.Kafka.PubSub.Subscription.Pipeline;

using Shouldly;

namespace Epam.Kafka.PubSub.Tests.Helpers;

public static class AssertExtensions
{
    public static void AssertSubNotAssigned(this TestObserver observer)
    {
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(0);
        observer.AssertStop(SubscriptionBatchResult.NotAssigned);
    }

    public static void AssertAssign(this TestObserver observer, bool offsetsCommit = false)
    {
        observer.AssertNextActivity("assign.Start");

        if (offsetsCommit)
        {
            observer.AssertCommitKafka();
        }

        observer.AssertNextActivity("assign.Stop");
    }

    public static void AssertRead(this TestObserver observer, int? count = null)
    {
        observer.AssertNextActivity("read.Start");
        if (count.HasValue)
        {
            observer.AssertNextActivity("read.Stop", count.Value);
        }
        else
        {
            observer.AssertNextActivity("read.Stop");
        }
    }

    public static void AssertCommitExternal(this TestObserver observer)
    {
        observer.AssertNextActivity("commit_external.Start");
        observer.AssertNextActivity("commit_external.Stop");
    }

    public static void AssertCommitKafka(this TestObserver observer)
    {
        observer.AssertNextActivity("commit_kafka.Start");
        observer.AssertNextActivity("commit_kafka.Stop");
    }

    public static void AssertProcess(this TestObserver observer)
    {
        observer.AssertNextActivity("process.Start");
        observer.AssertNextActivity("process.Stop");
    }

    public static void AssertSubEmpty(this TestObserver observer, bool offsetsCommit = false)
    {
        observer.AssertStart();
        observer.AssertAssign(offsetsCommit);
        observer.AssertRead(0);
        observer.AssertStop(SubscriptionBatchResult.Empty);
    }

    public static void AssertPubEmpty(this TestObserver observer)
    {
        observer.AssertStart();
        observer.AssertNextActivity("src_read.Start");
        observer.AssertNextActivity("src_read.Stop", 0);
        observer.AssertStop(PublicationBatchResult.Empty);
    }

    public static void AssertStop<TException>(this TestObserver observer, string message)
        where TException : Exception
    {
        var exception = (Exception?)observer.AssertStop(typeof(TException));
        exception!.Message.ShouldContain(message);
    }

    public static void AssertSubPaused(this TestObserver observer)
    {
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(0);
        observer.AssertStop(SubscriptionBatchResult.Paused);
    }
}