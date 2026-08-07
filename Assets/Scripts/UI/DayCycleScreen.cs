using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DayCycleScreen : MonoBehaviour
{
    [SerializeField] private LocalDayStateStore stateStore;
    [SerializeField] private TextMeshProUGUI phaseLabel;
    [SerializeField] private TextMeshProUGUI guestLabel;
    [SerializeField] private TextMeshProUGUI detailLabel;
    [SerializeField] private TextMeshProUGUI dialogueLabel;
    [SerializeField] private TextMeshProUGUI actionLabel;
    [SerializeField] private Button actionButton;

    private GuestRecord guest;
    private DishRecord featuredDish;
    private ContentPackage<LineRecord> lines;

    public void Configure(
        LocalDayStateStore configuredStateStore,
        TextMeshProUGUI configuredPhaseLabel,
        TextMeshProUGUI configuredGuestLabel,
        TextMeshProUGUI configuredDetailLabel,
        TextMeshProUGUI configuredDialogueLabel,
        TextMeshProUGUI configuredActionLabel,
        Button configuredActionButton)
    {
        stateStore = configuredStateStore;
        phaseLabel = configuredPhaseLabel;
        guestLabel = configuredGuestLabel;
        detailLabel = configuredDetailLabel;
        dialogueLabel = configuredDialogueLabel;
        actionLabel = configuredActionLabel;
        actionButton = configuredActionButton;
    }

    private void Awake()
    {
        try
        {
            actionButton.onClick.AddListener(Advance);
            ContentPackage<GuestRecord> guests = ContentLoader.LoadGuests();
            ContentPackage<DishRecord> dishes = ContentLoader.LoadDishes();
            lines = ContentLoader.LoadLines();

            guest = guests.items.First(item => item.guest_id == stateStore.GuestId);
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
                stateStore.ServeDish();
                break;
            case DayPhase.Reaction:
                stateStore.FinishDay();
                break;
            case DayPhase.Complete:
                stateStore.AdvanceDay(guest.preferred_needs);
                break;
            default:
                return;
        }

        Refresh();
    }

    private void Refresh()
    {
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
                dialogueLabel.text = FindLine("order", stateStore.VisitState.needs[0]).text;
                SetAction($"{featuredDish.name} 요리하기");
                break;
            case DayPhase.Cooking:
                phaseLabel.text = $"{stateStore.VisitState.day_number}일차 · 3. 요리";
                guestLabel.text = featuredDish.name;
                detailLabel.text = $"{featuredDish.description}\n가격 {featuredDish.base_price} · 태그: {string.Join(" · ", featuredDish.need_tags)}";
                dialogueLabel.text = "천천히 끓여 한 그릇을 완성했습니다.";
                SetAction("손님에게 내기");
                break;
            case DayPhase.Reaction:
                phaseLabel.text = $"{stateStore.VisitState.day_number}일차 · 4. 반응";
                guestLabel.text = "오늘의 한 그릇";
                detailLabel.text = "시연용 고정 성공 반응입니다. 실제 만족도 계산은 3단계에서 이식합니다.";
                dialogueLabel.text = FindLine("reaction_high", null).text;
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

    private void SetAction(string label)
    {
        actionButton.interactable = true;
        actionLabel.text = label;
    }
}
