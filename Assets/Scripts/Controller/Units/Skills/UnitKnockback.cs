using System.Collections;
using UnityEngine;

/// <summary>
/// Đẩy lùi một unit khỏi tâm nổ: choáng ngắn (để AI không ghì vận tốc lại)
/// rồi dịch chuyển mượt bằng rb.MovePosition trong thời gian ngắn.
/// Component được thêm lúc runtime vào nạn nhân khi lần đầu bị đẩy.
/// </summary>
[DisallowMultipleComponent]
public class UnitKnockback : MonoBehaviour
{
    private const float PushDuration = 0.2f;

    private Rigidbody2D rb;
    private Coroutine activePush;

    public static void Push(GameObject victim, Vector2 direction, float distance)
    {
        if (distance <= 0f)
        {
            return;
        }

        var knockback = victim.GetComponent<UnitKnockback>();
        if (knockback == null)
        {
            knockback = victim.AddComponent<UnitKnockback>();
        }

        // Choáng bằng thời gian đẩy để AI/lệnh di chuyển không ghì lại giữa chừng.
        UnitStatusEffects.GetOrAdd(victim).ApplyStun(PushDuration);
        knockback.StartPush(direction.normalized, distance);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void StartPush(Vector2 direction, float distance)
    {
        if (activePush != null)
        {
            StopCoroutine(activePush);
        }

        activePush = StartCoroutine(PushOverTime(direction, distance));
    }

    private IEnumerator PushOverTime(Vector2 direction, float distance)
    {
        float elapsed = 0f;
        while (elapsed < PushDuration)
        {
            yield return new WaitForFixedUpdate();

            float stepDistance = distance * (Time.fixedDeltaTime / PushDuration);
            Vector2 currentPosition = rb != null ? rb.position : (Vector2)transform.position;
            Vector2 nextPosition = currentPosition + direction * stepDistance;
            if (rb != null)
            {
                rb.MovePosition(nextPosition);
            }
            else
            {
                transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);
            }

            elapsed += Time.fixedDeltaTime;
        }

        activePush = null;
    }
}
