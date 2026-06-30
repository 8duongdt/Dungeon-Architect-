using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Hub của hệ thống xây dựng theo lưới: dựng lưới chiếm dụng khớp với map dungeon, theo dõi chuột để
/// đặt (trái chuột) hoặc phá dỡ (phải chuột) công trình. Lưới căn theo Tilemap qua
/// <see cref="TilemapVisualizer"/>; chỉ cho đặt trên ô sàn (Floor) còn trống.
/// </summary>
public class GridBuildingSystem : MonoBehaviour
{
    [Header("Tham chiếu")]
    [SerializeField]
    private DungeonManager dungeonManager;

    [SerializeField]
    private TilemapVisualizer tilemapVisualizer;

    [Header("Danh sách công trình xây được")]
    [SerializeField]
    private List<PlacedObjectTypeSO> placedObjectTypeList = new List<PlacedObjectTypeSO>();

    private GridXY<GridObject> grid;
    private PlacedObjectTypeSO activeType;

    public PlacedObjectTypeSO ActiveType => activeType;

    /// <summary>Đang cầm một loại công trình để xây hay không.</summary>
    public bool IsBuildModeActive => activeType != null;

    public int GridWidth => grid != null ? grid.Width : 0;
    public int GridHeight => grid != null ? grid.Height : 0;

    [Header("Hoàn trả khi phá dỡ")]
    [Tooltip("Tỉ lệ chi phí xây được hoàn lại khi phá công trình.")]
    [SerializeField]
    [Range(0f, 1f)]
    private float refundFraction = 0.7f;

    /// <summary>Bắn khi trạng thái xây đổi (vào/ra chế độ xây, hoặc đặt/phá) - để overlay vẽ lại.</summary>
    public event Action BuildStateChanged;

    /// <summary>Bắn khi người chơi chuột phải vào một công trình (ngoài chế độ xây) - mở Cửa sổ Trạng thái.</summary>
    public event Action<PlacedObject> ConstructInspectRequested;

    public float RefundFraction => refundFraction;

    private void OnEnable()
    {
        if (dungeonManager != null)
        {
            dungeonManager.DungeonGenerated += RebuildGrid;
        }
    }

    private void OnDisable()
    {
        if (dungeonManager != null)
        {
            dungeonManager.DungeonGenerated -= RebuildGrid;
        }
    }

    private void Update()
    {
        if (Mouse.current == null || grid == null)
        {
            return;
        }

        // Bấm vào UI (nút trên bảng điều khiển) thì không tính là click xây/phá trên bản đồ.
        if (IsPointerOverUI())
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryPlace();
        }
        else if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleRightClick();
        }
    }

    // Chuột phải: đang cầm công trình -> huỷ chế độ xây; ngược lại -> mở Cửa sổ Trạng thái nếu trúng công trình.
    private void HandleRightClick()
    {
        if (IsBuildModeActive)
        {
            ClearActiveBuildingType();
            return;
        }

        PlacedObject placedObject = GetPlacedObjectAtMouse();
        if (placedObject != null)
        {
            ConstructInspectRequested?.Invoke(placedObject);
        }
    }

    private PlacedObject GetPlacedObjectAtMouse()
    {
        GridObject gridObject = grid != null ? grid.GetGridObject(GetMouseCell()) : null;
        return gridObject != null ? gridObject.GetPlacedObject() : null;
    }

    public void SetActiveBuildingType(PlacedObjectTypeSO type)
    {
        activeType = type;
        BuildStateChanged?.Invoke();
    }

    public void ClearActiveBuildingType()
    {
        activeType = null;
        BuildStateChanged?.Invoke();
    }

    /// <summary>Ô lưới ngay dưới con trỏ chuột.</summary>
    public Vector2Int GetMouseCell()
    {
        return tilemapVisualizer.WorldToCell(MouseUtils.GetMouseWorldPosition());
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>Có thể đặt <paramref name="type"/> với ô gốc <paramref name="origin"/> hay không.</summary>
    public bool CanBuildAt(PlacedObjectTypeSO type, Vector2Int origin)
    {
        if (type == null || grid == null)
        {
            return false;
        }

        foreach (Vector2Int cell in type.GetGridPositionList(origin))
        {
            GridObject gridObject = grid.GetGridObject(cell);
            bool isCellBuildable = gridObject != null && gridObject.CanBuild() && IsFloorCell(cell);
            if (!isCellBuildable)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>Tọa độ world tâm một ô lưới - để overlay/ghost căn đúng theo Tilemap sàn.</summary>
    public Vector3 CellToWorldCenter(Vector2Int cell)
    {
        return tilemapVisualizer.CellToWorldCenter(cell);
    }

    /// <summary>Tọa độ world để đặt prefab/ghost: tâm khối footprint width x height.</summary>
    public Vector3 GetPlacementWorldPosition(PlacedObjectTypeSO type, Vector2Int origin)
    {
        float cellSize = tilemapVisualizer.CellSize;
        Vector3 blockOffset = new Vector3((type.width - 1) * 0.5f, (type.height - 1) * 0.5f, 0f) * cellSize;
        return tilemapVisualizer.CellToWorldCenter(origin) + blockOffset;
    }

    private void TryPlace()
    {
        if (activeType == null)
        {
            return;
        }

        Vector2Int origin = GetMouseCell();
        if (!CanBuildAt(activeType, origin))
        {
            Debug.Log("Cannot build here!");
            return;
        }

        if (!TrySpendBuildCost(activeType))
        {
            Debug.Log("Not enough resources to build!");
            return;
        }

        Vector3 placementWorldPosition = GetPlacementWorldPosition(activeType, origin);
        PlacedObject placedObject = PlacedObject.Create(placementWorldPosition, origin, activeType);

        foreach (Vector2Int cell in activeType.GetGridPositionList(origin))
        {
            grid.GetGridObject(cell).SetPlacedObject(placedObject);
        }

        // Đặt xong một công trình thì thoát chế độ xây - muốn xây tiếp phải chọn lại nút UI.
        // (ClearActiveBuildingType bắn BuildStateChanged -> ghost ẩn, overlay tắt.)
        ClearActiveBuildingType();
    }

    /// <summary>Đủ tài nguyên để xây loại công trình này hay không (cho HUD làm mờ ô không xây được).</summary>
    public bool CanAfford(PlacedObjectTypeSO type)
    {
        if (type == null)
        {
            return false;
        }

        ResourceManager resources = ResourceManager.Instance;
        return resources == null || resources.CanAfford(type.goldCost, type.manaCost);
    }

    // Trừ chi phí xây. Không có ResourceManager thì coi như miễn phí (cho phép xây bình thường).
    private bool TrySpendBuildCost(PlacedObjectTypeSO type)
    {
        ResourceManager resources = ResourceManager.Instance;
        if (resources == null)
        {
            return true;
        }

        if (!resources.CanAfford(type.goldCost, type.manaCost))
        {
            return false;
        }

        resources.TrySpendGold(type.goldCost);
        resources.TrySpendMana(type.manaCost);
        return true;
    }

    /// <summary>Phá một công trình cụ thể: giải phóng ô lưới, hoàn trả tài nguyên rồi huỷ object.</summary>
    public void DemolishPlaced(PlacedObject placedObject)
    {
        if (placedObject == null || grid == null)
        {
            return;
        }

        foreach (Vector2Int cell in placedObject.GetGridPositionList())
        {
            grid.GetGridObject(cell)?.ClearPlacedObject();
        }

        RefundBuildCost(placedObject.Type);
        placedObject.DestroySelf();

        // Ô vừa giải phóng cần đổi lại màu overlay nếu đang ở chế độ xây.
        BuildStateChanged?.Invoke();
    }

    /// <summary>Số Vàng hoàn lại khi phá loại công trình này.</summary>
    public int GetGoldRefund(PlacedObjectTypeSO type)
    {
        return type != null ? Mathf.RoundToInt(type.goldCost * refundFraction) : 0;
    }

    /// <summary>Số Mana hoàn lại khi phá loại công trình này.</summary>
    public int GetManaRefund(PlacedObjectTypeSO type)
    {
        return type != null ? Mathf.RoundToInt(type.manaCost * refundFraction) : 0;
    }

    private void RefundBuildCost(PlacedObjectTypeSO type)
    {
        ResourceManager resources = ResourceManager.Instance;
        if (resources == null || type == null)
        {
            return;
        }

        resources.AddGold(GetGoldRefund(type));
        resources.AddMana(GetManaRefund(type));
    }

    // Dựng lại lưới chiếm dụng (trống) khớp kích thước map mỗi khi dungeon sinh lại.
    private void RebuildGrid()
    {
        TileType[,] map = dungeonManager.CurrentMap;
        if (map == null)
        {
            return;
        }

        int width = map.GetLength(0);
        int height = map.GetLength(1);
        grid = new GridXY<GridObject>(width, height, (g, x, y) => new GridObject(g, x, y));
    }

    /// <summary>Ô có phải là sàn dungeon (đặt được nền) hay không.</summary>
    public bool IsFloorCell(Vector2Int cell)
    {
        TileType[,] map = dungeonManager.CurrentMap;
        if (map == null || grid == null || !grid.IsInBounds(cell))
        {
            return false;
        }
        return map[cell.x, cell.y] == TileType.Floor;
    }

    /// <summary>Ô đang bị một công trình chiếm dụng hay không.</summary>
    public bool IsCellOccupied(Vector2Int cell)
    {
        GridObject gridObject = grid != null ? grid.GetGridObject(cell) : null;
        return gridObject != null && !gridObject.CanBuild();
    }
}
