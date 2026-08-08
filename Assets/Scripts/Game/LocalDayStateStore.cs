using System;
using System.Collections.Generic;
using System.Linq;
using DailySpecial.Domain;
using UnityEngine;

[Serializable]
public sealed class VisitStateResponse
{
    public string save_id;
    public int day_number;
    public string guest_id;
    public int hunger;
    public string condition;
    public string mood;
    public int wallet;
    public List<string> needs;
}

[Serializable]
public sealed class AxisHintState
{
    public string axis;
    public int count;
}

[Serializable]
public sealed class LocalRelationshipState
{
    public string guest_id;
    public int affinity;
    public List<AxisHintState> axis_hints = new();
}

public enum DayPhase
{
    Shopping,
    GuestArrival,
    Cooking,
    Reaction,
    Complete
}

public sealed class LocalDayStateStore : MonoBehaviour
{
    [SerializeField] private VisitStateResponse visitState;
    [SerializeField] private string guestId;
    [SerializeField] private List<LocalRelationshipState> relationships = new();

    public VisitStateResponse VisitState => visitState;
    public DayPhase Phase { get; private set; }
    public bool HasBoughtIngredients { get; private set; }
    public string SelectedDishId { get; private set; }

    public string GuestId => string.IsNullOrWhiteSpace(guestId) ? visitState?.guest_id : guestId;

    public LocalRelationshipState RelationshipState => FindOrCreateRelationship(GuestId);

    public void Configure(string configuredGuestId)
    {
        guestId = configuredGuestId;
        Phase = DayPhase.Shopping;
    }

    public void Initialize(IEnumerable<string> preferredNeedSlugs)
    {
        guestId = GuestId;
        if (string.IsNullOrWhiteSpace(guestId))
        {
            throw new InvalidOperationException("오늘 손님 식별자가 없습니다.");
        }

        // 2단계 이전 씬에는 day_number가 0인 직렬화 표본이 남아 있을 수 있다.
        int dayNumber = Math.Max(1, visitState?.day_number ?? 1);
        GuestTraits traits = new(preferredNeedSlugs.Select(NeedCatalog.FromSlug));
        VisitState generated = new VisitStateGenerator(VisitNumbers.Defaults())
            .Generate(new VisitSeed("local-save-1", dayNumber, guestId), traits);

        // 서버 API가 붙으면 이 매핑의 공급자만 GET 응답으로 교체한다.
        visitState = new VisitStateResponse
        {
            save_id = "local-save-1",
            day_number = dayNumber,
            guest_id = guestId,
            hunger = generated.Hunger,
            condition = generated.Condition.ToString().ToLowerInvariant(),
            mood = generated.Mood.ToString().ToLowerInvariant(),
            wallet = generated.Wallet,
            needs = new NeedResolver(NeedNumbers.Defaults()).Resolve(generated, traits).Select(NeedCatalog.ToSlug).ToList()
        };
        FindOrCreateRelationship(guestId);
        Phase = DayPhase.Shopping;
    }

    public void BuyIngredients()
    {
        HasBoughtIngredients = true;
        Phase = DayPhase.GuestArrival;
    }

    public void ChooseDish(string dishId)
    {
        SelectedDishId = dishId;
        Phase = DayPhase.Cooking;
    }

    public void ServeDish()
    {
        Phase = DayPhase.Reaction;
    }

    public void FinishDay()
    {
        Phase = DayPhase.Complete;
    }

    public void AdvanceDay(IEnumerable<string> preferredNeedSlugs)
    {
        visitState.day_number++;
        Initialize(preferredNeedSlugs);
    }

    public void ApplyRelationship(double satisfaction, IEnumerable<string> offAxes)
    {
        LocalRelationshipState localState = RelationshipState;
        RelationshipRules rules = new(RelationshipNumbers.Defaults());
        Relationship updated = rules.AfterVisit(ToDomainRelationship(localState), satisfaction, offAxes);

        localState.affinity = updated.Affinity;
        localState.axis_hints = updated.AxisHints
            .OrderBy(hint => hint.Key, StringComparer.Ordinal)
            .Select(hint => new AxisHintState { axis = hint.Key, count = hint.Value })
            .ToList();
    }

    public Disclosure GetDisclosure()
    {
        RelationshipRules rules = new(RelationshipNumbers.Defaults());
        return rules.Disclose(ToDomainRelationship(RelationshipState));
    }

    public Tier GetRelationshipTier()
    {
        RelationshipRules rules = new(RelationshipNumbers.Defaults());
        return rules.TierOf(ToDomainRelationship(RelationshipState));
    }

    private LocalRelationshipState FindOrCreateRelationship(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException("손님 식별자가 없습니다.");
        }

        LocalRelationshipState state = relationships.FirstOrDefault(item => item.guest_id == id);
        if (state != null)
        {
            return state;
        }

        state = new LocalRelationshipState { guest_id = id };
        relationships.Add(state);
        return state;
    }

    private static Relationship ToDomainRelationship(LocalRelationshipState state)
    {
        Dictionary<string, int> hints = new(StringComparer.Ordinal);
        foreach (AxisHintState hint in state.axis_hints ?? new List<AxisHintState>())
        {
            hints.Add(hint.axis, hint.count);
        }

        return new Relationship(state.affinity, hints);
    }
}
