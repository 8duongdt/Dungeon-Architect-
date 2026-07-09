using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Dựng và nối dây màn cốt truyện mở đầu (StoryIntro) chỉ bằng một thao tác:
///   Menu Tools > Story > Setup Story Intro
///
/// Tạo scene mới với Canvas Overlay, ảnh nền toàn màn hình, dải chữ kể chuyện ở dưới, nút Continue
/// (góc dưới phải) và Skip (góc trên phải), màn trích dẫn mở đầu, rồi gán StoryIntroController và
/// 3 ảnh minh hoạ (1/2/3.jpeg) kèm nội dung tiếng Anh mặc định. Lưu scene và thêm vào Build Settings
/// giữa MainMenu và Phase 1. Chạy lại được nhiều lần (dựng lại từ đầu).
/// </summary>
public static class StoryIntroSetup
{
    private const string ScenePath = "Assets/Scenes/StoryIntro.unity";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameplaySceneName = "Lobby";

    private static readonly string[] StorySpritePaths =
    {
        "Assets/Sprites/1.jpeg",
        "Assets/Sprites/2.jpeg",
        "Assets/Sprites/3.jpeg",
    };

    private static readonly string[] StoryNarration =
    {
        "A thousand years ago, you were no Demon Lord. You were Alaric — the greatest Paladin who " +
        "ever lived, leading six chosen heroes into the deepest abyss to destroy the old Demon Lord.",

        "And the Demon Lord fell. The abyss went silent. The world was saved, and victory belonged " +
        "to you and your companions.",

        "But greed poisons even the bravest hearts. Fearing your power, your six companions struck " +
        "you down at the heart of the Dungeon Core. They branded you 'the one who surrendered to " +
        "darkness,' sealed your soul within the core, and offered your body as a sacrifice — fuel to " +
        "sustain humankind's prosperity for a thousand years.",
    };

    private const string QuoteLine = "Every monster was once someone's hero.";
    private const string QuoteSubLine = "— from the sealed chronicles of the Dungeon Core";

    private static readonly Color DarkPanel = new Color(0f, 0f, 0f, 0.72f);
    private static readonly Color QuoteBackground = new Color(0.02f, 0.02f, 0.035f, 1f);
    private static readonly Color ButtonTint = new Color(0.12f, 0.1f, 0.14f, 0.85f);
    private static readonly Color NarrationColor = new Color(0.95f, 0.93f, 0.86f);
    private static readonly Color QuoteColor = new Color(0.96f, 0.9f, 0.72f);

    [MenuItem("Tools/Story/Setup Story Intro")]
    public static void SetupStoryIntro()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        EnsureCamera();
        EnsureEventSystem();
        Canvas canvas = CreateCanvas();
        RectTransform root = (RectTransform)canvas.transform;

        Image backgroundArt = BuildBackgroundArt(root);
        BuildNarrationBand(root, out CanvasGroup textBand, out TMP_Text narrationText);
        CanvasGroup fadeOverlay = BuildFadeOverlay(root);
        BuildQuoteScreen(root, out GameObject quoteScreen, out TMP_Text quoteText, out TMP_Text quoteSubText);
        Button skipButton = BuildSkipButton(root);
        Button continueButton = BuildContinueButton(root);

        StoryIntroController controller = canvas.gameObject.AddComponent<StoryIntroController>();
        WireController(controller, backgroundArt, quoteScreen, quoteText, quoteSubText,
            textBand, narrationText, continueButton, skipButton, fadeOverlay);

        SaveSceneAndRegister(scene);
        Debug.Log("[StoryIntroSetup] Đã dựng màn cốt truyện StoryIntro và thêm vào Build Settings.");
    }

    // ---------------------------------------------------------------- Scene scaffolding

    private static void EnsureCamera()
    {
        if (Object.FindFirstObjectByType<Camera>() != null)
        {
            return;
        }
        var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        go.tag = "MainCamera";
        var camera = go.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
        go.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static Canvas CreateCanvas()
    {
        var go = new GameObject("StoryIntroCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    // ---------------------------------------------------------------- UI regions

    private static Image BuildBackgroundArt(RectTransform parent)
    {
        RectTransform rect = CreateFullscreenRect("BackgroundArt", parent);
        Image image = AddImage(rect, LoadStorySprite(0), Color.white);
        image.preserveAspect = true;
        return image;
    }

    private static void BuildNarrationBand(RectTransform parent, out CanvasGroup band, out TMP_Text narration)
    {
        RectTransform rect = CreateRect("NarrationBand", parent,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 0f), new Vector2(0f, 320f));
        AddImage(rect, null, DarkPanel);
        band = rect.gameObject.AddComponent<CanvasGroup>();
        band.alpha = 0f;

        narration = CreateText("Narration", rect, string.Empty, 34, TextAlignmentOptions.TopLeft, NarrationColor);
        SetStretch((RectTransform)narration.transform, new Vector2(90f, 100f), new Vector2(-320f, -40f));
        narration.textWrappingMode = TextWrappingModes.Normal;
    }

    private static CanvasGroup BuildFadeOverlay(RectTransform parent)
    {
        RectTransform rect = CreateFullscreenRect("FadeOverlay", parent);
        AddImage(rect, null, Color.black);
        CanvasGroup group = rect.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = false;
        return group;
    }

    private static void BuildQuoteScreen(RectTransform parent, out GameObject quoteScreen,
        out TMP_Text quoteText, out TMP_Text quoteSubText)
    {
        RectTransform rect = CreateFullscreenRect("QuoteScreen", parent);
        // Nền không nhận raycast để click vẫn lọt xuống nút Continue nằm dưới trong hierarchy.
        AddImage(rect, null, QuoteBackground).raycastTarget = false;
        quoteScreen = rect.gameObject;

        quoteText = CreateText("Quote", rect, QuoteLine, 52, TextAlignmentOptions.Center, QuoteColor);
        quoteText.fontStyle = FontStyles.Italic;
        quoteText.textWrappingMode = TextWrappingModes.Normal;
        SetAnchored((RectTransform)quoteText.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1200f, 260f));

        quoteSubText = CreateText("QuoteSub", rect, QuoteSubLine, 28, TextAlignmentOptions.Center,
            new Color(0.75f, 0.72f, 0.66f));
        SetAnchored((RectTransform)quoteSubText.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(1000f, 60f));
    }

    private static Button BuildSkipButton(RectTransform parent)
    {
        return CreateLabeledButton("SkipButton", parent, "Skip",
            new Vector2(1f, 1f), new Vector2(-32f, -32f), new Vector2(150f, 60f), 26);
    }

    private static Button BuildContinueButton(RectTransform parent)
    {
        return CreateLabeledButton("ContinueButton", parent, "Continue »",
            new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(260f, 74f), 30);
    }

    private static void WireController(StoryIntroController controller, Image backgroundArt,
        GameObject quoteScreen, TMP_Text quoteText, TMP_Text quoteSubText,
        CanvasGroup textBand, TMP_Text narrationText, Button continueButton, Button skipButton,
        CanvasGroup fadeOverlay)
    {
        SetSerialized(controller, so =>
        {
            so.FindProperty("backgroundArt").objectReferenceValue = backgroundArt;
            so.FindProperty("quoteScreen").objectReferenceValue = quoteScreen;
            so.FindProperty("quoteText").objectReferenceValue = quoteText;
            so.FindProperty("quoteSubText").objectReferenceValue = quoteSubText;
            so.FindProperty("textBand").objectReferenceValue = textBand;
            so.FindProperty("narrationText").objectReferenceValue = narrationText;
            so.FindProperty("continueButton").objectReferenceValue = continueButton;
            so.FindProperty("skipButton").objectReferenceValue = skipButton;
            so.FindProperty("fadeOverlay").objectReferenceValue = fadeOverlay;
            so.FindProperty("quoteLine").stringValue = QuoteLine;
            so.FindProperty("quoteSubLine").stringValue = QuoteSubLine;
            so.FindProperty("gameplaySceneName").stringValue = GameplaySceneName;

            SerializedProperty pages = so.FindProperty("pages");
            pages.arraySize = StoryNarration.Length;
            for (int i = 0; i < StoryNarration.Length; i++)
            {
                SerializedProperty page = pages.GetArrayElementAtIndex(i);
                page.FindPropertyRelative("image").objectReferenceValue = LoadStorySprite(i);
                page.FindPropertyRelative("narration").stringValue = StoryNarration[i];
            }
        });
    }

    // ---------------------------------------------------------------- Save & Build Settings

    private static void SaveSceneAndRegister(Scene scene)
    {
        EnsureFolder("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        RegisterInBuildSettings();
    }

    private static void EnsureFolder(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }
    }

    private static void RegisterInBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == ScenePath))
        {
            return;
        }

        var storyScene = new EditorBuildSettingsScene(ScenePath, true);
        int mainMenuIndex = scenes.FindIndex(s => s.path == MainMenuScenePath);
        if (mainMenuIndex >= 0)
        {
            scenes.Insert(mainMenuIndex + 1, storyScene);
        }
        else
        {
            scenes.Add(storyScene);
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ---------------------------------------------------------------- Sprite loading

    private static Sprite LoadStorySprite(int index)
    {
        if (index < 0 || index >= StorySpritePaths.Length)
        {
            return null;
        }

        // Ảnh nhập ở chế độ Sprite "Multiple" nên phải lấy sub-sprite, không dùng LoadAssetAtPath<Sprite>.
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(StorySpritePaths[index]).OfType<Sprite>().FirstOrDefault();
        if (sprite == null)
        {
            Debug.LogWarning($"[StoryIntroSetup] Không tìm thấy sprite tại {StorySpritePaths[index]}");
        }
        return sprite;
    }

    // ---------------------------------------------------------------- UI helpers (theo GameplayHudSetup)

    private static RectTransform CreateFullscreenRect(string name, Transform parent)
    {
        RectTransform rect = CreateRect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        SetStretch(rect, Vector2.zero, Vector2.zero);
        return rect;
    }

    private static Button CreateLabeledButton(string name, Transform parent, string label,
        Vector2 anchor, Vector2 anchoredPos, Vector2 size, float fontSize)
    {
        RectTransform rect = CreateRect(name, parent, anchor, anchor, anchor, anchoredPos, size);
        Image image = AddImage(rect, null, ButtonTint);
        image.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        TMP_Text text = CreateText("Label", rect, label, fontSize, TextAlignmentOptions.Center, Color.white);
        SetStretch((RectTransform)text.transform, Vector2.zero, Vector2.zero);
        return button;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    private static Image AddImage(RectTransform rect, Sprite sprite, Color color)
    {
        Image image = rect.gameObject.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, float fontSize,
        TextAlignmentOptions alignment, Color color)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 60f));
        var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        return tmp;
    }

    private static void SetStretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void SetAnchored(RectTransform rect, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
    }

    private static void SetSerialized(Object target, System.Action<SerializedObject> apply)
    {
        var so = new SerializedObject(target);
        apply(so);
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
