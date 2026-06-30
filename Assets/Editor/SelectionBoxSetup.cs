using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dựng khung kéo chuột RTS (Screen Space - Overlay) chỉ bằng một thao tác:
///   Menu Tools > UI > Build Selection Box
///
/// Tạo UI Image "SelectionBox" (pivot/anchor Bottom-Left, màu bán trong suốt) dưới Canvas Overlay,
/// gắn + nối <see cref="SelectionBoxManager"/>, và tắt khung world-space cũ trên <see cref="UnitController"/>
/// để tránh hiện hai khung. Chạy lại được nhiều lần.
/// </summary>
public static class SelectionBoxSetup
{
    private const string BoxName = "SelectionBox";
    private const string ManagerName = "SelectionSystem";

    // Xanh lá bán trong suốt (~alpha 100/255) để nhìn xuyên thấu.
    private static readonly Color BoxColor = new Color(0.4f, 1f, 0.45f, 0.39f);

    [MenuItem("Tools/UI/Build Selection Box")]
    public static void BuildSelectionBox()
    {
        Canvas canvas = FindOverlayCanvas();
        if (canvas == null)
        {
            Debug.LogError("[SelectionBoxSetup] Không tìm thấy Canvas Screen Space - Overlay trong scene.");
            return;
        }

        RectTransform box = CreateSelectionBoxImage(canvas);
        SelectionBoxManager manager = CreateManager(box, canvas);
        DisableLegacyWorldBox();

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log($"[SelectionBoxSetup] Đã dựng khung chọn '{BoxName}' + '{ManagerName}'. Nhớ lưu scene (Ctrl+S).");
    }

    private static RectTransform CreateSelectionBoxImage(Canvas canvas)
    {
        Transform existing = canvas.transform.Find(BoxName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        var go = new GameObject(BoxName, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(canvas.transform, false);
        rect.SetAsFirstSibling(); // vẽ dưới các bảng HUD khác

        // Pivot & Anchor Bottom-Left để đặt vị trí theo góc dưới-trái khung.
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        var image = go.GetComponent<Image>();
        image.color = BoxColor;
        image.raycastTarget = false; // không chặn click xuống bản đồ

        go.SetActive(false);
        return rect;
    }

    private static SelectionBoxManager CreateManager(RectTransform box, Canvas canvas)
    {
        GameObject managerGo = GameObject.Find(ManagerName) ?? new GameObject(ManagerName);
        SelectionBoxManager manager = managerGo.GetComponent<SelectionBoxManager>()
            ?? managerGo.AddComponent<SelectionBoxManager>();

        var so = new SerializedObject(manager);
        so.FindProperty("selectionBox").objectReferenceValue = box;
        so.FindProperty("canvas").objectReferenceValue = canvas;
        so.ApplyModifiedPropertiesWithoutUndo();
        return manager;
    }

    // Tắt khung world-space cũ: bỏ tham chiếu trên UnitController và ẩn object khung để không hiện hai khung.
    private static void DisableLegacyWorldBox()
    {
        UnitController controller = Object.FindFirstObjectByType<UnitController>();
        if (controller == null)
        {
            return;
        }

        var so = new SerializedObject(controller);
        SerializedProperty boxProp = so.FindProperty("selectionBoxTransform");
        var legacyBox = boxProp.objectReferenceValue as Transform;
        if (legacyBox != null)
        {
            legacyBox.gameObject.SetActive(false);
        }
        boxProp.objectReferenceValue = null;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Canvas FindOverlayCanvas()
    {
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.transform.parent == null)
            {
                return c;
            }
        }
        return Object.FindFirstObjectByType<Canvas>();
    }
}
