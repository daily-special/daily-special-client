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

    public VisitStateResponse VisitState => visitState;
    public DayPhase Phase { get; private set; }
    public bool HasBoughtIngredients { get; private set; }
    public string SelectedDishId { get; private set; }

    public string GuestId => string.IsNullOrWhiteSpace(guestId) ? visitState?.guest_id : guestId;

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
}
