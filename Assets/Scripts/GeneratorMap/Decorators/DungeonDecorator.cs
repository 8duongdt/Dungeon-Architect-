using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rải vật trang trí (đuốc, rương) lên bản đồ TileType[,] đã sinh.
/// Quy tắc đặt: đuốc chỉ lên ô Wall có ô Floor ngay bên dưới; rương chỉ lên ô Floor.
/// Không tự sinh map - chỉ đọc dữ liệu và spawn prefab.
/// </summary>
public class DungeonDecorator : MonoBehaviour
{
    [SerializeField]
    private GameObject torchPrefab;

    [SerializeField]
    private GameObject chestPrefab;

    // Gốc chứa các vật trang trí đã spawn (để gom gọn và dễ dọn dẹp). Có thể bỏ trống.
    [SerializeField]
    private Transform decorationParent;

    // Theo dõi các vật đã spawn để dọn ở lần sinh map sau.
    private readonly List<GameObject> spawnedDecorations = new List<GameObject>();

    /// <summary>
    /// Đổi prefab trang trí lúc chạy (dùng khi mỗi theme có đuốc/rương riêng).
    /// Bỏ trống (null) thì giữ nguyên prefab mặc định gán trong Inspector.
    /// </summary>
    public void SetDecorationPrefabs(GameObject torch, GameObject chest)
    {
        if (torch != null)
            torchPrefab = torch;
        if (chest != null)
            chestPrefab = chest;
    }

    /// <summary>
    /// Rải vật trang trí dựa trên bản đồ và tỉ lệ sinh trong DungeonData.
    /// Dùng visualizer (không sửa đổi) để đổi tọa độ ô sang tâm ô thế giới.
    /// </summary>
    public void Decorate(TileType[,] map, DungeonData data, TilemapVisualizer visualizer)
    {
        if (map == null || data == null || visualizer == null)
            return;

        int width = map.GetLength(0);
        int height = map.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TryPlaceTorch(map, data, visualizer, x, y);
                TryPlaceChest(map, data, visualizer, x, y);
            }
        }
    }

    // Đuốc: chỉ trên ô Wall và ô ngay bên dưới (y - 1) là Floor.
    private void TryPlaceTorch(TileType[,] map, DungeonData data, TilemapVisualizer visualizer, int x, int y)
    {
        if (torchPrefab == null || map[x, y] != TileType.Wall)
            return;

        int below = y - 1;
        if (below < 0 || map[x, below] != TileType.Floor)
            return;

        if (RollPercent(data.torchSpawnRate))
            SpawnAt(torchPrefab, visualizer, x, y);
    }

    // Rương: chỉ trên ô Floor.
    private void TryPlaceChest(TileType[,] map, DungeonData data, TilemapVisualizer visualizer, int x, int y)
    {
        if (chestPrefab == null || map[x, y] != TileType.Floor)
            return;

        if (RollPercent(data.chestSpawnRate))
            SpawnAt(chestPrefab, visualizer, x, y);
    }

    private void SpawnAt(GameObject prefab, TilemapVisualizer visualizer, int x, int y)
    {
        Vector3 worldPosition = visualizer.CellToWorldCenter(new Vector2Int(x, y));
        GameObject instance = Instantiate(prefab, worldPosition, Quaternion.identity, decorationParent);
        spawnedDecorations.Add(instance);
    }

    // true nếu trúng theo tỉ lệ phần trăm (0-100).
    private bool RollPercent(float percent)
    {
        return Random.value * 100f < percent;
    }

    /// <summary>
    /// Dọn toàn bộ vật trang trí của lần sinh map trước.
    /// </summary>
    public void ClearDecorations()
    {
        foreach (var decoration in spawnedDecorations)
        {
            if (decoration == null)
                continue;

            if (Application.isPlaying)
                Destroy(decoration);
            else
                DestroyImmediate(decoration);
        }

        spawnedDecorations.Clear();
    }
}
