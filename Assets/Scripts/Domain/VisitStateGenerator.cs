using System;
using System.Collections.Generic;

namespace DailySpecial.Domain
{

public sealed class VisitStateGenerator
{
    private readonly VisitNumbers numbers;

    public VisitStateGenerator(VisitNumbers numbers)
    {
        this.numbers = numbers ?? throw new ArgumentException("numbers가 없다", nameof(numbers));
    }

    public VisitState Generate(VisitSeed seed, GuestTraits traits)
    {
        if (seed == null) throw new ArgumentException("seed가 없다", nameof(seed));
        if (traits == null) throw new ArgumentException("traits가 없다", nameof(traits));

        SplitMix64 random = new(seed.ToSeed());
        int hunger = Between(random, numbers.HungerMin, numbers.HungerMax);
        Condition condition = Pick(random, numbers.ConditionWeights);
        Mood mood = Pick(random, numbers.MoodWeights);
        int wallet = Between(random, numbers.WalletMin, traits.PrefersAffordable ? numbers.AffordableWalletMax : numbers.WalletMax);
        return new VisitState(hunger, condition, mood, wallet);
    }

    private static int Between(SplitMix64 random, int min, int max) => min + random.NextInt(max - min + 1);

    private static T Pick<T>(SplitMix64 random, IReadOnlyDictionary<T, int> weights) where T : struct, Enum
    {
        int total = 0;
        foreach (T value in Enum.GetValues(typeof(T))) total += weights[value];

        int roll = random.NextInt(total);
        int accumulated = 0;
        foreach (T value in Enum.GetValues(typeof(T)))
        {
            accumulated += weights[value];
            if (roll < accumulated) return value;
        }

        throw new InvalidOperationException("가중 추첨이 값을 고르지 못했다");
    }
}
}
