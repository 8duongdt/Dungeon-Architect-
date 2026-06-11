using UnityEngine;

[DisallowMultipleComponent]
public class RTSUnitLineOfSight : MonoBehaviour
{
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float sampleRadius = 0.08f;
    [SerializeField] private float sampleSpacing = 0.2f;

    public bool HasLineOfSight(Vector2 from, Vector2 to)
    {
        float distance = Vector2.Distance(from, to);
        if (distance <= sampleRadius)
        {
            return true;
        }

        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(distance / sampleSpacing));
        for (int i = 1; i < sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            Vector2 samplePoint = Vector2.Lerp(from, to, t);
            if (Physics2D.OverlapCircle(samplePoint, sampleRadius, obstacleLayer) != null)
            {
                return false;
            }
        }

        return true;
    }

    private void OnValidate()
    {
        sampleRadius = Mathf.Max(0.01f, sampleRadius);
        sampleSpacing = Mathf.Max(0.01f, sampleSpacing);
    }
}
