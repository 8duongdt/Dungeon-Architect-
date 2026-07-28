using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sương mù chiến tranh cho Phase 1, gồm HAI phần:
/// 1) Một lớp phủ MỜ (bán trong suốt) trên vùng NGOÀI tầm nhìn - vẫn thấy địa hình xuyên qua, chỉ tối
///    đi để phân biệt vùng đã/chưa có mắt (độ đục chỉnh bằng <see cref="unseenDarkness"/>).
/// 2) GIẤU hẳn hình ảnh đơn vị/công trình PHE ĐỊCH khi chúng ngoài tầm nhìn (qua <see cref="FogHideable"/>),
///    tới gần thì hiện; địch vẫn tồn tại và hành động bình thường.
///
/// "Mắt" là các <see cref="UnitFaction.ActiveUnits"/> phe Player (unit + công trình). Chỉ cần thả một
/// GameObject mang script này vào scene: tự tìm DungeonManager/TilemapVisualizer và dựng lại lớp phủ
/// theo <see cref="DungeonManager.DungeonGenerated"/>.
/// </summary>
[DisallowMultipleComponent]
public class FogOfWarController : MonoBehaviour
{
    [Header("Tham chiếu (tự tìm nếu để trống)")]
    [SerializeField] private DungeonManager dungeonManager;
    [SerializeField] private TilemapVisualizer tilemapVisualizer;

    [Header("Tầm nhìn (đơn vị world)")]
    [Tooltip("Bán kính tầm nhìn quanh mỗi unit di động phe người chơi.")]
    [SerializeField] private float unitRevealRadius = 8f;

    [Tooltip("Bán kính tầm nhìn quanh mỗi công trình phe người chơi (thường rộng hơn unit).")]
    [SerializeField] private float buildingRevealRadius = 11f;

    [Tooltip("Độ loe mềm ở mép vòng sáng (đơn vị world) để chuyển tối-sáng không bị cắt cứng.")]
    [SerializeField] private float edgeSoftness = 2.5f;

    [Header("Lớp phủ vùng chưa có tầm nhìn")]
    [Tooltip("Độ tối của vùng ngoài tầm nhìn: 0 = trong suốt (không thấy fog), 1 = đen đặc. Để thấy địa hình nên để ~0.5.")]
    [Range(0f, 1f)]
    [SerializeField] private float unseenDarkness = 0.55f;

    [SerializeField] private Color fogColor = Color.black;

    [Header("Chất lượng / hiệu năng")]
    [Tooltip("Số texel mask trên mỗi đơn vị world. Cao hơn = mép vòng mịn hơn nhưng tốn hơn.")]
    [SerializeField] private float texelsPerUnit = 1.5f;

    [Tooltip("Chu kỳ (giây) cập nhật lại theo vị trí unit/công trình.")]
    [SerializeField] private float refreshInterval = 0.1f;

    [Tooltip("Sorting order của lớp phủ - đặt cao để nằm trên mọi unit/tile.")]
    [SerializeField] private int sortingOrder = 30000;

    /// <summary>Singleton nhẹ để minimap hỏi vùng nào đang trong tầm nhìn (ẩn blip địch ngoài tầm).</summary>
    public static FogOfWarController Instance { get; private set; }

    // Ảnh chụp "mắt" phe người chơi ở nhịp làm mới gần nhất (vị trí + bán kính) để test tầm nhìn.
    private readonly List<PlayerEye> playerEyes = new List<PlayerEye>();

    private SpriteRenderer overlayRenderer;
    private Material overlayMaterial;
    private Texture2D fogTexture;
    private Color32[] pixelBuffer;
    private int textureWidth;
    private int textureHeight;
    private Rect worldBounds;
    private bool isBuilt;
    private float refreshTimer;

    private void Awake()
    {
        if (dungeonManager == null)
        {
            dungeonManager = FindAnyObjectByType<DungeonManager>();
        }
        if (tilemapVisualizer == null)
        {
            tilemapVisualizer = FindAnyObjectByType<TilemapVisualizer>();
        }
    }

    private void OnEnable()
    {
        Instance = this;

        if (dungeonManager == null)
        {
            return;
        }

        dungeonManager.DungeonGenerated += HandleDungeonGenerated;
        if (dungeonManager.CurrentMap != null)
        {
            HandleDungeonGenerated();
        }
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (dungeonManager != null)
        {
            dungeonManager.DungeonGenerated -= HandleDungeonGenerated;
        }

        // Rời scene / tắt fog thì hiện lại toàn bộ địch để không kẹt ẩn vĩnh viễn.
        ShowAllEnemies();
    }

    private void OnDestroy()
    {
        if (overlayRenderer != null && overlayRenderer.sprite != null)
        {
            Destroy(overlayRenderer.sprite);
        }
        if (fogTexture != null)
        {
            Destroy(fogTexture);
        }
        if (overlayMaterial != null)
        {
            Destroy(overlayMaterial);
        }
    }

    private void Update()
    {
        if (!isBuilt)
        {
            return;
        }

        refreshTimer -= Time.deltaTime;
        if (refreshTimer > 0f)
        {
            return;
        }

        refreshTimer = refreshInterval;
        RefreshAll();
    }

    /// <summary>
    /// Vị trí world này có đang trong tầm nhìn của ít nhất một mắt phe người chơi không. Dùng cho
    /// minimap ẩn blip địch ngoài tầm nhìn.
    /// </summary>
    public bool IsRevealed(Vector3 worldPosition)
    {
        foreach (PlayerEye eye in playerEyes)
        {
            if ((worldPosition - eye.Position).sqrMagnitude <= eye.Radius * eye.Radius)
            {
                return true;
            }
        }

        return false;
    }

    // Map vừa sinh (hoặc sinh lại) -> tính lại khung world và dựng lớp phủ khớp kích thước mới.
    private void HandleDungeonGenerated()
    {
        TileType[,] map = dungeonManager != null ? dungeonManager.CurrentMap : null;
        if (map == null || tilemapVisualizer == null)
        {
            return;
        }

        worldBounds = tilemapVisualizer.GetWorldBounds(map.GetLength(0), map.GetLength(1));
        BuildOverlay();
        RefreshAll();
    }

    private void RefreshAll()
    {
        CollectPlayerEyes();
        RefreshFogTexture();
        RefreshEnemyVisibility();
    }

    // Gom mọi mắt phe người chơi (unit + công trình) kèm bán kính nhìn tương ứng.
    private void CollectPlayerEyes()
    {
        playerEyes.Clear();
        foreach (UnitFaction faction in UnitFaction.ActiveUnits)
        {
            if (faction == null || faction.Faction != FactionType.Player)
            {
                continue;
            }

            float radius = IsBuilding(faction) ? buildingRevealRadius : unitRevealRadius;
            playerEyes.Add(new PlayerEye(faction.transform.position, radius));
        }
    }

    // Ẩn/hiện hình ảnh địch theo việc có nằm trong tầm nhìn hay không.
    private void RefreshEnemyVisibility()
    {
        foreach (UnitFaction faction in UnitFaction.ActiveUnits)
        {
            if (faction == null || faction.Faction == FactionType.Player)
            {
                continue;
            }

            GetHideable(faction).SetVisible(IsRevealed(faction.transform.position));
        }
    }

    private void BuildOverlay()
    {
        textureWidth = Mathf.Max(8, Mathf.RoundToInt(worldBounds.width * texelsPerUnit));
        textureHeight = Mathf.Max(8, Mathf.RoundToInt(worldBounds.height * texelsPerUnit));

        bool needsNewTexture = fogTexture == null
            || fogTexture.width != textureWidth
            || fogTexture.height != textureHeight;
        if (needsNewTexture)
        {
            // Texture cũ (map sinh lại kích thước khác) phải Destroy tay - Unity không tự GC native.
            if (fogTexture != null)
            {
                Destroy(fogTexture);
            }

            fogTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            pixelBuffer = new Color32[textureWidth * textureHeight];
        }

        EnsureOverlayRenderer();

        if (overlayRenderer.sprite != null)
        {
            Destroy(overlayRenderer.sprite);
        }

        var spriteRect = new Rect(0f, 0f, textureWidth, textureHeight);
        overlayRenderer.sprite = Sprite.Create(fogTexture, spriteRect, new Vector2(0.5f, 0.5f), texelsPerUnit);
        overlayRenderer.transform.position = new Vector3(worldBounds.center.x, worldBounds.center.y, 0f);
        isBuilt = true;
    }

    private void EnsureOverlayRenderer()
    {
        if (overlayRenderer != null)
        {
            return;
        }

        var overlayObject = new GameObject("FogOverlay");
        overlayObject.transform.SetParent(transform, false);
        overlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
        // Shader unlit để lớp phủ không bị đèn 2D làm sáng/tối lệch - fog luôn đúng độ đậm đã tính.
        overlayMaterial = new Material(Shader.Find("Sprites/Default"));
        overlayRenderer.sharedMaterial = overlayMaterial;
        overlayRenderer.sortingOrder = sortingOrder;
    }

    // Tô kín màu fog ở độ đục "chưa thấy" rồi đục vòng trong suốt tại từng mắt phe người chơi.
    private void RefreshFogTexture()
    {
        if (!isBuilt || pixelBuffer == null)
        {
            return;
        }

        byte unseenAlpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(unseenDarkness) * 255f);
        Color32 unseenFog = fogColor;
        unseenFog.a = unseenAlpha;
        for (int i = 0; i < pixelBuffer.Length; i++)
        {
            pixelBuffer[i] = unseenFog;
        }

        foreach (PlayerEye eye in playerEyes)
        {
            CarveReveal(eye.Position, eye.Radius, unseenAlpha);
        }

        fogTexture.SetPixels32(pixelBuffer);
        fogTexture.Apply(false);
    }

    // Đục vòng sáng: alpha giảm dần từ 0 (tâm, trong suốt) tới unseenAlpha (ngoài mép) - lấy min để
    // vùng được nhiều mắt soi lấy alpha thấp nhất (sáng nhất).
    private void CarveReveal(Vector3 worldPosition, float radius, byte unseenAlpha)
    {
        float centerX = (worldPosition.x - worldBounds.xMin) / worldBounds.width * textureWidth;
        float centerY = (worldPosition.y - worldBounds.yMin) / worldBounds.height * textureHeight;
        float radiusTexels = radius * texelsPerUnit;
        float softTexels = Mathf.Max(0.001f, edgeSoftness * texelsPerUnit);

        int minX = Mathf.Max(0, Mathf.FloorToInt(centerX - radiusTexels - softTexels));
        int maxX = Mathf.Min(textureWidth - 1, Mathf.CeilToInt(centerX + radiusTexels + softTexels));
        int minY = Mathf.Max(0, Mathf.FloorToInt(centerY - radiusTexels - softTexels));
        int maxY = Mathf.Min(textureHeight - 1, Mathf.CeilToInt(centerY + radiusTexels + softTexels));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float distance = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                float fogAmount = Mathf.InverseLerp(radiusTexels, radiusTexels + softTexels, distance);
                byte alpha = (byte)(fogAmount * unseenAlpha);

                int index = y * textureWidth + x;
                if (alpha < pixelBuffer[index].a)
                {
                    Color32 pixel = pixelBuffer[index];
                    pixel.a = alpha;
                    pixelBuffer[index] = pixel;
                }
            }
        }
    }

    private static FogHideable GetHideable(UnitFaction faction)
    {
        FogHideable hideable = faction.GetComponent<FogHideable>();
        return hideable != null ? hideable : faction.gameObject.AddComponent<FogHideable>();
    }

    private static void ShowAllEnemies()
    {
        foreach (UnitFaction faction in UnitFaction.ActiveUnits)
        {
            if (faction == null || faction.Faction == FactionType.Player)
            {
                continue;
            }

            FogHideable hideable = faction.GetComponent<FogHideable>();
            if (hideable != null)
            {
                hideable.SetVisible(true);
            }
        }
    }

    private static bool IsBuilding(UnitFaction faction)
    {
        return faction.GetComponent<BuildingDurability>() != null;
    }

    private readonly struct PlayerEye
    {
        public readonly Vector3 Position;
        public readonly float Radius;

        public PlayerEye(Vector3 position, float radius)
        {
            Position = position;
            Radius = radius;
        }
    }
}
