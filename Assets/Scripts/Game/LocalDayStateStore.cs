using System;
using System.Collections.Generic;
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

    public VisitStateResponse VisitState => visitState;
    public DayPhase Phase { get; private set; }
    public bool HasBoughtIngredients { get; private set; }
    public string SelectedDishId { get; private set; }

    public void Configure(string guestId)
    {
        // 서버 API가 붙으면 이 고정 객체를 GET 응답으로 교체한다.
        visitState = new VisitStateResponse
        {
            save_id = "local-save-1",
            day_number = 1,
            guest_id = guestId,
            hunger = 18,
            condition = "tired",
            mood = "gloomy",
            wallet = 10,
            needs = new List<string> { "mild", "affordable" }
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
}
