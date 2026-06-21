using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RoomFirstDungeonGenerator : SimpleRandomWalkDungeonGenerator
{
    [SerializeField]
    private int minRoomWidth = 4, minRoomHeight = 4;
    [SerializeField]
    private int dungeonWidth = 20, dungeonHeight = 20;
    [SerializeField]
    [Range(0,10)]
    private int offset = 1;
    [SerializeField]
    private bool randomWalkRooms = false;

    [Header("Portal")]
    [Tooltip("Prefab của cổng sẽ được đặt ngẫu nhiên vào một vài phòng.")]
    [SerializeField]
    private GameObject portalPrefab;
    [Tooltip("Số lượng cổng tối đa sinh ra trên bản đồ. Sẽ được giới hạn theo số phòng hiện có.")]
    [SerializeField]
    [Min(0)]
    private int portalCount = 1;
    [Tooltip("Vật chứa (cha) cho các cổng được sinh ra. Để trống sẽ tự tạo một đối tượng chứa.")]
    [SerializeField]
    private Transform portalParent;

    private readonly List<GameObject> spawnedPortals = new List<GameObject>();

    protected override void RunProceduralGeneration()
    {
        CreateRooms();
    }

    private void CreateRooms()
    {
        var roomsList = ProceduralGenerationAlgorithms.BinarySpacePartitioning(new BoundsInt((Vector3Int)startPosition, new Vector3Int(dungeonWidth, dungeonHeight, 0)), minRoomWidth, minRoomHeight);

        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();

        if (randomWalkRooms)
        {
            floor = CreateRoomsRandomly(roomsList);
        }
        else
        {
            floor = CreateSimpleRooms(roomsList);
        }
        

        List<Vector2Int> roomCenters = new List<Vector2Int>();
        foreach (var room in roomsList)
        {
            roomCenters.Add((Vector2Int)Vector3Int.RoundToInt(room.center));
        }

        // Đặt cổng sinh quái trước khi ConnectRooms làm thay đổi danh sách tâm phòng.
        SpawnPortals(roomCenters);

        List<List<Vector2Int>> corridors = ConnectRooms(roomCenters);
        for (int i = 0; i < corridors.Count; i++)
        {
            corridors[i] = IncreaseCorridorBrush3by3(corridors[i]);
            floor.UnionWith(corridors[i]);
        }

        tilemapVisualizer.PaintFloorTiles(floor);
        WallGeneration.CreateWalls(floor, tilemapVisualizer);
    }

    private HashSet<Vector2Int> CreateRoomsRandomly(List<BoundsInt> roomsList)
    {
        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();
        for (int i = 0; i < roomsList.Count; i++)
        {
            var roomBounds = roomsList[i];
            var roomCenter = new Vector2Int(Mathf.RoundToInt(roomBounds.center.x), Mathf.RoundToInt(roomBounds.center.y));
            var roomFloor = RunRandomWalk(randomWalkParameters, roomCenter);
            foreach (var position in roomFloor)
            {
                if(position.x >= (roomBounds.xMin + offset) && position.x <= (roomBounds.xMax - offset) && position.y >= (roomBounds.yMin - offset) && position.y <= (roomBounds.yMax - offset))
                {
                    floor.Add(position);
                }
            }
        }
        return floor;
    }

    public List<Vector2Int> IncreaseCorridorBrush3by3(List<Vector2Int> corridor)
    {
        List<Vector2Int> newCorridor = new List<Vector2Int>();
        for (int i = 1; i < corridor.Count - 1; i++)
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    newCorridor.Add(corridor[i - 1] + new Vector2Int(x, y));
                }
            }
        }
        return newCorridor;
    }

    private List<List<Vector2Int>> ConnectRooms(List<Vector2Int> roomCenters)
    {
        List<List<Vector2Int>> corridors = new List<List<Vector2Int>>();
        var currentRoomCenter = roomCenters[Random.Range(0, roomCenters.Count)];
        roomCenters.Remove(currentRoomCenter);

        while (roomCenters.Count > 0)
        {
            Vector2Int closest = FindClosestPointTo(currentRoomCenter, roomCenters);
            roomCenters.Remove(closest);
            List<Vector2Int> newCorridor = CreateCorridor(currentRoomCenter, closest);
            currentRoomCenter = closest;
            corridors.Add(newCorridor);
        }
        return corridors;
    }

    private List<Vector2Int> CreateCorridor(Vector2Int currentRoomCenter, Vector2Int destination)
    {
        List<Vector2Int> corridor = new List<Vector2Int>();
        var position = currentRoomCenter;
        corridor.Add(position);
        while (position.y != destination.y)
        {
            if(destination.y > position.y)
            {
                position += Vector2Int.up;
            }
            else if(destination.y < position.y)
            {
                position += Vector2Int.down;
            }
            corridor.Add(position);
        }
        while (position.x != destination.x)
        {
            if (destination.x > position.x)
            {
                position += Vector2Int.right;
            }else if(destination.x < position.x)
            {
                position += Vector2Int.left;
            }
            corridor.Add(position);
        }
        return corridor;
    }

    private Vector2Int FindClosestPointTo(Vector2Int currentRoomCenter, List<Vector2Int> roomCenters)
    {
        Vector2Int closest = Vector2Int.zero;
        float distance = float.MaxValue;
        foreach (var position in roomCenters)
        {
            float currentDistance = Vector2.Distance(position, currentRoomCenter);
            if(currentDistance < distance)
            {
                distance = currentDistance;
                closest = position;
            }
        }
        return closest;
    }

    // Sinh ngẫu nhiên các cổng vào một vài phòng (số lượng cấu hình qua portalCount).
    private void SpawnPortals(List<Vector2Int> roomCenters)
    {
        ClearPortals();

        if (portalPrefab == null || portalCount <= 0 || roomCenters.Count == 0)
        {
            return;
        }

        // Tạo vật chứa nếu chưa được gán để giữ Hierarchy gọn gàng.
        if (portalParent == null)
        {
            portalParent = new GameObject("Portals").transform;
            portalParent.SetParent(transform, false);
        }

        // Số cổng không vượt quá số phòng đang có.
        int portalsToSpawn = Mathf.Min(portalCount, roomCenters.Count);

        // Trộn danh sách tâm phòng rồi lấy ra portalsToSpawn phòng đầu tiên
        // để mỗi phòng chỉ có tối đa một cổng và vị trí là ngẫu nhiên.
        List<Vector2Int> shuffledCenters = new List<Vector2Int>(roomCenters);
        for (int i = shuffledCenters.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffledCenters[i], shuffledCenters[j]) = (shuffledCenters[j], shuffledCenters[i]);
        }

        for (int i = 0; i < portalsToSpawn; i++)
        {
            Vector3 worldPosition = tilemapVisualizer.CellToWorldCenter(shuffledCenters[i]);
            GameObject portal = Instantiate(portalPrefab, worldPosition, Quaternion.identity, portalParent);
            spawnedPortals.Add(portal);
        }
    }

    // Xóa các cổng đã sinh ở lần tạo bản đồ trước để tránh trùng lặp khi tạo lại.
    private void ClearPortals()
    {
        for (int i = 0; i < spawnedPortals.Count; i++)
        {
            if (spawnedPortals[i] == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(spawnedPortals[i]);
            }
            else
            {
                DestroyImmediate(spawnedPortals[i]);
            }
        }
        spawnedPortals.Clear();
    }

    private HashSet<Vector2Int> CreateSimpleRooms(List<BoundsInt> roomsList)
    {
        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();
        foreach (var room in roomsList)
        {
            for (int col = offset; col < room.size.x - offset; col++)
            {
                for (int row = offset; row < room.size.y - offset; row++)
                {
                    Vector2Int position = (Vector2Int)room.min + new Vector2Int(col, row);
                    floor.Add(position);
                }
            }
        }
        return floor;
    }
}