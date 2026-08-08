using System;
using System.Collections.Generic;
using System.Linq;
using DailySpecial.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DayCycleScreen : MonoBehaviour
{
    [SerializeField] private LocalDayStateStore stateStore;
    [SerializeField] private TextMeshProUGUI phaseLabel;
    [SerializeField] private TextMeshProUGUI guestLabel;
    [SerializeField] private TextMeshProUGUI detailLabel;
    [SerializeField] private TextMeshProUGUI relationshipLabel;
    [SerializeField] private TextMeshProUGUI dialogueLabel;
    [SerializeField] private TextMeshProUGUI actionLabel;
    [SerializeField] private Button actionButton;
    [SerializeField] private Slider heatSlider;
    [SerializeField] private Slider cookTimeSlider;
    [SerializeField] private Slider seasoningSlider;
    [SerializeField] private TextMeshProUGUI heatValueLabel;
    [SerializeField] private TextMeshProUGUI cookTimeValueLabel;
    [SerializeField] private TextMeshProUGUI seasoningValueLabel;

    private List<GuestRecord> guests;
    private GuestRecord guest;
    private DishRecord featuredDish;
    private ContentPackage<IngredientRecord> ingredients;
    private ContentPackage<LineRecord> lines;
    private int guestIndex;
    private Satisfaction servedSatisfaction;

    public void Configure(
        LocalDayStateStore configuredStateStore,
        TextMeshProUGUI configuredPhaseLabel,
        TextMeshProUGUI configuredGuestLabel,
        TextMeshProUGUI configuredDetailLabel,
        TextMeshProUGUI configuredRelationshipLabel,
        TextMeshProUGUI configuredDialogueLabel,
        TextMeshProUGUI configuredActionLabel,
        Button configuredActionButton,
        Slider configuredHeatSlider,
        Slider configuredCookTimeSlider,
        Slider configuredSeasoningSlider,
        TextMeshProUGUI configuredHeatValueLabel,
        TextMeshProUGUI configuredCookTimeValueLabel,
        TextMeshProUGUI configuredSeasoningValueLabel)
    {
        stateStore = configuredStateStore;
        phaseLabel = configuredPhaseLabel;
        guestLabel = configuredGuestLabel;
        detailLabel = configuredDetailLabel;
        relationshipLabel = configuredRelationshipLabel;
        dialogueLabel = configuredDialogueLabel;
        actionLabel = configuredActionLabel;
        actionButton = configuredActionButton;
        heatSlider = configuredHeatSlider;
        cookTimeSlider = configuredCookTimeSlider;
        seasoningSlider = configuredSeasoningSlider;
        heatValueLabel = configuredHeatValueLabel;
        cookTimeValueLabel = configuredCookTimeValueLabel;
        seasoningValueLabel = configuredSeasoningValueLabel;
    }

    private void Awake()
    {
        try
        {
            actionButton.onClick.AddListener(Advance);
            heatSlider.onValueChanged.AddListener(_ => RefreshCookingValues());
            cookTimeSlider.onValueChanged.AddListener(_ => RefreshCookingValues());
            seasoningSlider.onValueChanged.AddListener(_ => RefreshCookingValues());
            guests = ContentLoader.LoadGuests().items;
            ContentPackage<DishRecord> dishes = ContentLoader.LoadDishes();
            ingredients = ContentLoader.LoadIngredients();
            lines = ContentLoader.LoadLines();

            guestIndex = guests.FindIndex(item => item.guest_id == stateStore.GuestId);
            if (guestIndex < 0) guestIndex = 0;
            guest = guests[guestIndex];
            stateStore.Configure(guest.guest_id);
            stateStore.Initialize(guest.preferred_needs);
            featuredDish = dishes.items.First(item => item.dish_id == "dish_barley_bean_porridge");
            Refresh();
        }
        catch (Exception exception)
        {
            phaseLabel.text = "콘텐츠를 읽지 못했습니다";
            detailLabel.text = exception.Message;
            actionButton.interactable = false;
            Debug.LogException(exception);
        }
    }

    public void Advance()
    {
        switch (stateStore.Phase)
        {
            case DayPhase.Shopping:
                stateStore.BuyIngredients();
                break;
            case DayPhase.GuestArrival:
                stateStore.ChooseDish(featuredDish.dish_id);
                break;
            case DayPhase.Cooking:
                servedSatisfaction = EvaluateSatisfaction();
                stateStore.ApplyRelationship(
                    servedSatisfaction.Total,
                    servedSatisfaction.AxisScores
                        .Where(score => score.Direction != 0)
                        .Select(score => score.Axis));
                stateStore.ServeDish();
                break;
            case DayPhase.Reaction:
                stateStore.FinishDay();
                break;
            case DayPhase.Complete:
                AdvanceToNextGuest();
                break;
            default:
                return;
        }

        Refresh();
    }

    private void Refresh()
    {
        relationshipLabel.text = BuildRelationshipText();
        SetCookingControlsVisible(stateStore.Phase == DayPhase.Cooking);
        switch (stateStore.Phase)
        {
            case DayPhase.Shopping:
                phaseLabel.text = $"{stateStore.VisitState.day_number}일차 · 1. 장보기";
                guestLabel.text = "오늘의 재료";
                detailLabel.text = "자루보리와 갈색콩을 사서 보리콩죽을 준비합니다.\n시연용 선택이며 재고 규칙은 아직 없습니다.";
                dialogueLabel.text = "시장 상인이 재료를 내어줍니다.";
                SetAction("재료 사기");
                break;
            case DayPhase.GuestArrival:
                phaseLabel.text = $"{stateStore.VisitState.day_number}일차 · 2. 손님 맞이";
                guestLabel.text = $"{guest.name} · {guest.title}";
                detailLabel.text = $"허기 {stateStore.VisitState.hunger} · {stateStore.VisitState.condition} · {stateStore.VisitState.mood}\n예산 {stateStore.VisitState.wallet} · 오늘의 욕구: {string.Join(" · ", stateStore.VisitState.needs)}";
                dialogueLabel.text = FindLine("greet", null).text + "\n"
                    + FindLine("order", stateStore.VisitState.needs[0]).text;
                SetAction($"{featuredDish.name} 요리하기");
                break;
            case DayPhase.Cooking:
                phaseLabel.text = $"{stateStore.VisitState.day_number}일차 · 3. 요리";
                guestLabel.text = featuredDish.name;
                detailLabel.text = $"{featuredDish.description}\n가격 {featuredDish.base_price} · 태그: {string.Join(" · ", featuredDish.need_tags)}\n아래 세 값을 맞춰 조리합니다.";
                dialogueLabel.text = "손님 취향을 떠올리며 불, 시간, 간을 맞춥니다.";
                RefreshCookingValues();
                SetAction("손님에게 내기");
                break;
            case DayPhase.Reaction:
                Satisfaction satisfaction = servedSatisfaction ?? EvaluateSatisfaction();
                phaseLabel.text = $"{stateStore.VisitState.day_number}일차 · 4. 반응";
                guestLabel.text = "오늘의 한 그릇";
                detailLabel.text = $"만족도 {satisfaction.Total * 100:0}%\n욕구 {satisfaction.NeedScore * 100:0}% · 취향 {satisfaction.TasteScore * 100:0}% · 예산 {satisfaction.BudgetScore * 100:0}% · 식이 {satisfaction.DietaryFactor * 100:0}%";
                if (satisfaction.UnmetNeeds.Count > 0) detailLabel.text += $"\n놓친 욕구: {string.Join(" · ", satisfaction.UnmetNeeds)}";
                dialogueLabel.text = FindReactionLine(satisfaction);
                SetAction("하루 마치기");
                break;
            case DayPhase.Complete:
                phaseLabel.text = "오늘 장사를 마쳤습니다";
                guestLabel.text = $"{stateStore.VisitState.day_number}일차 완료";
                detailLabel.text = "다음 날을 열어 오늘 상태와 욕구가 달라지는 것을 확인하세요.";
                dialogueLabel.text = FindLine("leave", null).text;
                SetAction("다음 날 열기");
                break;
        }
    }

    private LineRecord FindLine(string situation, string subject)
    {
        return lines.items.First(item => item.situation == situation && item.subject == subject && item.voice == guest.voice);
    }

    private void AdvanceToNextGuest()
    {
        guestIndex = (guestIndex + 1) % guests.Count;
        guest = guests[guestIndex];
        stateStore.Configure(guest.guest_id);
        stateStore.AdvanceDay(guest.preferred_needs);
        servedSatisfaction = null;
    }

    private string BuildRelationshipText()
    {
        LocalRelationshipState state = stateStore.RelationshipState;
        Disclosure disclosure = stateStore.GetDisclosure();
        List<string> learned = new();

        if (disclosure.PreferredNeeds)
        {
            learned.Add("평소 욕구: " + string.Join(", ", guest.preferred_needs ?? new List<string>()));
        }

        IEnumerable<string> axes = disclosure.AllAxes
            ? guest.ideal_ranges.Keys
            : disclosure.RevealedAxes;
        foreach (string axis in axes.OrderBy(axis => axis))
        {
            if (guest.ideal_ranges.TryGetValue(axis, out IdealRangeRecord range))
            {
                learned.Add($"{AxisName(axis)}: {range.low}~{range.high}");
            }
        }

        if (disclosure.Dietary)
        {
            learned.Add((guest.dietary ?? new List<string>()).Count == 0
                ? "식이 제한: 없음"
                : "식이 제한: " + string.Join(", ", guest.dietary));
        }

        string knowledge = learned.Count == 0 ? "아직 알게 된 정보가 없습니다." : string.Join(" · ", learned);
        return $"관계: {TierName(stateStore.GetRelationshipTier())} ({state.affinity}/100)\n알게 된 것: {knowledge}";
    }

    private static string TierName(Tier tier)
    {
        return tier switch
        {
            Tier.Familiar => "낯익은 손님",
            Tier.Regular => "단골",
            _ => "낯선 손님"
        };
    }

    private static string AxisName(string axis)
    {
        return axis switch
        {
            "heat" => "불 세기",
            "cook_time" => "조리 시간",
            "seasoning" => "간",
            _ => axis
        };
    }

    private Satisfaction EvaluateSatisfaction()
    {
        Dictionary<string, int> parameters = new()
        {
            { "heat", Mathf.RoundToInt(heatSlider.value) },
            { "cook_time", Mathf.RoundToInt(cookTimeSlider.value) },
            { "seasoning", Mathf.RoundToInt(seasoningSlider.value) }
        };
        VisitState state = new(stateStore.VisitState.needs, stateStore.VisitState.wallet);
        ServedDish dish = ContentSatisfactionMapper.ToServedDish(featuredDish, ingredients.items, parameters);
        return new SatisfactionEngine(ScoringNumbers.Defaults()).Evaluate(ContentSatisfactionMapper.ToPersona(guest), state, dish);
    }

    private string FindReactionLine(Satisfaction satisfaction)
    {
        AxisScore missedAxis = satisfaction.AxisScores.FirstOrDefault(score => score.Direction != 0);
        if (missedAxis != null)
        {
            string situation = missedAxis.Direction < 0 ? "feedback_low" : "feedback_high";
            return FindLine(situation, missedAxis.Axis).text;
        }

        return FindLine(satisfaction.Total >= 0.7 ? "reaction_high" : "reaction_low", null).text;
    }

    private void RefreshCookingValues()
    {
        heatValueLabel.text = $"불 세기  {heatSlider.value:0}";
        cookTimeValueLabel.text = $"조리 시간  {cookTimeSlider.value:0}";
        seasoningValueLabel.text = $"간  {seasoningSlider.value:0}";
    }

    private void SetCookingControlsVisible(bool visible)
    {
        heatSlider.gameObject.SetActive(visible);
        cookTimeSlider.gameObject.SetActive(visible);
        seasoningSlider.gameObject.SetActive(visible);
        heatValueLabel.gameObject.SetActive(visible);
        cookTimeValueLabel.gameObject.SetActive(visible);
        seasoningValueLabel.gameObject.SetActive(visible);
    }

    private void SetAction(string label)
    {
        actionButton.interactable = true;
        actionLabel.text = label;
    }
}
