using System;
using System.Collections.Generic;
using System.Linq;

namespace DailySpecial.Domain
{

public sealed class SatisfactionEngine
{
    private readonly ScoringNumbers numbers;

    public SatisfactionEngine(ScoringNumbers numbers)
    {
        this.numbers = numbers ?? throw new ArgumentException("만족도 계수가 없다", nameof(numbers));
    }

    public Satisfaction Evaluate(GuestPersona persona, VisitState state, ServedDish dish)
    {
        if (persona == null) throw new ArgumentException("손님 페르소나가 없다", nameof(persona));
        if (state == null) throw new ArgumentException("방문 상태가 없다", nameof(state));
        if (dish == null) throw new ArgumentException("낸 요리가 없다", nameof(dish));

        (double needScore, List<string> unmetNeeds) = ScoreNeeds(state, dish);
        List<AxisScore> axisScores = ScoreAxes(persona, dish);
        double tasteScore = axisScores.Count == 0 ? 1.0 : axisScores.Average(score => score.Score);
        double budgetScore = ScoreBudget(state, dish);
        (double dietaryFactor, List<string> violatedDietary) = ScoreDietary(persona, dish);
        double total = needScore * tasteScore * budgetScore * dietaryFactor;

        return new Satisfaction(total, needScore, tasteScore, budgetScore, dietaryFactor,
            axisScores, unmetNeeds, violatedDietary);
    }

    private (double, List<string>) ScoreNeeds(VisitState state, ServedDish dish)
    {
        if (state.Needs.Count == 0) return (1.0, new List<string>());

        HashSet<string> tags = new(dish.NeedTags);
        List<string> unmet = state.Needs.Where(need => !tags.Contains(need)).ToList();
        double ratio = (double)(state.Needs.Count - unmet.Count) / state.Needs.Count;
        return (Math.Max(ratio, numbers.NeedFloor), unmet);
    }

    private List<AxisScore> ScoreAxes(GuestPersona persona, ServedDish dish)
    {
        List<AxisScore> scores = new();
        foreach (string axis in numbers.Axes)
        {
            if (!persona.IdealRanges.TryGetValue(axis, out IdealRange ideal)) continue;
            if (!dish.Params.TryGetValue(axis, out int value))
            {
                throw new InvalidOperationException($"요리에 축 '{axis}'의 값이 없다. 손님은 이 축에 취향을 갖고 있다");
            }

            int distance;
            int direction;
            if (value < ideal.Low) { distance = ideal.Low - value; direction = -1; }
            else if (value > ideal.High) { distance = value - ideal.High; direction = 1; }
            else { distance = 0; direction = 0; }

            double score = distance == 0 ? 1.0 : Math.Max(0.0, 1.0 - (double)distance / numbers.AxisTolerance);
            scores.Add(new AxisScore(axis, value, score, distance, direction));
        }

        return scores;
    }

    private double ScoreBudget(VisitState state, ServedDish dish)
    {
        if (dish.Price <= state.Wallet) return 1.0;
        if (state.Wallet <= 0) return 0.0;

        double zeroAt = state.Wallet * numbers.BudgetOverrunRatio;
        return dish.Price >= zeroAt ? 0.0 : (zeroAt - dish.Price) / (zeroAt - state.Wallet);
    }

    private (double, List<string>) ScoreDietary(GuestPersona persona, ServedDish dish)
    {
        HashSet<string> conflicts = new(dish.DietaryConflicts);
        List<string> violated = persona.Dietary.Where(conflicts.Contains).ToList();
        return (Math.Pow(numbers.DietaryViolationFactor, violated.Count), violated);
    }
}
}
