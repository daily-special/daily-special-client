using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DailySpecial.Domain
{

public enum Condition
{
    Normal,
    Injured,
    Tired
}

public enum Mood
{
    Gloomy,
    Calm,
    Elated
}

// 선언 순서는 동점일 때의 우선순위다.
public enum Need
{
    Filling,
    Restorative,
    Mild,
    Stimulating,
    Affordable,
    Special
}

public static class NeedCatalog
{
    public static string ToSlug(Need need)
    {
        return need.ToString().ToLowerInvariant();
    }

    public static Need FromSlug(string slug)
    {
        foreach (Need need in Enum.GetValues(typeof(Need)))
        {
            if (ToSlug(need) == slug)
            {
                return need;
            }
        }

        throw new ArgumentException($"계약에 없는 욕구다: {slug}");
    }
}

public sealed class VisitSeed
{
    public VisitSeed(string saveId, int dayNumber, string guestId)
    {
        if (string.IsNullOrWhiteSpace(saveId))
        {
            throw new ArgumentException("saveId가 비어 있다", nameof(saveId));
        }
        if (string.IsNullOrWhiteSpace(guestId))
        {
            throw new ArgumentException("guestId가 비어 있다", nameof(guestId));
        }
        if (dayNumber < 1)
        {
            throw new ArgumentException($"dayNumber는 1 이상이어야 한다: {dayNumber}", nameof(dayNumber));
        }

        SaveId = saveId;
        DayNumber = dayNumber;
        GuestId = guestId;
    }

    public string SaveId { get; }
    public int DayNumber { get; }
    public string GuestId { get; }

    public long ToSeed()
    {
        byte[] digest;
        using (SHA256 sha256 = SHA256.Create())
        {
            digest = sha256.ComputeHash(Encoding.UTF8.GetBytes($"{SaveId}:{DayNumber}:{GuestId}"));
        }

        long seed = 0;
        for (int index = 0; index < sizeof(long); index++)
        {
            seed = unchecked((seed << 8) | digest[index]);
        }

        return seed;
    }
}

public sealed class VisitState : IEquatable<VisitState>
{
    public VisitState(int hunger, Condition condition, Mood mood, int wallet)
        : this(hunger, condition, mood, wallet, Array.Empty<string>())
    {
    }

    // 만족도 엔진은 오늘의 욕구와 지갑만 읽는다.
    public VisitState(IEnumerable<string> needs, int wallet)
        : this(0, Condition.Normal, Mood.Calm, wallet, needs)
    {
    }

    private VisitState(int hunger, Condition condition, Mood mood, int wallet, IEnumerable<string> needs)
    {
        if (needs == null)
        {
            throw new ArgumentException("욕구 목록이 없다", nameof(needs));
        }

        Hunger = hunger;
        Condition = condition;
        Mood = mood;
        Wallet = wallet;
        Needs = needs.ToArray();
    }

    public int Hunger { get; }
    public Condition Condition { get; }
    public Mood Mood { get; }
    public int Wallet { get; }
    public IReadOnlyList<string> Needs { get; }

    public bool Equals(VisitState other)
    {
        return other != null
            && Hunger == other.Hunger
            && Condition == other.Condition
            && Mood == other.Mood
            && Wallet == other.Wallet
            && Needs.SequenceEqual(other.Needs);
    }

    public override bool Equals(object obj) => Equals(obj as VisitState);
    public override int GetHashCode()
    {
        int hash = HashCode.Combine(Hunger, Condition, Mood, Wallet);
        foreach (string need in Needs) hash = HashCode.Combine(hash, need);
        return hash;
    }
}

public sealed class GuestTraits
{
    public GuestTraits(IEnumerable<Need> preferredNeeds)
    {
        if (preferredNeeds == null)
        {
            throw new ArgumentException("preferredNeeds가 없다", nameof(preferredNeeds));
        }

        PreferredNeeds = new HashSet<Need>(preferredNeeds);
    }

    public IReadOnlyCollection<Need> PreferredNeeds { get; }
    public bool PrefersAffordable => PreferredNeeds.Contains(Need.Affordable);

    public static GuestTraits Of(params Need[] needs) => new(needs ?? Array.Empty<Need>());
}

public sealed class VisitNumbers
{
    public VisitNumbers(
        int hungerMin,
        int hungerMax,
        IDictionary<Condition, int> conditionWeights,
        IDictionary<Mood, int> moodWeights,
        int walletMin,
        int walletMax,
        double affordableWalletMaxRatio)
    {
        if (hungerMin < 0 || hungerMin > hungerMax)
        {
            throw new ArgumentException($"허기 구간이 뒤집혔다: {hungerMin}~{hungerMax}");
        }
        if (walletMin <= 0 || walletMin > walletMax)
        {
            throw new ArgumentException($"지갑 구간이 뒤집혔다: {walletMin}~{walletMax}");
        }
        if (affordableWalletMaxRatio <= 0 || affordableWalletMaxRatio > 1)
        {
            throw new ArgumentException($"저렴 성향 지갑 비율은 0 초과 1 이하여야 한다: {affordableWalletMaxRatio}");
        }

        HungerMin = hungerMin;
        HungerMax = hungerMax;
        WalletMin = walletMin;
        WalletMax = walletMax;
        AffordableWalletMaxRatio = affordableWalletMaxRatio;
        ConditionWeights = CheckedWeights(conditionWeights, "컨디션");
        MoodWeights = CheckedWeights(moodWeights, "기분");
    }

    public int HungerMin { get; }
    public int HungerMax { get; }
    public IReadOnlyDictionary<Condition, int> ConditionWeights { get; }
    public IReadOnlyDictionary<Mood, int> MoodWeights { get; }
    public int WalletMin { get; }
    public int WalletMax { get; }
    public double AffordableWalletMaxRatio { get; }
    public int AffordableWalletMax => WalletMin + (int)Math.Round(
        (WalletMax - WalletMin) * AffordableWalletMaxRatio,
        MidpointRounding.AwayFromZero);

    public static VisitNumbers Defaults() => new(
        0, 100,
        new Dictionary<Condition, int> { { Condition.Normal, 70 }, { Condition.Tired, 20 }, { Condition.Injured, 10 } },
        new Dictionary<Mood, int> { { Mood.Calm, 60 }, { Mood.Elated, 25 }, { Mood.Gloomy, 15 } },
        8, 40, 0.5);

    private static IReadOnlyDictionary<T, int> CheckedWeights<T>(IDictionary<T, int> weights, string label) where T : struct, Enum
    {
        if (weights == null)
        {
            throw new ArgumentException($"{label} 가중치가 없다");
        }

        int total = 0;
        foreach (T value in Enum.GetValues(typeof(T)))
        {
            if (!weights.TryGetValue(value, out int weight))
            {
                throw new ArgumentException($"{label} 가중치에 {value}이(가) 빠졌다");
            }
            if (weight < 0)
            {
                throw new ArgumentException($"{label} 가중치가 음수다: {value}={weight}");
            }
            total += weight;
        }
        if (total == 0)
        {
            throw new ArgumentException($"{label} 가중치 합이 0이다");
        }

        return new Dictionary<T, int>(weights);
    }
}
}
