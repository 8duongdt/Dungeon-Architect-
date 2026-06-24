using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Mini-map kiểu RTS: vẽ nền địa hình (texture sinh từ lưới dungeon), chấm unit theo phe
/// (xanh đồng minh / đỏ kẻ địch), ô khung viewport của camera chính, và cho click/kéo trên
/// mini-map để nhảy camera chính tới vị trí tương ứng.
///
/// Đặt component này trên GameObject UI có một Graphic raycast-target phủ vùng mini-map
/// (vd. RawImage nền bật Raycast Target) để nhận sự kiện con trỏ.
/// </summary>
public class MinimapController : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("Nguồn dữ liệu map")]
    [SerializeField] private DungeonManager dungeonManager;
    [SerializeField] private TilemapVisualizer tilemapVisualizer;

    [Header("Camera chính")]
    [SerializeField] private RTSCamera mainRtsCamera;
    [SerializeField] private Camera mainCamera;

    [Header("Thành phần UI")]
    [Tooltip("Ảnh nền hiển thị texture địa hình.")]
    [SerializeField] private RawImage terrainImage;
    [Tooltip("Khung chứa các chấm unit; chồng khít vùng nền, pivot/anchor ở GIỮA.")]
    [SerializeField] private RectTransform blipContainer;
    [Tooltip("Ô khung thể hiện vùng camera chính đang nhìn.")]
    [SerializeField] private RectTransform viewportBox;

    [Header("Màu sắc")]
    [SerializeField] private MinimapPalette palette;
    [SerializeField] private Color allyBlipColor = new Color(0.30f, 0.95f, 0.30f, 1f);
    [SerializeField] private Color enemyBlipColor = new Color(0.95f, 0.25f, 0.25f, 1f);

    [Header("Chấm unit")]
    [SerializeField] private Vector2 blipSize = new Vector2(6f, 6f);
    [Tooltip("Sprite tròn cho chấm; bỏ trống sẽ dùng ô vuông mặc định của UI.")]
    [SerializeField] private Sprite blipSprite;

    private readonly List<Image> blipPool = new();
    private Rect mapWorldRect;
    private Texture2D terrainTexture;
    private bool hasMap;

    private void OnEnable()
    {
        if (dungeonManager != null)
        {
            dungeonManager.DungeonGenerated += HandleDungeonGenerated;
        }

        // Map có thể đã được sinh trước khi mini-map bật -> dựng lại ngay nếu có sẵn.
        if (dungeonManager != null && dungeonManager.CurrentMap != null)
        {
            HandleDungeonGenerated();
        }
    }

    private void OnDisable()
    {
        if (dungeonManager != null)
        {
            dungeonManager.DungeonGenerated -= HandleDungeonGenerated;
        }
    }

    private void Update()
    {
        if (!hasMap)
        {
            return;
        }

        RefreshBlips();
        UpdateViewportBox();
    }

    // ----- Dựng nền địa hình khi có map mới -----

    private void HandleDungeonGenerated()
    {
        TileType[,] map = dungeonManager.CurrentMap;
        if (map == null)
        {
            return;
        }

        RebuildTerrainTexture(map);
        mapWorldRect = tilemapVisualizer.GetWorldBounds(map.GetLength(0), map.GetLength(1));
        hasMap = true;
    }

    private void RebuildTerrainTexture(TileType[,] map)
    {
        if (terrainTexture != null)
        {
            Destroy(terrainTexture);
        }

        terrainTexture = MinimapTextureBuilder.Build(map, palette);
        terrainImage.texture = terrainTexture;
    }

    // ----- Chấm unit -----

    private void RefreshBlips()
    {
        IReadOnlyList<UnitFaction> units = MinimapRegistry.Units;
        EnsureBlipPool(units.Count);

        for (int i = 0; i < units.Count; i++)
        {
            UnitFaction unit = units[i];
            Image blip = blipPool[i];

            if (unit == null)
            {
                blip.enabled = false;
                continue;
            }

            bool isAlly = unit.Faction == FactionType.Player;
            blip.enabled = true;
            blip.color = isAlly ? allyBlipColor : enemyBlipColor;
            blip.rectTransform.anchoredPosition = WorldToContainerLocal(unit.transform.position);
        }

        HideUnusedBlips(units.Count);
    }

    private void EnsureBlipPool(int requiredCount)
    {
        while (blipPool.Count < requiredCount)
        {
            blipPool.Add(CreateBlip());
        }
    }

    private Image CreateBlip()
    {
        var blipObject = new GameObject("Blip", typeof(RectTransform), typeof(Image));
        var rect = blipObject.GetComponent<RectTransform>();
        rect.SetParent(blipContainer, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = blipSize;

        var image = blipObject.GetComponent<Image>();
        image.sprite = blipSprite;
        image.raycastTarget = false;
        return image;
    }

    private void HideUnusedBlips(int activeCount)
    {
        for (int i = activeCount; i < blipPool.Count; i++)
        {
            blipPool[i].enabled = false;
        }
    }

    // ----- Ô khung viewport của camera chính -----

    private void UpdateViewportBox()
    {
        if (viewportBox == null || mainCamera == null)
        {
            return;
        }

        float halfHeight = mainCamera.orthographicSize;
        float halfWidth = halfHeight * mainCamera.aspect;
        Vector2 containerSize = blipContainer.rect.size;

        viewportBox.anchoredPosition = WorldToContainerLocal(mainCamera.transform.position);
        viewportBox.sizeDelta = new Vector2(
            (halfWidth * 2f / mapWorldRect.width) * containerSize.x,
            (halfHeight * 2f / mapWorldRect.height) * containerSize.y);
    }

    // ----- Click/kéo để di chuyển camera chính -----

    public void OnPointerDown(PointerEventData eventData)
    {
        MoveCameraToPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        MoveCameraToPointer(eventData);
    }

    private void MoveCameraToPointer(PointerEventData eventData)
    {
        if (!hasMap || mainRtsCamera == null)
        {
            return;
        }

        bool isInsideMinimap = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            blipContainer, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
        if (!isInsideMinimap)
        {
            return;
        }

        mainRtsCamera.MoveTo(ContainerLocalToWorld(localPoint));
    }

    // ----- Chuyển đổi toạ độ world <-> khung mini-map (pivot/anchor giữa) -----

    private Vector2 WorldToContainerLocal(Vector3 worldPosition)
    {
        Vector2 containerSize = blipContainer.rect.size;
        float normalizedX = (worldPosition.x - mapWorldRect.xMin) / mapWorldRect.width;
        float normalizedY = (worldPosition.y - mapWorldRect.yMin) / mapWorldRect.height;
        return new Vector2(
            (normalizedX - 0.5f) * containerSize.x,
            (normalizedY - 0.5f) * containerSize.y);
    }

    private Vector2 ContainerLocalToWorld(Vector2 localPoint)
    {
        Vector2 containerSize = blipContainer.rect.size;
        float normalizedX = localPoint.x / containerSize.x + 0.5f;
        float normalizedY = localPoint.y / containerSize.y + 0.5f;
        return new Vector2(
            mapWorldRect.xMin + normalizedX * mapWorldRect.width,
            mapWorldRect.yMin + normalizedY * mapWorldRect.height);
    }
}
