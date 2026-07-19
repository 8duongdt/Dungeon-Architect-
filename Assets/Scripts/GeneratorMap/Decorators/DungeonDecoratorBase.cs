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
    // Gốc gom các vật đã spawn (để Hierarchy gọn và dễ dọn). Bỏ trống thì TỰ TẠO một vật chứa con
    // (xem EnsureDecorationParent) - tuyệt đối không spawn thẳng ra gốc scene, vì vật ở gốc scene
    // không được ClearSpawned quét: lỡ lưu scene khi đang Play là rác tích tụ vĩnh viễn qua từng phiên.
    [SerializeField] protected Transform decorationParent;

    // Theo dõi vật đã spawn để dọn ở lần sinh map sau.
    private readonly List<GameObject> spawnedObjects = new List<GameObject>();

    // Hạt nhân đặt vật của lần Decorate hiện tại (snap tâm ô + theo dõi ô đã chiếm).
    protected MapPlacement Placement { get; private set; }

    // Lớp con gọi đầu mỗi lần Decorate để gắn map vào hạt nhân đặt vật dùng chung.
    protected void BeginPlacement(MapPlacement placement)
    {
        Placement = placement;
    }

    // Spawn tại tâm ô và gom lại để dọn sau (KHÔNG đánh dấu ô đã chiếm) - dùng cho vật trên
    // tường (đuốc) hay hazard trên nước, vốn không tranh ô sàn với vật khác.
    protected GameObject SpawnAt(GameObject prefab, Vector2Int cell)
    {
        if (prefab == null || Placement == null)
        {
            return null;
        }

        Vector3 worldPosition = Placement.CellCenter(cell);
        GameObject instance = Instantiate(prefab, worldPosition, Quaternion.identity, EnsureDecorationParent());
        spawnedObjects.Add(instance);
        return instance;
    }

    // Vật chứa để gom đồ đã spawn - chưa gán thì tự tạo một GameObject con. Nhờ vậy MỌI vật spawn
    // đều nằm dưới một gốc mà ClearSpawned quét được, không bao giờ rơi ra gốc scene.
    private Transform EnsureDecorationParent()
    {
        if (decorationParent == null)
        {
            var container = new GameObject($"{name}_Spawned");
            container.transform.SetParent(transform, false);
            decorationParent = container.transform;
        }

        return decorationParent;
    }

    // Spawn tại tâm ô RỒI đánh dấu ô đã chiếm để các luật đặt sau không chồng lên.
    protected GameObject SpawnAndOccupy(GameObject prefab, Vector2Int cell)
    {
        GameObject instance = SpawnAt(prefab, cell);
        if (instance != null)
        {
            Placement.Occupy(cell);
        }
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

    // protected để lớp con dọn thêm rác riêng của nó (vd tinh thể mồ côi ở gốc scene) đúng cách
    // cho cả Edit mode lẫn Play mode.
    protected static void DestroyObject(GameObject target)
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
