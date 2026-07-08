using UnityEngine;

/// <summary>
/// Thanh kỹ năng của người chơi: bind 4 ô từ PlayerSkillCaster lúc Start,
/// mỗi frame poll tỉ lệ hồi chiêu (theo precedent TrainingSlotView - không cần event).
/// </summary>
public class SkillBarView : MonoBehaviour
{
    [SerializeField] private PlayerSkillCaster caster;
    [SerializeField] private SkillSlotView[] slots;

    private void Start()
    {
        if (caster == null)
        {
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i]?.Bind(caster.GetSlotSkill(i), (i + 1).ToString());
        }
    }

    private void Update()
    {
        if (caster == null)
        {
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i]?.SetCooldownFraction(caster.CooldownFraction(i));
        }
    }
}
