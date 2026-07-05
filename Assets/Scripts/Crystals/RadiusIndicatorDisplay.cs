using UnityEngine;

/// <summary>
/// Vẽ một vòng tròn world-space quanh tâm bất kỳ, dùng LineRenderer - hiển thị bán kính ảnh hưởng
/// của tinh thể hoặc vùng lãnh thổ pha lê đen khi chuột phải/khi đặt công trình gần đó.
/// Không có sẵn hiển thị bán kính nào trong project trước đây nên đây là component mới, dùng
/// chung cho mọi <see cref="IHasInfluenceRadius"/>.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class RadiusIndicatorDisplay : MonoBehaviour
{
    private const int CircleSegments = 48;

    [SerializeField] private float lineWidth = 0.08f;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.loop = true;
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = CircleSegments;
        lineRenderer.widthMultiplier = lineWidth;
        Hide();
    }

    public void Show(Vector3 center, float radius, Color color)
    {
        lineRenderer.enabled = true;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;

        for (int i = 0; i < CircleSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / CircleSegments;
            Vector3 point = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            lineRenderer.SetPosition(i, point);
        }
    }

    public void Hide()
    {
        lineRenderer.enabled = false;
    }
}
