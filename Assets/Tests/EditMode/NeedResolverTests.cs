using System.Collections.Generic;
using DailySpecial.Domain;
using NUnit.Framework;

namespace DailySpecial.Domain.Tests
{

public sealed class NeedResolverTests
{
    private readonly NeedResolver resolver = new(NeedNumbers.Defaults());

    [Test]
    public void HurtOrTiredWantsRestorativeAndMild()
    {
        CollectionAssert.AreEqual(new[] { Need.Restorative, Need.Mild }, resolver.Resolve(Plain(30, Condition.Injured, Mood.Calm, 30), GuestTraits.Of()));
        CollectionAssert.AreEqual(new[] { Need.Restorative, Need.Mild }, resolver.Resolve(Plain(30, Condition.Tired, Mood.Calm, 30), GuestTraits.Of()));
    }

    [Test]
    public void BrokeGuestWantsAffordable()
    {
        CollectionAssert.AreEqual(new[] { Need.Affordable }, resolver.Resolve(Plain(30, Condition.Normal, Mood.Calm, 10), GuestTraits.Of()));
    }

    [Test]
    public void ElatedAndHungryWantsSpecialAndFilling()
    {
        CollectionAssert.AreEqual(new[] { Need.Special, Need.Filling }, resolver.Resolve(Plain(80, Condition.Normal, Mood.Elated, 30), GuestTraits.Of()));
    }

    [Test]
    public void AlwaysBetweenOneAndTwo()
    {
        foreach (Condition condition in System.Enum.GetValues(typeof(Condition)))
        foreach (Mood mood in System.Enum.GetValues(typeof(Mood)))
        foreach (int hunger in new[] { 0, 39, 40, 69, 70, 100 })
        foreach (int wallet in new[] { 8, 16, 17, 24, 25, 40 })
        {
            int count = resolver.Resolve(Plain(hunger, condition, mood, wallet), GuestTraits.Of()).Count;
            Assert.That(count, Is.InRange(1, 2));
        }
    }

    [Test]
    public void FallsBackToTheTopScoreWhenNothingReachesTheThreshold()
    {
        CollectionAssert.AreEqual(new[] { Need.Filling }, resolver.Resolve(Plain(50, Condition.Normal, Mood.Calm, 30), GuestTraits.Of()));
    }

    [Test]
    public void TiesFollowDeclarationOrder()
    {
        CollectionAssert.AreEqual(new[] { Need.Restorative, Need.Mild }, resolver.Resolve(Plain(30, Condition.Tired, Mood.Calm, 30), GuestTraits.Of()));
    }

    [Test]
    public void IsDeterministic()
    {
        VisitState state = Plain(72, Condition.Tired, Mood.Gloomy, 12);
        GuestTraits traits = GuestTraits.Of(Need.Special);
        IReadOnlyList<Need> first = resolver.Resolve(state, traits);
        for (int index = 0; index < 50; index++) CollectionAssert.AreEqual(first, resolver.Resolve(state, traits));
    }

    [Test]
    public void PreferredNeedNudgesWithoutOverriding()
    {
        IReadOnlyList<Need> needs = resolver.Resolve(Plain(30, Condition.Injured, Mood.Calm, 30), GuestTraits.Of(Need.Special));
        Assert.AreEqual(Need.Restorative, needs[0]);
    }

    [Test]
    public void PreferredNeedSeparatesGuestsInTheSameState()
    {
        VisitState state = Plain(50, Condition.Normal, Mood.Calm, 30);
        CollectionAssert.AreEqual(new[] { Need.Filling }, resolver.Resolve(state, GuestTraits.Of()));
        CollectionAssert.AreEqual(new[] { Need.Stimulating }, resolver.Resolve(state, GuestTraits.Of(Need.Stimulating)));
    }

    [Test]
    public void ExplainReportsEveryScore()
    {
        IReadOnlyDictionary<Need, int> scores = resolver.Explain(Plain(80, Condition.Normal, Mood.Elated, 30), GuestTraits.Of());
        Assert.AreEqual(3, scores[Need.Special]);
        Assert.AreEqual(2, scores[Need.Filling]);
        Assert.AreEqual(2, scores[Need.Stimulating]);
        Assert.AreEqual(0, scores[Need.Affordable]);
    }

    [Test]
    public void ExplainCoversTheWholeVocabulary()
    {
        IReadOnlyDictionary<Need, int> scores = resolver.Explain(Plain(50, Condition.Normal, Mood.Calm, 30), GuestTraits.Of());
        foreach (Need need in System.Enum.GetValues(typeof(Need))) Assert.IsTrue(scores.ContainsKey(need), $"빠진 욕구: {need}");
    }

    [Test]
    public void RejectsMissingArguments()
    {
        Assert.Throws<System.ArgumentException>(() => new NeedResolver(null));
        Assert.Throws<System.ArgumentException>(() => resolver.Resolve(null, GuestTraits.Of()));
        Assert.Throws<System.ArgumentException>(() => resolver.Resolve(Plain(50, Condition.Normal, Mood.Calm, 30), null));
    }

    [Test]
    public void SlugsRoundTrip()
    {
        foreach (Need need in System.Enum.GetValues(typeof(Need))) Assert.AreEqual(need, NeedCatalog.FromSlug(NeedCatalog.ToSlug(need)));
        Assert.Throws<System.ArgumentException>(() => NeedCatalog.FromSlug("nonexistent"));
    }

    private static VisitState Plain(int hunger, Condition condition, Mood mood, int wallet) => new(hunger, condition, mood, wallet);
}
}
