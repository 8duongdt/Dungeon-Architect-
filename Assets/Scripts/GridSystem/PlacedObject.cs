using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Một công trình đã được đặt xuống lưới. Gắn runtime lên instance của prefab, ghi nhớ ô gốc (origin)
/// và loại công trình để có thể liệt kê lại vùng chiếm dụng khi phá dỡ.
/// </summary>
public class PlacedObject : MonoBehaviour
{
    private Vector2Int origin;
    private PlacedObjectTypeSO type;

    /// <summary>Loại công trình - để đọc chi phí xây (tính hoàn trả) và thông tin hiển thị.</summary>
    public PlacedObjectTypeSO Type => type;

    /// <summary>Sinh prefab tại <paramref name="worldPosition"/> rồi gắn dữ liệu chiếm dụng vào nó.</summary>
    public static PlacedObject Create(Vector3 worldPosition, Vector2Int origin, PlacedObjectTypeSO type)
    {
        Transform instance = Instantiate(type.prefab, worldPosition, Quaternion.identity);

        PlacedObject placedObject = instance.gameObject.AddComponent<PlacedObject>();
        placedObject.origin = origin;
        placedObject.type = type;
        return placedObject;
    }

    /// <summary>Mọi ô lưới mà công trình này đang chiếm dụng.</summary>
    public List<Vector2Int> GetGridPositionList()
    {
        return type.GetGridPositionList(origin);
    }

    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
