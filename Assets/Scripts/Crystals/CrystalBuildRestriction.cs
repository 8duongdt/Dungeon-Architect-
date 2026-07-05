using UnityEngine;

/// <summary>
/// Luật ràng buộc "chỉ xây được gần tinh thể cùng loại đang Active" cho công trình khai thác
/// (máy đào Vàng cần tinh thể Gold, lò ma thuật cần tinh thể Mana). Loại tinh thể cần thiết suy ra
/// trực tiếp từ <see cref="ResourceProducer"/> gắn trên prefab công trình - không cần field dữ liệu
/// trùng lặp trên <see cref="PlacedObjectTypeSO"/>.
/// </summary>
public static class CrystalBuildRestriction
{
    /// <summary>true nếu công trình này không cần tinh thể, hoặc có một tinh thể cùng loại đang
    /// Active mà <paramref name="worldOrigin"/> nằm trong bán kính ảnh hưởng.</summary>
    public static bool IsSatisfied(PlacedObjectTypeSO type, Vector3 worldOrigin)
    {
        CrystalType? requiredType = GetRequiredCrystalType(type);
        if (requiredType == null)
        {
            return true;
        }

        foreach (CrystalNode node in CrystalNodeRegistry.All)
        {
            if (node.Type != requiredType.Value || node.State != CrystalState.Active)
            {
                continue;
            }

            if (Vector2.Distance(node.transform.position, worldOrigin) <= node.InfluenceRadius)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Loại tinh thể mà công trình này cần đứng gần - null nếu công trình không phải máy
    /// khai thác Gold/Mana (vd hàng rào, doanh trại...).</summary>
    public static CrystalType? GetRequiredCrystalType(PlacedObjectTypeSO type)
    {
        ResourceProducer producer = type != null && type.prefab != null
            ? type.prefab.GetComponent<ResourceProducer>()
            : null;
        if (producer == null)
        {
            return null;
        }

        return producer.Kind == ResourceProducer.ResourceKind.Gold ? CrystalType.Gold : CrystalType.Mana;
    }
}
