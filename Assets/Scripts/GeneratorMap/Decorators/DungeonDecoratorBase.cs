using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lớp cơ sở chung cho mọi decorator sinh vật trang trí lên map dungeon. Gom phần lặp lại
/// giữa các decorator: spawn prefab tại tâm ô (qua <see cref="TilemapVisualizer"/>), theo dõi
/// vật đã spawn để dọn ở lần sinh map sau, và roll xác suất phần trăm.
/// Lớp con chỉ lo LUẬT ĐẶT vật (đặt ở đâu, theo điều kiện gì).
/// </summary>
public abstract class DungeonDecoratorBase : MonoBehaviour
{
    // Gốc gom các vật đã spawn (để Hierarchy gọn và dễ dọn). Có thể bỏ trống.
    [SerializeField] protected Transform decorationParent;

    // Theo dõi vật đã spawn để dọn ở lần sinh map sau.
    private readonly List<GameObject> spawnedObjects = new List<GameObject>();

    // Spawn prefab tại tâm ô và gom lại để dọn sau. Trả về instance (null nếu prefab trống).
    protected GameObject Spawn(GameObject prefab, TilemapVisualizer visualizer, Vector2Int cell)
    {
        if (prefab == null)
        {
            return null;
        }

        Vector3 worldPosition = visualizer.CellToWorldCenter(cell);
        GameObject instance = Instantiate(prefab, worldPosition, Quaternion.identity, decorationParent);
        spawnedObjects.Add(instance);
        return instance;
    }

    /// <summary>
    /// Dọn toàn bộ vật trang trí của lần sinh map trước. Dọn cả danh sách đã theo dõi LẪN mọi vật
    /// còn sót dưới <see cref="decorationParent"/> - nhờ vậy không tích tụ rác qua các phiên Editor
    /// (vật baked từ phiên trước không nằm trong danh sách theo dõi vẫn bị dọn sạch).
    /// </summary>
    public void ClearSpawned()
    {
        foreach (GameObject spawned in spawnedObjects)
        {
            DestroyObject(spawned);
        }
        spawnedObjects.Clear();

        DestroyLeftoverChildren();
        OnCleared();
    }

    // Dọn mọi con còn sót dưới vật chứa trang trí (rác baked từ lần sinh/phiên trước).
    private void DestroyLeftoverChildren()
    {
        if (decorationParent == null)
        {
            return;
        }

        for (int i = decorationParent.childCount - 1; i >= 0; i--)
        {
            DestroyObject(decorationParent.GetChild(i).gameObject);
        }
    }

    private static void DestroyObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    // Lớp con override để dọn thêm trạng thái riêng (vd: tập ô đã chiếm).
    protected virtual void OnCleared() { }

    // true nếu trúng theo tỉ lệ phần trăm (0-100).
    protected static bool RollPercent(float percent)
    {
        return Random.value * 100f < percent;
    }
}
