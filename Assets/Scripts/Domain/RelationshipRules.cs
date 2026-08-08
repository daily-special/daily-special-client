using System;
using System.Collections.Generic;

namespace DailySpecial.Domain
{
public sealed class RelationshipRules
{
    private readonly RelationshipNumbers numbers;

    public RelationshipRules(RelationshipNumbers numbers)
    {
        this.numbers = numbers ?? throw new ArgumentException("관계 규칙 수치가 없습니다.", nameof(numbers));
    }

    public Relationship AfterVisit(Relationship current, double satisfaction, IEnumerable<string> offAxes)
    {
        if (current == null)
        {
            throw new ArgumentException("현재 관계가 없습니다.", nameof(current));
        }
        if (offAxes == null)
        {
            throw new ArgumentException("벗어난 축 목록이 없습니다.", nameof(offAxes));
        }
        if (!(satisfaction >= 0.0) || satisfaction > 1.0)
        {
            throw new ArgumentException("만족도는 0과 1 사이여야 합니다.", nameof(satisfaction));
        }

        double raw = (satisfaction - numbers.SatisfactionNeutral) * numbers.AffinityGain;
        int delta = (int)Math.Floor(raw + 0.5);
        int affinity = Math.Clamp(current.Affinity + delta, 0, numbers.AffinityMax);

        SortedDictionary<string, int> hints = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, int> hint in current.AxisHints)
        {
            hints.Add(hint.Key, hint.Value);
        }
        foreach (string axis in offAxes)
        {
            if (string.IsNullOrWhiteSpace(axis))
            {
                throw new ArgumentException("벗어난 축 이름이 올바르지 않습니다.", nameof(offAxes));
            }

            hints[axis] = hints.TryGetValue(axis, out int count) ? count + 1 : 1;
        }

        return new Relationship(affinity, hints);
    }

    public Tier TierOf(Relationship relationship)
    {
        if (relationship == null)
        {
            throw new ArgumentException("관계가 없습니다.", nameof(relationship));
        }
        if (relationship.Affinity >= numbers.RegularFrom)
        {
            return Tier.Regular;
        }
        if (relationship.Affinity >= numbers.FamiliarFrom)
        {
            return Tier.Familiar;
        }

        return Tier.Stranger;
    }

    public Disclosure Disclose(Relationship relationship)
    {
        if (relationship == null)
        {
            throw new ArgumentException("관계가 없습니다.", nameof(relationship));
        }

        Tier tier = TierOf(relationship);
        bool regular = tier == Tier.Regular;
        SortedSet<string> revealedAxes = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, int> hint in relationship.AxisHints)
        {
            if (hint.Value >= numbers.AxisRevealHints)
            {
                revealedAxes.Add(hint.Key);
            }
        }

        return new Disclosure(tier != Tier.Stranger, regular, regular, revealedAxes);
    }
}
}
