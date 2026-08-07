using System.Collections.Generic;
using DailySpecial.Domain;
using NUnit.Framework;

namespace DailySpecial.Domain.Tests
{

public sealed class VisitStateGeneratorTests
{
    private readonly VisitStateGenerator generator = new(VisitNumbers.Defaults());

    [TestCase("save-1", 1, "guest_rolf", false, 71, Condition.Tired, Mood.Elated, 24)]
    [TestCase("save-1", 1, "guest_rolf", true, 71, Condition.Tired, Mood.Elated, 8)]
    [TestCase("save-1", 2, "guest_rolf", false, 63, Condition.Normal, Mood.Calm, 13)]
    [TestCase("save-1", 2, "guest_rolf", true, 63, Condition.Normal, Mood.Calm, 19)]
    [TestCase("save-2", 1, "guest_rolf", false, 99, Condition.Normal, Mood.Calm, 8)]
    [TestCase("save-2", 1, "guest_rolf", true, 99, Condition.Normal, Mood.Calm, 10)]
    public void MatchesGoldenVectors(string saveId, int dayNumber, string guestId, bool prefersAffordable,
        int hunger, Condition condition, Mood mood, int wallet)
    {
        VisitState actual = generator.Generate(new VisitSeed(saveId, dayNumber, guestId), TraitsFor(prefersAffordable));
        Assert.AreEqual(new VisitState(hunger, condition, mood, wallet), actual);
    }

    [Test]
    public void SplitMix64UsesSignedFloorMod()
    {
        SplitMix64 random = new(0);
        Assert.AreEqual(-2152535657050944081L, random.NextLong());
        Assert.AreEqual(7960286522194355700L, random.NextLong());
        Assert.AreEqual(487617019471545679L, random.NextLong());

        // 같은 비트 0xFFFFFFFFFFFFFFFF를 signed long -1로 읽어 floorMod 해야 한다.
        Assert.AreEqual(100, SplitMix64.FloorMod(-1, 101));
    }

    [Test]
    public void IsDeterministic()
    {
        VisitSeed seed = new("save-1", 3, "guest_mira");
        VisitState first = generator.Generate(seed, GuestTraits.Of());
        for (int index = 0; index < 100; index++)
        {
            Assert.AreEqual(first, generator.Generate(seed, GuestTraits.Of()), $"{index + 1}번째에 값이 갈렸다");
        }
    }

    [Test]
    public void AffordableOnlyMovesTheWallet()
    {
        for (int day = 1; day <= 50; day++)
        {
            VisitSeed seed = new("save-1", day, "guest_rolf");
            VisitState plain = generator.Generate(seed, GuestTraits.Of());
            VisitState thrifty = generator.Generate(seed, GuestTraits.Of(Need.Affordable));
            Assert.AreEqual(plain.Hunger, thrifty.Hunger, $"{day}일차 허기");
            Assert.AreEqual(plain.Condition, thrifty.Condition, $"{day}일차 컨디션");
            Assert.AreEqual(plain.Mood, thrifty.Mood, $"{day}일차 기분");
        }
    }

    [Test]
    public void StaysWithinConfiguredRanges()
    {
        VisitNumbers numbers = VisitNumbers.Defaults();
        for (int day = 1; day <= 2000; day++)
        {
            VisitState state = generator.Generate(new VisitSeed("save-1", day, "guest_rolf"), GuestTraits.Of());
            Assert.That(state.Hunger, Is.InRange(numbers.HungerMin, numbers.HungerMax));
            Assert.That(state.Wallet, Is.InRange(numbers.WalletMin, numbers.WalletMax));
        }
    }

    [Test]
    public void ThriftyGuestsStayUnderTheLoweredCeiling()
    {
        int ceiling = VisitNumbers.Defaults().AffordableWalletMax;
        for (int day = 1; day <= 2000; day++)
        {
            VisitState state = generator.Generate(new VisitSeed("save-1", day, "guest_rolf"), GuestTraits.Of(Need.Affordable));
            Assert.LessOrEqual(state.Wallet, ceiling, $"저렴 성향인데 지갑이 {state.Wallet}이다");
        }
    }

    [Test]
    public void DistributionFollowsTheWeights()
    {
        const int samples = 20000;
        Dictionary<Condition, int> conditions = new();
        Dictionary<Mood, int> moods = new();
        for (int day = 1; day <= samples; day++)
        {
            VisitState state = generator.Generate(new VisitSeed("save-1", day, "guest_rolf"), GuestTraits.Of());
            conditions[state.Condition] = conditions.GetValueOrDefault(state.Condition) + 1;
            moods[state.Mood] = moods.GetValueOrDefault(state.Mood) + 1;
        }

        AssertShare(conditions, Condition.Normal, samples, 0.70);
        AssertShare(conditions, Condition.Tired, samples, 0.20);
        AssertShare(conditions, Condition.Injured, samples, 0.10);
        AssertShare(moods, Mood.Calm, samples, 0.60);
        AssertShare(moods, Mood.Elated, samples, 0.25);
        AssertShare(moods, Mood.Gloomy, samples, 0.15);
    }

    [Test]
    public void RejectsMissingArguments()
    {
        Assert.Throws<System.ArgumentException>(() => new VisitStateGenerator(null));
        Assert.Throws<System.ArgumentException>(() => generator.Generate(null, GuestTraits.Of()));
        Assert.Throws<System.ArgumentException>(() => generator.Generate(new VisitSeed("save-1", 1, "guest_rolf"), null));
    }

    private static GuestTraits TraitsFor(bool prefersAffordable) => prefersAffordable ? GuestTraits.Of(Need.Affordable) : GuestTraits.Of();

    private static void AssertShare<T>(Dictionary<T, int> counts, T value, int samples, double expected)
    {
        double actual = counts.GetValueOrDefault(value) / (double)samples;
        Assert.LessOrEqual(System.Math.Abs(actual - expected), 0.03, $"{value} 비율이 {actual:F3}인데 기대는 {expected:F2}다");
    }
}
}
