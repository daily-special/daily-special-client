using System;
using System.Collections.Generic;
using System.Linq;

namespace DailySpecial.Domain
{

public sealed class NeedNumbers
{
    public NeedNumbers(int hungerHigh, int hungerMid, int fillingWhenVeryHungry, int fillingWhenHungry,
        int restorativeWhenInjured, int mildWhenInjured, int restorativeWhenTired, int mildWhenTired,
        int stimulatingWhenNormal, int mildWhenGloomy, int specialWhenElated, int stimulatingWhenElated,
        int walletLow, int walletMid, int affordableWhenBroke, int affordableWhenTight,
        int preferredBonus, int selectionThreshold, int maxNeeds)
    {
        if (hungerMid >= hungerHigh) throw new ArgumentException($"허기 문턱이 뒤집혔다: {hungerMid} >= {hungerHigh}");
        if (walletLow >= walletMid) throw new ArgumentException($"지갑 문턱이 뒤집혔다: {walletLow} >= {walletMid}");
        if (selectionThreshold < 1) throw new ArgumentException($"선택 문턱은 1 이상이어야 한다: {selectionThreshold}");
        if (maxNeeds < 1) throw new ArgumentException($"욕구는 최소 1개는 나와야 한다: {maxNeeds}");
        HungerHigh = hungerHigh; HungerMid = hungerMid; FillingWhenVeryHungry = fillingWhenVeryHungry; FillingWhenHungry = fillingWhenHungry;
        RestorativeWhenInjured = restorativeWhenInjured; MildWhenInjured = mildWhenInjured; RestorativeWhenTired = restorativeWhenTired; MildWhenTired = mildWhenTired;
        StimulatingWhenNormal = stimulatingWhenNormal; MildWhenGloomy = mildWhenGloomy; SpecialWhenElated = specialWhenElated; StimulatingWhenElated = stimulatingWhenElated;
        WalletLow = walletLow; WalletMid = walletMid; AffordableWhenBroke = affordableWhenBroke; AffordableWhenTight = affordableWhenTight;
        PreferredBonus = preferredBonus; SelectionThreshold = selectionThreshold; MaxNeeds = maxNeeds;
    }

    public int HungerHigh { get; } public int HungerMid { get; } public int FillingWhenVeryHungry { get; } public int FillingWhenHungry { get; }
    public int RestorativeWhenInjured { get; } public int MildWhenInjured { get; } public int RestorativeWhenTired { get; } public int MildWhenTired { get; }
    public int StimulatingWhenNormal { get; } public int MildWhenGloomy { get; } public int SpecialWhenElated { get; } public int StimulatingWhenElated { get; }
    public int WalletLow { get; } public int WalletMid { get; } public int AffordableWhenBroke { get; } public int AffordableWhenTight { get; }
    public int PreferredBonus { get; } public int SelectionThreshold { get; } public int MaxNeeds { get; }

    public static NeedNumbers Defaults() => new(70, 40, 2, 1, 3, 2, 2, 2, 1, 2, 3, 1, 16, 24, 3, 1, 1, 2, 2);
}

public sealed class NeedResolver
{
    private readonly NeedNumbers numbers;

    public NeedResolver(NeedNumbers numbers)
    {
        this.numbers = numbers ?? throw new ArgumentException("numbers가 없다", nameof(numbers));
    }

    public IReadOnlyList<Need> Resolve(VisitState state, GuestTraits traits)
    {
        Dictionary<Need, int> scores = Score(state, traits);
        List<Need> ranked = Enum.GetValues(typeof(Need)).Cast<Need>()
            .Where(need => scores[need] > 0)
            .OrderByDescending(need => scores[need])
            .ThenBy(need => (int)need)
            .ToList();
        List<Need> chosen = ranked.Where(need => scores[need] >= numbers.SelectionThreshold).Take(numbers.MaxNeeds).ToList();
        return chosen.Count == 0 ? ranked.Take(1).ToList() : chosen;
    }

    public IReadOnlyDictionary<Need, int> Explain(VisitState state, GuestTraits traits) => Score(state, traits);

    private Dictionary<Need, int> Score(VisitState state, GuestTraits traits)
    {
        if (state == null) throw new ArgumentException("state가 없다", nameof(state));
        if (traits == null) throw new ArgumentException("traits가 없다", nameof(traits));

        Dictionary<Need, int> scores = Enum.GetValues(typeof(Need)).Cast<Need>().ToDictionary(need => need, _ => 0);
        Add(scores, Need.Filling, state.Hunger >= numbers.HungerHigh ? numbers.FillingWhenVeryHungry : state.Hunger >= numbers.HungerMid ? numbers.FillingWhenHungry : 0);
        switch (state.Condition)
        {
            case Condition.Injured: Add(scores, Need.Restorative, numbers.RestorativeWhenInjured); Add(scores, Need.Mild, numbers.MildWhenInjured); break;
            case Condition.Tired: Add(scores, Need.Restorative, numbers.RestorativeWhenTired); Add(scores, Need.Mild, numbers.MildWhenTired); break;
            case Condition.Normal: Add(scores, Need.Stimulating, numbers.StimulatingWhenNormal); break;
        }
        switch (state.Mood)
        {
            case Mood.Gloomy: Add(scores, Need.Mild, numbers.MildWhenGloomy); break;
            case Mood.Elated: Add(scores, Need.Special, numbers.SpecialWhenElated); Add(scores, Need.Stimulating, numbers.StimulatingWhenElated); break;
        }
        Add(scores, Need.Affordable, state.Wallet <= numbers.WalletLow ? numbers.AffordableWhenBroke : state.Wallet <= numbers.WalletMid ? numbers.AffordableWhenTight : 0);
        foreach (Need need in traits.PreferredNeeds) Add(scores, need, numbers.PreferredBonus);
        return scores;
    }

    private static void Add(IDictionary<Need, int> scores, Need need, int points) => scores[need] += points;
}
}
