using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ContentBootstrapBuilder
{
    private const string SourceFontPath = "Assets/Fonts/NotoSansCJKkr-Regular.otf";
    [MenuItem("Daily Special/1단계 씬 만들기")]
    public static void Build()
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (font == null)
        {
            throw new System.InvalidOperationException("Noto Sans KR 원본 폰트를 찾지 못했습니다.");
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera();
        GameObject canvasObject = new("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);

        GameObject panel = CreatePanel(canvasObject.transform);
        Text nameLabel = CreateLabel(panel.transform, font, "Name", 64, FontStyle.Bold);
        Text titleLabel = CreateLabel(panel.transform, font, "Title", 34, FontStyle.Normal);
        Text bioLabel = CreateLabel(panel.transform, font, "Bio", 38, FontStyle.Normal);
        Text statusLabel = CreateLabel(panel.transform, font, "Status", 26, FontStyle.Normal);

        SetAnchors(nameLabel.rectTransform, new Vector2(0.12f, 0.67f), new Vector2(0.88f, 0.83f));
        SetAnchors(titleLabel.rectTransform, new Vector2(0.12f, 0.58f), new Vector2(0.88f, 0.67f));
        SetAnchors(bioLabel.rectTransform, new Vector2(0.12f, 0.30f), new Vector2(0.88f, 0.57f));
        SetAnchors(statusLabel.rectTransform, new Vector2(0.12f, 0.18f), new Vector2(0.88f, 0.25f));

        GameObject controller = new("GuestProfileScreen", typeof(GuestProfileScreen));
        controller.GetComponent<GuestProfileScreen>().Configure(nameLabel, titleLabel, bioLabel, statusLabel);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/SampleScene.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("1단계 손님 소개 씬을 만들었습니다.");
    }

    private static GameObject CreatePanel(Transform parent)
    {
        GameObject panel = new("GuestCard", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.10f, 0.14f, 0.19f, 1f);
        SetAnchors(panel.GetComponent<RectTransform>(), new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.88f));
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

    private static Text CreateLabel(
        Transform parent,
        Font font,
        string objectName,
        int fontSize,
        FontStyle style)
    {
        GameObject label = new(objectName, typeof(RectTransform), typeof(Text));
        label.transform.SetParent(parent, false);
        Text text = label.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = new Color(0.95f, 0.92f, 0.84f, 1f);
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static void SetAnchors(RectTransform transform, Vector2 minimum, Vector2 maximum)
    {
        transform.anchorMin = minimum;
        transform.anchorMax = maximum;
        transform.offsetMin = Vector2.zero;
        transform.offsetMax = Vector2.zero;
    }
}
