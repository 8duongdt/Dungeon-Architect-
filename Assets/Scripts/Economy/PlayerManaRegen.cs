using UnityEngine;

/// <summary>
/// Tự hồi Mana theo thời gian cho người chơi. Phase 2 không có công trình khai thác
/// (ResourceProducer gắn với tinh thể/building của RTS) nên nếu không hồi, Mana sẽ
/// cạn vĩnh viễn - ngược với thiết kế skill spam CD ngắn (FireBall/Lightning).
/// Cộng dồn phần lẻ vì ResourceManager.AddMana chỉ nhận số nguyên.
/// </summary>
public class PlayerManaRegen : MonoBehaviour
{
    [Tooltip("Lượng Mana hồi mỗi giây.")]
    [SerializeField] private float regenPerSecond = 4f;

    private float accumulatedMana;

    private void Update()
    {
        if (ResourceManager.Instance == null)
        {
            return;
        }

        accumulatedMana += regenPerSecond * Time.deltaTime;
        int wholeMana = Mathf.FloorToInt(accumulatedMana);
        if (wholeMana <= 0)
        {
            return;
        }

        ResourceManager.Instance.AddMana(wholeMana);
        accumulatedMana -= wholeMana;
    }

    private void OnValidate()
    {
        regenPerSecond = Mathf.Max(0f, regenPerSecond);
    }
}
