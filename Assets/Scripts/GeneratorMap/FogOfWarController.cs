using UnityEngine;

/// <summary>
/// Sương mù chiến tranh (fog of war) kiểu Đế Chế cho Phase 1: phủ toàn map một MÀN CHE ĐEN, chỉ
/// vén sáng quanh unit/công trình PHE NGƯỜI CHƠI; rời đi thì vùng đó tối trở lại (không lưu "đã
/// khám phá"). Màn che vẽ TRÊN mọi thứ (sorting order cao) nên vùng tối che khuất luôn kẻ địch và
/// địa hình - phải có mắt (unit/công trình) mới thấy.
///
/// Cách hoạt động: giữ một texture mask che toàn khung map (lấy từ
/// <see cref="TilemapVisualizer.GetWorldBounds"/>). Mỗi nhịp làm mới, tô kín màu fog rồi "đục" các
/// vòng tròn trong suốt tại vị trí từng "con mắt" (<see cref="UnitFaction.ActiveUnits"/> lọc phe
/// Player). Tự tìm DungeonManager/TilemapVisualizer nếu chưa gán, và dựng lại theo sự kiện
/// <see cref="DungeonManager.DungeonGenerated"/> nên chỉ cần thả GameObject có script này vào scene.
/// </summary>
[DisallowMultipleComponent]
public class FogOfWarController : MonoBehaviour
{
    [Header("Tham chiếu (tự tìm nếu để trống)")]
    [SerializeField] private DungeonManager dungeonManager;
    [SerializeField] private TilemapVisualizer tilemapVisualizer;

    [Header("Tầm soi sáng (đơn vị world)")]
    [Tooltip("Bán kính vén sương quanh mỗi unit di động phe người chơi.")]
    [SerializeField] private float unitRevealRadius = 8f;

    [Tooltip("Bán kính vén sương quanh mỗi công trình phe người chơi (thường rộng hơn unit).")]
    [SerializeField] private float buildingRevealRadius = 11f;

    [Tooltip("Độ loe mềm ở mép vòng sáng (đơn vị world) để chuyển tối-sáng không bị cắt cứng.")]
    [SerializeField] private float edgeSoftness = 2.5f;

    [Header("Chất lượng / hiệu năng")]
    [Tooltip("Số texel mask trên mỗi đơn vị world. Cao hơn = mép vòng mịn hơn nhưng tốn hơn.")]
    [SerializeField] private float texelsPerUnit = 1.5f;

    [Tooltip("Chu kỳ (giây) cập nhật lại mask theo vị trí unit/công trình.")]
    [SerializeField] private float refreshInterval = 0.1f;

    [Tooltip("Sorting order của màn che - đặt cao để phủ trên mọi unit/tile.")]
    [SerializeField] private int sortingOrder = 30000;

    [SerializeField] private Color fogColor = Color.black;

    // Ngưỡng alpha (0..255) coi một ô là "đã được soi": alpha thấp hơn = đủ sáng để minimap hiện blip địch.
    private const byte RevealedAlphaThreshold = 200;

    /// <summary>Singleton nhẹ để minimap hỏi vùng nào đã được soi (ẩn blip địch trong vùng tối).</summary>
    public static FogOfWarController Instance { get; private set; }

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
    }

    /// <summary>
    /// Vị trí world này có đang được soi sáng không (nằm trong vòng sáng của unit/công trình người
    /// chơi). Dùng cho minimap ẩn blip địch ở vùng tối. Chưa dựng fog -> coi như thấy (degrade an toàn).
    /// </summary>
    public bool IsRevealed(Vector3 worldPosition)
    {
        if (!isBuilt || pixelBuffer == null)
        {
            return true;
        }

        int px = Mathf.FloorToInt((worldPosition.x - worldBounds.xMin) / worldBounds.width * textureWidth);
        int py = Mathf.FloorToInt((worldPosition.y - worldBounds.yMin) / worldBounds.height * textureHeight);
        if (px < 0 || py < 0 || px >= textureWidth || py >= textureHeight)
        {
            return true;
        }

        return pixelBuffer[py * textureWidth + px].a < RevealedAlphaThreshold;
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
        RefreshFog();
    }

    // Map vừa sinh (hoặc sinh lại) -> tính lại khung world và dựng màn che khớp kích thước mới.
    private void HandleDungeonGenerated()
    {
        TileType[,] map = dungeonManager != null ? dungeonManager.CurrentMap : null;
        if (map == null || tilemapVisualizer == null)
        {
            return;
        }

        worldBounds = tilemapVisualizer.GetWorldBounds(map.GetLength(0), map.GetLength(1));
        BuildOverlay();
        RefreshFog();
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
            // Texture cũ (map sinh lại với kích thước khác) phải Destroy tay - Unity không tự GC
            // tài nguyên native, để nguyên sẽ leak mỗi lần regenerate.
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

        // Sprite cũ cũng là tài nguyên tạo runtime - Destroy trước khi gán sprite mới.
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
        // Shader unlit để màn che không bị đèn 2D làm sáng/tối lệch - fog luôn đúng độ đậm đã tính.
        overlayMaterial = new Material(Shader.Find("Sprites/Default"));
        overlayRenderer.sharedMaterial = overlayMaterial;
        overlayRenderer.sortingOrder = sortingOrder;
    }

    // Dọn mọi tài nguyên tạo runtime (sprite/texture/material) khi controller bị hủy cùng scene.
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

    // Tô kín màu fog rồi đục vòng sáng tại từng con mắt phe người chơi (union: texel nào được BẤT KỲ
    // con mắt nào soi tới thì lấy alpha thấp nhất = sáng nhất).
    private void RefreshFog()
    {
        if (!isBuilt || pixelBuffer == null)
        {
            return;
        }

        Color32 opaqueFog = fogColor;
        opaqueFog.a = 255;
        for (int i = 0; i < pixelBuffer.Length; i++)
        {
            pixelBuffer[i] = opaqueFog;
        }

        foreach (UnitFaction faction in UnitFaction.ActiveUnits)
        {
            if (faction == null || faction.Faction != FactionType.Player)
            {
                continue;
            }

            float radius = IsBuilding(faction) ? buildingRevealRadius : unitRevealRadius;
            CarveReveal(faction.transform.position, radius);
        }

        fogTexture.SetPixels32(pixelBuffer);
        fogTexture.Apply(false);
    }

    private static bool IsBuilding(UnitFaction faction)
    {
        return faction.GetComponent<BuildingDurability>() != null;
    }

    private void CarveReveal(Vector3 worldPosition, float radius)
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
                byte alpha = (byte)(fogAmount * 255f);

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
}
