using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ContentBootstrapBuilder
{
    private const string FontAssetPath = "Assets/Fonts/NotoSansCJKkr-Regular Extended SDF.asset";
    private const string DemoGuestId = "guest_dusty_patrol_01";

    [MenuItem("Daily Special/2단계 하루 사이클 만들기")]
    public static void Build()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font == null)
        {
            throw new System.InvalidOperationException("Noto Sans KR TMP 폰트 아틀라스를 찾지 못했습니다.");
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera();
        GameObject canvasObject = CreateCanvas();
        CreateEventSystem();
        GameObject panel = CreatePanel(canvasObject.transform);

        TextMeshProUGUI titleLabel = CreateLabel(panel.transform, font, "Title", 56, FontStyles.Bold);
        TextMeshProUGUI phaseLabel = CreateLabel(panel.transform, font, "Phase", 38, FontStyles.Bold);
        TextMeshProUGUI guestLabel = CreateLabel(panel.transform, font, "Guest", 46, FontStyles.Bold);
        TextMeshProUGUI detailLabel = CreateLabel(panel.transform, font, "Detail", 32, FontStyles.Normal);
        TextMeshProUGUI dialogueLabel = CreateLabel(panel.transform, font, "Dialogue", 34, FontStyles.Normal);
        Button actionButton = CreateButton(panel.transform, font, out TextMeshProUGUI actionLabel);

        titleLabel.text = "오늘의 정식";
        SetAnchors(titleLabel.rectTransform, new Vector2(0.10f, 0.86f), new Vector2(0.90f, 0.95f));
        SetAnchors(phaseLabel.rectTransform, new Vector2(0.10f, 0.76f), new Vector2(0.90f, 0.84f));
        SetAnchors(guestLabel.rectTransform, new Vector2(0.10f, 0.64f), new Vector2(0.90f, 0.74f));
        SetAnchors(detailLabel.rectTransform, new Vector2(0.10f, 0.42f), new Vector2(0.90f, 0.62f));
        SetAnchors(dialogueLabel.rectTransform, new Vector2(0.10f, 0.25f), new Vector2(0.90f, 0.38f));
        SetAnchors(actionButton.GetComponent<RectTransform>(), new Vector2(0.10f, 0.10f), new Vector2(0.90f, 0.20f));

        GameObject stateObject = new("LocalDayStateStore", typeof(LocalDayStateStore));
        LocalDayStateStore stateStore = stateObject.GetComponent<LocalDayStateStore>();
        stateStore.Configure(DemoGuestId);

        GameObject screenObject = new("DayCycleScreen", typeof(DayCycleScreen));
        DayCycleScreen screen = screenObject.GetComponent<DayCycleScreen>();
        screen.Configure(stateStore, phaseLabel, guestLabel, detailLabel, dialogueLabel, actionLabel, actionButton);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/SampleScene.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("2단계 하루 사이클 씬을 만들었습니다.");
    }

    private static GameObject CreateCanvas()
    {
        GameObject canvasObject = new("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        return canvasObject;
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    private static GameObject CreatePanel(Transform parent)
    {
        GameObject panel = new("DayCard", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = new Color(0.10f, 0.14f, 0.19f, 1f);
        SetAnchors(panel.GetComponent<RectTransform>(), new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f));
        return panel;
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new("Main Camera", typeof(Camera));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.05f, 0.07f, 0.10f, 1f);
        cameraObject.tag = "MainCamera";
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, TMP_FontAsset font, string objectName, int fontSize, FontStyles style)
    {
        GameObject label = new(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(parent, false);
        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = new Color(0.95f, 0.92f, 0.84f, 1f);
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private static Button CreateButton(Transform parent, TMP_FontAsset font, out TextMeshProUGUI label)
    {
        GameObject buttonObject = new("ActionButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.32f, 0.46f, 0.44f, 1f);

        label = CreateLabel(buttonObject.transform, font, "Label", 34, FontStyles.Bold);
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        return buttonObject.GetComponent<Button>();
    }

    private static void SetAnchors(RectTransform transform, Vector2 minimum, Vector2 maximum)
    {
        transform.anchorMin = minimum;
        transform.anchorMax = maximum;
        transform.offsetMin = Vector2.zero;
        transform.offsetMax = Vector2.zero;
    }
}
