using System;
using System.Collections.Generic;
using DailySpecial.Domain;
using NUnit.Framework;

public sealed class RelationshipRulesTests
{
    private readonly RelationshipRules rules = new(RelationshipNumbers.Defaults());

    [TestCase(1.0, 12)]
    [TestCase(0.75, 7)]
    [TestCase(0.5, 2)]
    [TestCase(0.4, 0)]
    [TestCase(0.375, -1)]
    [TestCase(0.3, -2)]
    [TestCase(0.15, -5)]
    [TestCase(0.0, -8)]
    public void MatchesGoldenDeltas(double satisfaction, int expectedDelta)
    {
        Relationship before = new(50, new Dictionary<string, int>());

        Relationship after = rules.AfterVisit(before, satisfaction, Array.Empty<string>());

        Assert.That(after.Affinity, Is.EqualTo(50 + expectedDelta));
    }

    [Test]
    public void NeverGoesBelowZero()
    {
        Relationship after = rules.AfterVisit(Relationship.None(), 0.0, Array.Empty<string>());

        Assert.That(after.Affinity, Is.EqualTo(0));
    }

    [Test]
    public void NeverExceedsCeiling()
    {
        Relationship after = rules.AfterVisit(
            new Relationship(95, new Dictionary<string, int>()), 1.0, Array.Empty<string>());

        Assert.That(after.Affinity, Is.EqualTo(100));
    }

    [Test]
    public void CurveFeelsRight()
    {
        Relationship relationship = Relationship.None();
        int familiarOn = 0;
        int regularOn = 0;

        for (int visit = 1; visit <= 12; visit++)
        {
            relationship = rules.AfterVisit(relationship, 1.0, Array.Empty<string>());
            if (familiarOn == 0 && rules.TierOf(relationship) == Tier.Familiar) familiarOn = visit;
            if (regularOn == 0 && rules.TierOf(relationship) == Tier.Regular) regularOn = visit;
        }

        Assert.That(familiarOn, Is.EqualTo(2));
        Assert.That(regularOn, Is.EqualTo(5));
    }

    [Test]
    public void IsDeterministic()
    {
        Relationship before = new(37, new Dictionary<string, int> { { "heat", 1 } });

        Relationship first = rules.AfterVisit(before, 0.62, new[] { "seasoning" });
        Relationship second = rules.AfterVisit(before, 0.62, new[] { "seasoning" });

        Assert.That(second.Affinity, Is.EqualTo(first.Affinity));
        Assert.That(second.AxisHints, Is.EqualTo(first.AxisHints));
    }

    [TestCase(0, Tier.Stranger)]
    [TestCase(19, Tier.Stranger)]
    [TestCase(20, Tier.Familiar)]
    [TestCase(59, Tier.Familiar)]
    [TestCase(60, Tier.Regular)]
    [TestCase(100, Tier.Regular)]
    public void HasExpectedTierBoundaries(int affinity, Tier expectedTier)
    {
        Tier tier = rules.TierOf(new Relationship(affinity, new Dictionary<string, int>()));

        Assert.That(tier, Is.EqualTo(expectedTier));
    }

    [Test]
    public void StrangerDisclosesNothing()
    {
        Disclosure disclosure = rules.Disclose(Relationship.None());

        Assert.That(disclosure.PreferredNeeds, Is.False);
        Assert.That(disclosure.Dietary, Is.False);
        Assert.That(disclosure.AllAxes, Is.False);
        Assert.That(disclosure.RevealedAxes, Is.Empty);
    }

    [Test]
    public void FamiliarDisclosesPreferredNeedsOnly()
    {
        Disclosure disclosure = rules.Disclose(new Relationship(30, new Dictionary<string, int>()));

        Assert.That(disclosure.PreferredNeeds, Is.True);
        Assert.That(disclosure.Dietary, Is.False);
        Assert.That(disclosure.AllAxes, Is.False);
    }

    [Test]
    public void RegularDisclosesEverything()
    {
        Disclosure disclosure = rules.Disclose(new Relationship(60, new Dictionary<string, int>()));

        Assert.That(disclosure.PreferredNeeds, Is.True);
        Assert.That(disclosure.Dietary, Is.True);
        Assert.That(disclosure.AllAxes, Is.True);
        Assert.That(disclosure.RevealsAxis("heat"), Is.True);
    }

    [Test]
    public void ThreeHintsRevealThatAxisToStrangers()
    {
        Relationship relationship = Relationship.None();
        for (int visit = 0; visit < 3; visit++)
        {
            relationship = rules.AfterVisit(relationship, 0.2, new[] { "seasoning" });
        }

        Disclosure disclosure = rules.Disclose(relationship);

        Assert.That(rules.TierOf(relationship), Is.EqualTo(Tier.Stranger));
        Assert.That(disclosure.RevealedAxes, Is.EquivalentTo(new[] { "seasoning" }));
        Assert.That(disclosure.RevealsAxis("heat"), Is.False);
    }

    [Test]
    public void TwoHintsDoNotRevealAnAxis()
    {
        Relationship relationship = Relationship.None();
        for (int visit = 0; visit < 2; visit++)
        {
            relationship = rules.AfterVisit(relationship, 0.5, new[] { "heat" });
        }

        Assert.That(rules.Disclose(relationship).RevealedAxes, Is.Empty);
    }

    [Test]
    public void AccumulatesAxisHints()
    {
        Relationship relationship = rules.AfterVisit(Relationship.None(), 0.5, new[] { "heat", "seasoning" });
        relationship = rules.AfterVisit(relationship, 0.5, new[] { "heat" });

        Assert.That(relationship.HintsFor("heat"), Is.EqualTo(2));
        Assert.That(relationship.HintsFor("seasoning"), Is.EqualTo(1));
        Assert.That(relationship.HintsFor("cook_time"), Is.EqualTo(0));
    }

    [Test]
    public void RejectsInvalidInput()
    {
        Assert.Throws<ArgumentException>(() => new RelationshipRules(null));
        Assert.Throws<ArgumentException>(() => rules.AfterVisit(null, 0.5, Array.Empty<string>()));
        Assert.Throws<ArgumentException>(() => rules.AfterVisit(Relationship.None(), 1.5, Array.Empty<string>()));
        Assert.Throws<ArgumentException>(() => rules.AfterVisit(Relationship.None(), double.NaN, Array.Empty<string>()));
        Assert.Throws<ArgumentException>(() => rules.AfterVisit(Relationship.None(), 0.5, new[] { " " }));
        Assert.Throws<ArgumentException>(() => new Relationship(-1, new Dictionary<string, int>()));
    }
}
