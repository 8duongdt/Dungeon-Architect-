using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Lớp C# thuần sinh layout dungeon "Room-First" (BSP chia phòng + hành lang nối).
/// CHỈ tính toán layout lưới (TileType[,]) - KHÔNG chứa MonoBehaviour, không vẽ tile,
/// không spawn prefab. Lớp vẽ/đặt vật ở nơi khác đọc kết quả này.
/// </summary>
public class RoomFirstGenerator
{
    private readonly DungeonData data;
    private readonly List<RectInt> rooms = new();
    private TileType[,] map;

    /// <summary>Danh sách các phòng đã đặt - dùng để sinh cổng/portal sau khi Generate().</summary>
    public IReadOnlyList<RectInt> Rooms => rooms;

    public RoomFirstGenerator(DungeonData data)
    {
        this.data = data;
    }

    /// <summary>Sinh và trả về lưới bản đồ hoàn chỉnh: chia phòng BSP -> nối hành lang.</summary>
    public TileType[,] Generate()
    {
        map = CreateWallGrid();
        rooms.Clear();

        var bspBounds = new BoundsInt(Vector3Int.zero,
            new Vector3Int(data.mapWidth, data.mapHeight, 0));
        List<BoundsInt> bspRooms = ProceduralGenerationAlgorithms.BinarySpacePartitioning(
            bspBounds, data.roomFirstMinRoomWidth, data.roomFirstMinRoomHeight);

        List<Vector2Int> roomCenters = CarveRooms(bspRooms);
        ConnectRooms(roomCenters);

        return map;
    }

    private TileType[,] CreateWallGrid()
    {
        var grid = new TileType[data.mapWidth, data.mapHeight];
        for (int x = 0; x < data.mapWidth; x++)
            for (int y = 0; y < data.mapHeight; y++)
                grid[x, y] = TileType.Wall;
        return grid;
    }

    private List<Vector2Int> CarveRooms(List<BoundsInt> bspRooms)
    {
        var centers = new List<Vector2Int>(bspRooms.Count);
        foreach (BoundsInt bspRoom in bspRooms)
        {
            RectInt room = CarveRoom(bspRoom);
            rooms.Add(room);
            centers.Add(RoomCenter(room));
        }
        return centers;
    }

    private RectInt CarveRoom(BoundsInt bspBounds)
    {
        int offset = data.roomFirstOffset;
        var room = new RectInt(
            bspBounds.xMin + offset,
            bspBounds.yMin + offset,
            bspBounds.size.x - offset * 2,
            bspBounds.size.y - offset * 2);

        for (int x = room.xMin; x < room.xMax; x++)
            for (int y = room.yMin; y < room.yMax; y++)
                SetFloor(x, y);

        return room;
    }

    private void ConnectRooms(List<Vector2Int> roomCenters)
    {
        if (roomCenters.Count < 2)
            return;

        Vector2Int currentCenter = roomCenters[Random.Range(0, roomCenters.Count)];
        roomCenters.Remove(currentCenter);

        while (roomCenters.Count > 0)
        {
            Vector2Int closest = FindClosestCenter(currentCenter, roomCenters);
            roomCenters.Remove(closest);
            CarveCorridor(currentCenter, closest);
            currentCenter = closest;
        }
    }

    private void CarveCorridor(Vector2Int from, Vector2Int to)
    {
        List<Vector2Int> path = BuildCorridorPath(from, to);
        List<Vector2Int> widened = WidenCorridor(path);
        foreach (Vector2Int cell in widened)
            SetFloor(cell.x, cell.y);
    }

    private List<Vector2Int> BuildCorridorPath(Vector2Int from, Vector2Int to)
    {
        var path = new List<Vector2Int>();
        var pos = from;
        path.Add(pos);

        while (pos.y != to.y)
        {
            pos += to.y > pos.y ? Vector2Int.up : Vector2Int.down;
            path.Add(pos);
        }
        while (pos.x != to.x)
        {
            pos += to.x > pos.x ? Vector2Int.right : Vector2Int.left;
            path.Add(pos);
        }
        return path;
    }

    // Mở rộng hành lang thành bàn chải 3x3 - hành lang đủ rộng để nhân vật đi cạnh nhau.
    private List<Vector2Int> WidenCorridor(List<Vector2Int> path)
    {
        var widened = new List<Vector2Int>();
        for (int i = 0; i < path.Count; i++)
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    widened.Add(path[i] + new Vector2Int(dx, dy));
        return widened;
    }

    private Vector2Int FindClosestCenter(Vector2Int from, List<Vector2Int> centers)
    {
        Vector2Int closest = centers[0];
        float minDistance = float.MaxValue;
        foreach (Vector2Int center in centers)
        {
            float distance = Vector2.Distance(from, center);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = center;
            }
        }
        return closest;
    }

    private static Vector2Int RoomCenter(RectInt room)
    {
        return new Vector2Int(room.xMin + room.width / 2, room.yMin + room.height / 2);
    }

    private void SetFloor(int x, int y)
    {
        bool isInsideMap = x >= 0 && x < data.mapWidth && y >= 0 && y < data.mapHeight;
        if (isInsideMap)
            map[x, y] = TileType.Floor;
    }
}
