using System;
using System.Collections.Generic;
using System.Linq;

namespace DailySpecial.Domain
{

public sealed class IdealRange
{
    public IdealRange(int low, int high)
    {
        if (low > high)
        {
            throw new ArgumentException($"이상 구간이 뒤집혔다: {low} > {high}");
        }

        Low = low;
        High = high;
    }

    public int Low { get; }
    public int High { get; }
}

public sealed class GuestPersona
{
    public GuestPersona(IDictionary<string, IdealRange> idealRanges, IEnumerable<string> dietary = null)
    {
        if (idealRanges == null) throw new ArgumentException("이상 구간이 없다", nameof(idealRanges));

        IdealRanges = new Dictionary<string, IdealRange>(idealRanges);
        Dietary = (dietary ?? Array.Empty<string>()).ToArray();
    }

    public IReadOnlyDictionary<string, IdealRange> IdealRanges { get; }
    public IReadOnlyList<string> Dietary { get; }
}

public sealed class ServedDish
{
    public ServedDish(IEnumerable<string> needTags, int price, IDictionary<string, int> parameters, IEnumerable<string> dietaryConflicts = null)
    {
        if (needTags == null) throw new ArgumentException("욕구 태그가 없다", nameof(needTags));
        if (parameters == null) throw new ArgumentException("조리 파라미터가 없다", nameof(parameters));

        NeedTags = needTags.ToArray();
        Price = price;
        Params = new Dictionary<string, int>(parameters);
        DietaryConflicts = (dietaryConflicts ?? Array.Empty<string>()).ToArray();
    }

    public IReadOnlyList<string> NeedTags { get; }
    public int Price { get; }
    public IReadOnlyList<string> DietaryConflicts { get; }
    public IReadOnlyDictionary<string, int> Params { get; }
}

public sealed class ScoringNumbers
{
    public ScoringNumbers(double needFloor, int axisTolerance, double budgetOverrunRatio,
        double dietaryViolationFactor, IEnumerable<string> axes)
    {
        if (axes == null) throw new ArgumentException("축 목록이 없다", nameof(axes));

        NeedFloor = needFloor;
        AxisTolerance = axisTolerance;
        BudgetOverrunRatio = budgetOverrunRatio;
        DietaryViolationFactor = dietaryViolationFactor;
        Axes = axes.ToArray();
    }

    public double NeedFloor { get; }
    public int AxisTolerance { get; }
    public double BudgetOverrunRatio { get; }
    public double DietaryViolationFactor { get; }
    public IReadOnlyList<string> Axes { get; }

    public static ScoringNumbers Defaults() => new(0.15, 25, 1.5, 0.1,
        new[] { "heat", "cook_time", "seasoning" });
}

public sealed class AxisScore
{
    public AxisScore(string axis, int value, double score, int distance, int direction)
    {
        Axis = axis;
        Value = value;
        Score = score;
        Distance = distance;
        Direction = direction;
    }

    public string Axis { get; }
    public int Value { get; }
    public double Score { get; }
    public int Distance { get; }
    public int Direction { get; }
}

public sealed class Satisfaction
{
    public Satisfaction(double total, double needScore, double tasteScore, double budgetScore,
        double dietaryFactor, IEnumerable<AxisScore> axisScores, IEnumerable<string> unmetNeeds,
        IEnumerable<string> violatedDietary)
    {
        Total = total;
        NeedScore = needScore;
        TasteScore = tasteScore;
        BudgetScore = budgetScore;
        DietaryFactor = dietaryFactor;
        AxisScores = axisScores.ToArray();
        UnmetNeeds = unmetNeeds.ToArray();
        ViolatedDietary = violatedDietary.ToArray();
    }

    public double Total { get; }
    public double NeedScore { get; }
    public double TasteScore { get; }
    public double BudgetScore { get; }
    public double DietaryFactor { get; }
    public IReadOnlyList<AxisScore> AxisScores { get; }
    public IReadOnlyList<string> UnmetNeeds { get; }
    public IReadOnlyList<string> ViolatedDietary { get; }
}
}
