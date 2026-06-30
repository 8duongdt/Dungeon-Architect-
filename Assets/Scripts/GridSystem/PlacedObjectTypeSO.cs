using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Định nghĩa một loại công trình có thể xây: prefab thật, ảnh "bóng mờ" (ghost) bám chuột, icon cho
/// nút UI và kích thước chiếm dụng tính theo ô lưới (width x height). Không hỗ trợ xoay.
/// </summary>
[CreateAssetMenu(fileName = "PlacedObjectType_", menuName = "Building/PlacedObjectType")]
public class PlacedObjectTypeSO : ScriptableObject
{
    [Tooltip("Tên hiển thị của công trình.")]
    public string nameString;

    [Tooltip("Prefab 2D sinh ra khi xây.")]
    public Transform prefab;

    [Tooltip("Prefab/ảnh mờ đi theo chuột trước khi đặt (ghost visual).")]
    public Transform visual;

    [Tooltip("Icon hiển thị trên nút UI chọn công trình.")]
    public Sprite icon;

    [Header("Bảng điều khiển (HUD)")]
    [Tooltip("Giá Vàng để xây.")]
    [Min(0)]
    public int goldCost;

    [Tooltip("Giá Mana để xây.")]
    [Min(0)]
    public int manaCost;

    [Tooltip("Phím tắt hiển thị/kích hoạt ô lệnh (vd: Q, W, E, A, S, D).")]
    public string hotkey;

    [Tooltip("Mô tả ngắn hiển thị trên bảng lệnh.")]
    [TextArea]
    public string description;

    [Tooltip("Số ô chiếm theo chiều ngang.")]
    [Min(1)]
    public int width = 3;

    [Tooltip("Số ô chiếm theo chiều dọc.")]
    [Min(1)]
    public int height = 3;

    /// <summary>Danh sách mọi ô lưới mà công trình chiếm dụng khi đặt gốc tại <paramref name="origin"/>.</summary>
    public List<Vector2Int> GetGridPositionList(Vector2Int origin)
    {
        var gridPositionList = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                gridPositionList.Add(origin + new Vector2Int(x, y));
            }
        }
        return gridPositionList;
    }
}
