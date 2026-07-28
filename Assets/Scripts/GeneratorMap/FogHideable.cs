using UnityEngine;

/// <summary>
/// Bật/tắt toàn bộ HÌNH ẢNH của một đơn vị (SpriteRenderer, thanh máu, canvas...) theo sương mù -
/// dùng để giấu địch ngoài tầm nhìn mà KHÔNG tắt logic (địch vẫn di chuyển/tấn công). Chỉ tác động
/// tới Renderer và Canvas nên collider/tầm đánh/AI giữ nguyên. <see cref="FogOfWarController"/> tự
/// thêm component này vào địch khi cần.
/// </summary>
[DisallowMultipleComponent]
public class FogHideable : MonoBehaviour
{
    private Renderer[] renderers;
    private Canvas[] canvases;
    private bool cached;
    private bool isVisible = true;

    /// <summary>Hiện (true) hay giấu (false) mọi hình ảnh của đơn vị. Bỏ qua nếu trạng thái không đổi.</summary>
    public void SetVisible(bool value)
    {
        if (cached && value == isVisible)
        {
            return;
        }

        if (!cached)
        {
            CacheVisuals();
        }

        isVisible = value;
        ApplyVisibility(value);
    }

    private void CacheVisuals()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        canvases = GetComponentsInChildren<Canvas>(true);
        cached = true;
    }

    private void ApplyVisibility(bool value)
    {
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = value;
            }
        }

        foreach (Canvas canvas in canvases)
        {
            if (canvas != null)
            {
                canvas.enabled = value;
            }
        }
    }
}
