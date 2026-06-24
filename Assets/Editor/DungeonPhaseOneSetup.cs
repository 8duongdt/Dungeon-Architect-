using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Cửa sổ tự động dựng và liên kết toàn bộ pipeline sinh dungeon cho Phase 1:
///   - Tạo asset DungeonData (nếu chưa có)
///   - Tạo prefab Đuốc / Rương từ sprite đã slice
///   - Tìm hoặc tạo GameObject "DungeonManager", gắn DungeonManager + DungeonDecorator
///   - Tự gán mọi tham chiếu trong Inspector (TilemapVisualizer cũ, DungeonData, prefab)
///
/// Mở qua menu: Tools > Dungeon > Setup Phase One.
/// Công cụ chỉ chạy trong Editor, KHÔNG đụng vào code/logic runtime.
/// </summary>
public class DungeonPhaseOneSetup : EditorWindow
{
    private const string DataFolder = "Assets/Scripts/GeneratorMap/Data/DungeonData";
    private const string DataAssetPath = DataFolder + "/DungeonData_Phase1.asset";
    private const string PrefabFolder = "Assets/Prefabs/Decorations";
    private const string TorchPrefabPath = PrefabFolder + "/Torch.prefab";
    private const string ChestPrefabPath = PrefabFolder + "/Chest.prefab";

    private const string TorchSheetPath = "Assets/Sprites/Environment/dungeon asset/fire_animation.png";
    private const string ChestSheetPath = "Assets/Sprites/Environment/dungeon asset/doors_lever_chest_animation.png";

    private Sprite torchSprite;
    private Sprite chestSprite;
    private TilemapVisualizer tilemapVisualizer;
    private int decorationSortingOrder = 5;

    [MenuItem("Tools/Dungeon/Setup Phase One")]
    private static void Open()
    {
        GetWindow<DungeonPhaseOneSetup>("Dungeon Phase 1 Setup");
    }

    private void OnEnable()
    {
        // Gợi ý mặc định: frame 0 của mỗi sheet và TilemapVisualizer đang có trong scene.
        if (torchSprite == null)
            torchSprite = LoadSpriteByName(TorchSheetPath, "fire_animation_0");
        if (chestSprite == null)
            chestSprite = LoadSpriteByName(ChestSheetPath, "doors_lever_chest_animation_0");
        if (tilemapVisualizer == null)
            tilemapVisualizer = FindAnyObjectByType<TilemapVisualizer>();
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Kéo thả sprite Đuốc/Rương bạn muốn (frame đã slice). " +
            "Mặc định lấy frame 0 của mỗi sheet - hãy đổi nếu frame đó không phải đuốc/rương.",
            MessageType.Info);

        torchSprite = (Sprite)EditorGUILayout.ObjectField("Torch Sprite", torchSprite, typeof(Sprite), false);
        chestSprite = (Sprite)EditorGUILayout.ObjectField("Chest Sprite", chestSprite, typeof(Sprite), false);
        tilemapVisualizer = (TilemapVisualizer)EditorGUILayout.ObjectField(
            "Tilemap Visualizer", tilemapVisualizer, typeof(TilemapVisualizer), true);
        decorationSortingOrder = EditorGUILayout.IntField("Decoration Sorting Order", decorationSortingOrder);

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(torchSprite == null || chestSprite == null))
        {
            if (GUILayout.Button("Create / Wire Everything", GUILayout.Height(32)))
                RunSetup();
        }

        if (tilemapVisualizer == null)
            EditorGUILayout.HelpBox("Không tìm thấy TilemapVisualizer trong scene - hãy gán thủ công.", MessageType.Warning);
    }

    private void RunSetup()
    {
        if (tilemapVisualizer == null)
        {
            EditorUtility.DisplayDialog("Thiếu TilemapVisualizer",
                "Mở scene Phase 1 và gán TilemapVisualizer trước khi chạy.", "OK");
            return;
        }

        // 1. DungeonData asset.
        DungeonData data = CreateOrLoadDungeonData();

        // 2. Prefab đuốc / rương.
        GameObject torchPrefab = CreateDecorationPrefab(TorchPrefabPath, "Torch", torchSprite);
        GameObject chestPrefab = CreateDecorationPrefab(ChestPrefabPath, "Chest", chestSprite);

        // 3. GameObject DungeonManager + components.
        GameObject managerGO = GameObject.Find("DungeonManager");
        if (managerGO == null)
        {
            managerGO = new GameObject("DungeonManager");
            Undo.RegisterCreatedObjectUndo(managerGO, "Create DungeonManager");
        }

        DungeonManager manager = GetOrAddComponent<DungeonManager>(managerGO);
        DungeonDecorator decorator = GetOrAddComponent<DungeonDecorator>(managerGO);

        // 4. Gán tham chiếu (các field private [SerializeField] -> dùng SerializedObject theo tên).
        WireDecorator(decorator, torchPrefab, chestPrefab);
        WireManager(manager, data, tilemapVisualizer, decorator);

        EditorUtility.SetDirty(managerGO);
        EditorSceneManager.MarkSceneDirty(managerGO.scene);
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = managerGO;
        Debug.Log("[DungeonPhaseOneSetup] Hoàn tất: đã tạo prefab, DungeonData và gán mọi tham chiếu trên 'DungeonManager'.");
    }

    private DungeonData CreateOrLoadDungeonData()
    {
        var existing = AssetDatabase.LoadAssetAtPath<DungeonData>(DataAssetPath);
        if (existing != null)
            return existing;

        EnsureFolder(DataFolder);
        var data = ScriptableObject.CreateInstance<DungeonData>();
        AssetDatabase.CreateAsset(data, DataAssetPath);
        return data;
    }

    private GameObject CreateDecorationPrefab(string path, string name, Sprite sprite)
    {
        EnsureFolder(PrefabFolder);

        var temp = new GameObject(name);
        var renderer = temp.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = decorationSortingOrder; // vẽ phía trên tile sàn/tường

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
        DestroyImmediate(temp);
        return prefab;
    }

    private void WireDecorator(DungeonDecorator decorator, GameObject torchPrefab, GameObject chestPrefab)
    {
        var so = new SerializedObject(decorator);
        so.FindProperty("torchPrefab").objectReferenceValue = torchPrefab;
        so.FindProperty("chestPrefab").objectReferenceValue = chestPrefab;
        so.ApplyModifiedProperties();
    }

    private void WireManager(DungeonManager manager, DungeonData data, TilemapVisualizer visualizer, DungeonDecorator decorator)
    {
        var so = new SerializedObject(manager);
        // DungeonManager chọn map qua danh sách 'themes' (mỗi theme tự ôm DungeonData); ở đây chỉ
        // gán những tham chiếu trực tiếp mà tool tạo ra. Mỗi property đều kiểm tra null cho an toàn.
        AssignReference(so, "tilemapVisualizer", visualizer);
        AssignReference(so, "dungeonDecorator", decorator);
        so.ApplyModifiedProperties();
    }

    private static void AssignReference(SerializedObject so, string propertyName, Object value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(go);
    }

    private static Sprite LoadSpriteByName(string sheetPath, string spriteName)
    {
        Sprite fallback = null;
        foreach (var obj in AssetDatabase.LoadAllAssetRepresentationsAtPath(sheetPath))
        {
            if (obj is Sprite sprite)
            {
                fallback ??= sprite;
                if (sprite.name == spriteName)
                    return sprite;
            }
        }
        return fallback;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(folder);
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
