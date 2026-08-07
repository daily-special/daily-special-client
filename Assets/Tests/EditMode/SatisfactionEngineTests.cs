using System;
using System.Collections.Generic;
using System.Linq;
using DailySpecial.Domain;
using NUnit.Framework;

namespace DailySpecial.Domain.Tests
{

public sealed class SatisfactionEngineTests
{
    private const int Tolerance = 25;
    private const double Floor = 0.15;
    private const double Overrun = 1.5;
    private const double Violation = 0.1;

    [Test]
    public void IsDeterministic()
    {
        Satisfaction first = Evaluate();
        Satisfaction second = Evaluate();
        AssertScoresEqual(first, second);
    }

    [Test]
    public void PerfectServingScoresOne() => Assert.That(Evaluate().Total, Is.EqualTo(1.0).Within(0.0000001));

    [Test]
    public void OneZeroTermZeroesEverything()
    {
        Satisfaction result = Evaluate(dish: Dish(price: 1000));
        Assert.That(result.BudgetScore, Is.EqualTo(0.0));
        Assert.That(result.Total, Is.EqualTo(0.0));
        Assert.That(result.NeedScore, Is.EqualTo(1.0).Within(0.0000001));
    }

    [Test]
    public void CoveringHalfTheNeedsScoresHalf()
    {
        Satisfaction result = Evaluate(dish: Dish(needTags: new[] { "restorative" }));
        Assert.That(result.NeedScore, Is.EqualTo(0.5).Within(0.0000001));
        CollectionAssert.AreEqual(new[] { "mild" }, result.UnmetNeeds);
    }

    [Test]
    public void ExtraTagsAreNotPenalised() => Assert.That(Evaluate(dish: Dish(needTags: new[] { "restorative", "mild", "filling" })).NeedScore, Is.EqualTo(1.0).Within(0.0000001));

    [Test]
    public void MissingEveryNeedFallsBackToFloor()
    {
        Satisfaction result = Evaluate(dish: Dish(needTags: new[] { "filling" }));
        Assert.That(result.NeedScore, Is.EqualTo(Floor).Within(0.0000001));
        CollectionAssert.AreEqual(new[] { "restorative", "mild" }, result.UnmetNeeds);
    }

    [Test]
    public void SingleNeedMissDoesNotZeroTheServing()
    {
        Satisfaction result = Evaluate(state: State(new[] { "restorative" }), dish: Dish(needTags: new[] { "filling" }));
        Assert.That(result.NeedScore, Is.EqualTo(Floor).Within(0.0000001));
        Assert.That(result.Total, Is.GreaterThan(0.0));
    }

    [Test]
    public void InsideIdealRangeScoresOne()
    {
        foreach (int value in new[] { 40, 50, 60 })
        {
            AxisScore heat = Axis(Evaluate(dish: Dish(parameters: Params(heat: value))));
            Assert.That(heat.Score, Is.EqualTo(1.0).Within(0.0000001));
            Assert.That(heat.Distance, Is.EqualTo(0));
            Assert.That(heat.Direction, Is.EqualTo(0));
        }
    }

    [Test]
    public void OutsideIdealRangeDecaysLinearly()
    {
        AxisScore heat = Axis(Evaluate(dish: Dish(parameters: Params(heat: 65))));
        Assert.That(heat.Distance, Is.EqualTo(5));
        Assert.That(heat.Score, Is.EqualTo(1.0 - 5.0 / Tolerance).Within(0.0000001));
    }

    [Test]
    public void BeyondToleranceScoresZero() => Assert.That(Axis(Evaluate(dish: Dish(parameters: Params(heat: 200)))).Score, Is.EqualTo(0.0));

    [Test]
    public void AxesAreAveragedNotMultiplied() => Assert.That(Evaluate(dish: Dish(parameters: Params(65, 65, 65))).TasteScore, Is.EqualTo(0.8).Within(0.0000001));

    [Test]
    public void DirectionTellsWhichWayItMissed()
    {
        Dictionary<string, AxisScore> scores = Evaluate(dish: Dish(parameters: Params(20, 50, 80))).AxisScores.ToDictionary(score => score.Axis);
        Assert.That(scores["heat"].Direction, Is.EqualTo(-1));
        Assert.That(scores["seasoning"].Direction, Is.EqualTo(1));
        Assert.That(scores["cook_time"].Direction, Is.EqualTo(0));
    }

    [Test]
    public void AxisWithoutPreferenceIsNotScored()
    {
        GuestPersona persona = Persona(new Dictionary<string, IdealRange> { { "heat", new IdealRange(40, 60) } });
        Satisfaction result = Evaluate(persona: persona, dish: Dish(parameters: new Dictionary<string, int> { { "heat", 50 }, { "seasoning", 0 } }));
        CollectionAssert.AreEqual(new[] { "heat" }, result.AxisScores.Select(score => score.Axis));
        Assert.That(result.TasteScore, Is.EqualTo(1.0).Within(0.0000001));
    }

    [Test]
    public void MissingParamForPreferredAxisIsAnError()
    {
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => Evaluate(dish: Dish(parameters: new Dictionary<string, int> { { "cook_time", 50 }, { "seasoning", 50 } })));
        StringAssert.Contains("heat", error.Message);
    }

    [Test]
    public void PriceWithinWalletIsFullScore()
    {
        foreach (int price in new[] { 1, 50, 100 })
        {
            Assert.That(Evaluate(dish: Dish(price: price)).BudgetScore, Is.EqualTo(1.0).Within(0.0000001));
        }
    }

    [Test]
    public void PriceOverWalletDecaysInsteadOfCollapsing()
    {
        double score = Evaluate(dish: Dish(price: 101)).BudgetScore;
        Assert.That(score, Is.GreaterThan(0.0).And.LessThan(1.0));
    }

    [Test]
    public void BudgetReachesZeroAtTheOverrunRatio() => Assert.That(Evaluate(dish: Dish(price: (int)(100 * Overrun))).BudgetScore, Is.EqualTo(0.0));

    [Test]
    public void BudgetIsScaleFree()
    {
        double small = Evaluate(state: State(wallet: 100), dish: Dish(price: 125)).BudgetScore;
        double large = Evaluate(state: State(wallet: 10000), dish: Dish(price: 12500)).BudgetScore;
        Assert.That(small, Is.EqualTo(large).Within(0.0000001));
    }

    [Test]
    public void DietaryViolationMultipliesInsteadOfSubtracting()
    {
        Satisfaction result = Evaluate(persona: Persona(dietary: new[] { "no_meat" }), dish: Dish(dietaryConflicts: new[] { "no_meat" }));
        Assert.That(result.DietaryFactor, Is.EqualTo(Violation).Within(0.0000001));
        CollectionAssert.AreEqual(new[] { "no_meat" }, result.ViolatedDietary);
        Assert.That(result.Total, Is.EqualTo(Violation).Within(0.0000001));
    }

    [Test]
    public void TwoViolationsHurtMoreThanOne() => Assert.That(Evaluate(persona: Persona(dietary: new[] { "no_meat", "no_dairy" }), dish: Dish(dietaryConflicts: new[] { "no_meat", "no_dairy" })).DietaryFactor, Is.EqualTo(Violation * Violation).Within(0.0000001));

    [Test]
    public void IrrelevantConflictIsIgnored()
    {
        Satisfaction result = Evaluate(persona: Persona(dietary: new[] { "no_meat" }), dish: Dish(dietaryConflicts: new[] { "no_dairy" }));
        Assert.That(result.DietaryFactor, Is.EqualTo(1.0).Within(0.0000001));
        CollectionAssert.IsEmpty(result.ViolatedDietary);
    }

    [Test]
    public void SatisfactionNeverLeavesZeroToOne()
    {
        Satisfaction worst = Evaluate(persona: Persona(dietary: new[] { "no_meat", "no_dairy" }), dish: Dish(Array.Empty<string>(), 10000, Params(0, 0, 0), new[] { "no_meat", "no_dairy" }));
        Assert.That(worst.Total, Is.InRange(0.0, 1.0));
        Assert.That(Evaluate().Total, Is.InRange(0.0, 1.0));
    }

    [Test]
    public void InvertedIdealRangeIsRejected()
    {
        ArgumentException error = Assert.Throws<ArgumentException>(() => new IdealRange(60, 40));
        StringAssert.Contains("뒤집혔다", error.Message);
    }

    private static Satisfaction Evaluate(GuestPersona persona = null, VisitState state = null, ServedDish dish = null) => new SatisfactionEngine(Numbers()).Evaluate(persona ?? Persona(), state ?? State(), dish ?? Dish());
    private static ScoringNumbers Numbers() => new(Floor, Tolerance, Overrun, Violation, new[] { "heat", "cook_time", "seasoning" });
    private static GuestPersona Persona(IDictionary<string, IdealRange> ranges = null, IEnumerable<string> dietary = null) => new(ranges ?? new Dictionary<string, IdealRange> { { "heat", new IdealRange(40, 60) }, { "cook_time", new IdealRange(40, 60) }, { "seasoning", new IdealRange(40, 60) } }, dietary);
    private static VisitState State(IEnumerable<string> needs = null, int wallet = 100) => new(needs ?? new[] { "restorative", "mild" }, wallet);
    private static ServedDish Dish(IEnumerable<string> needTags = null, int price = 100, IDictionary<string, int> parameters = null, IEnumerable<string> dietaryConflicts = null) => new(needTags ?? new[] { "restorative", "mild" }, price, parameters ?? Params(), dietaryConflicts);
    private static Dictionary<string, int> Params(int heat = 50, int cookTime = 50, int seasoning = 50) => new() { { "heat", heat }, { "cook_time", cookTime }, { "seasoning", seasoning } };
    private static AxisScore Axis(Satisfaction result) => result.AxisScores.Single(score => score.Axis == "heat");

    private static void AssertScoresEqual(Satisfaction expected, Satisfaction actual)
    {
        Assert.That(actual.Total, Is.EqualTo(expected.Total).Within(0.0000001));
        Assert.That(actual.NeedScore, Is.EqualTo(expected.NeedScore).Within(0.0000001));
        Assert.That(actual.TasteScore, Is.EqualTo(expected.TasteScore).Within(0.0000001));
        Assert.That(actual.BudgetScore, Is.EqualTo(expected.BudgetScore).Within(0.0000001));
        Assert.That(actual.DietaryFactor, Is.EqualTo(expected.DietaryFactor).Within(0.0000001));
        CollectionAssert.AreEqual(expected.UnmetNeeds, actual.UnmetNeeds);
        CollectionAssert.AreEqual(expected.ViolatedDietary, actual.ViolatedDietary);
        Assert.That(actual.AxisScores.Count, Is.EqualTo(expected.AxisScores.Count));
        for (int index = 0; index < expected.AxisScores.Count; index++)
        {
            AxisScore left = expected.AxisScores[index]; AxisScore right = actual.AxisScores[index];
            Assert.That(right.Axis, Is.EqualTo(left.Axis)); Assert.That(right.Value, Is.EqualTo(left.Value));
            Assert.That(right.Score, Is.EqualTo(left.Score).Within(0.0000001));
            Assert.That(right.Distance, Is.EqualTo(left.Distance)); Assert.That(right.Direction, Is.EqualTo(left.Direction));
        }
    }
}
}
