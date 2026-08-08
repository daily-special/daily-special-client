using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DailySpecial.Domain
{
public enum Tier
{
    Stranger,
    Familiar,
    Regular
}

public sealed class RelationshipNumbers
{
    public RelationshipNumbers(
        int affinityMax,
        double satisfactionNeutral,
        double affinityGain,
        int familiarFrom,
        int regularFrom,
        int axisRevealHints)
    {
        if (affinityMax < 1)
        {
            throw new ArgumentException("친밀도 최댓값은 1 이상이어야 합니다.", nameof(affinityMax));
        }
        if (satisfactionNeutral < 0.0 || satisfactionNeutral > 1.0)
        {
            throw new ArgumentException("중립 만족도는 0과 1 사이여야 합니다.", nameof(satisfactionNeutral));
        }
        if (affinityGain <= 0.0)
        {
            throw new ArgumentException("친밀도 증감 계수는 양수여야 합니다.", nameof(affinityGain));
        }
        if (familiarFrom < 1 || regularFrom <= familiarFrom || regularFrom > affinityMax)
        {
            throw new ArgumentException("관계 단계 경계가 올바르지 않습니다.");
        }
        if (axisRevealHints < 1)
        {
            throw new ArgumentException("축 공개 힌트 횟수는 1 이상이어야 합니다.", nameof(axisRevealHints));
        }

        AffinityMax = affinityMax;
        SatisfactionNeutral = satisfactionNeutral;
        AffinityGain = affinityGain;
        FamiliarFrom = familiarFrom;
        RegularFrom = regularFrom;
        AxisRevealHints = axisRevealHints;
    }

    public int AffinityMax { get; }
    public double SatisfactionNeutral { get; }
    public double AffinityGain { get; }
    public int FamiliarFrom { get; }
    public int RegularFrom { get; }
    public int AxisRevealHints { get; }

    public static RelationshipNumbers Defaults() => new(100, 0.4, 20.0, 20, 60, 3);
}

public sealed class Relationship
{
    public Relationship(int affinity, IReadOnlyDictionary<string, int> axisHints)
    {
        if (affinity < 0)
        {
            throw new ArgumentException("친밀도는 0 이상이어야 합니다.", nameof(affinity));
        }
        if (axisHints == null)
        {
            throw new ArgumentException("축 힌트가 없습니다.", nameof(axisHints));
        }

        SortedDictionary<string, int> copiedHints = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, int> hint in axisHints)
        {
            if (string.IsNullOrWhiteSpace(hint.Key) || hint.Value < 0)
            {
                throw new ArgumentException("축 힌트가 올바르지 않습니다.", nameof(axisHints));
            }

            copiedHints.Add(hint.Key, hint.Value);
        }

        Affinity = affinity;
        AxisHints = new ReadOnlyDictionary<string, int>(copiedHints);
    }

    public int Affinity { get; }
    public IReadOnlyDictionary<string, int> AxisHints { get; }

    public int HintsFor(string axis) => AxisHints.TryGetValue(axis, out int hints) ? hints : 0;

    public static Relationship None() => new(0, new Dictionary<string, int>());
}

public sealed class Disclosure
{
    public Disclosure(bool preferredNeeds, bool dietary, bool allAxes, IEnumerable<string> revealedAxes)
    {
        if (revealedAxes == null)
        {
            throw new ArgumentException("공개된 축이 없습니다.", nameof(revealedAxes));
        }

        SortedSet<string> copiedAxes = new(revealedAxes, StringComparer.Ordinal);
        PreferredNeeds = preferredNeeds;
        Dietary = dietary;
        AllAxes = allAxes;
        RevealedAxes = new ReadOnlyCollection<string>(copiedAxes.ToList());
    }

    public bool PreferredNeeds { get; }
    public bool Dietary { get; }
    public bool AllAxes { get; }
    public IReadOnlyCollection<string> RevealedAxes { get; }

    public bool RevealsAxis(string axis) => AllAxes || RevealedAxes.Contains(axis);
}
}
