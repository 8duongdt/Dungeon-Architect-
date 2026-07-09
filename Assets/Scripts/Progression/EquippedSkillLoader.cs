using UnityEngine;

/// <summary>
/// Nạp skill ĐÃ TRANG BỊ (chọn ở Lobby, lưu trong PlayerProgression) vào 4 ô của
/// PlayerSkillCaster khi vào scene. Chạy trong Awake - trước SkillBarView.Start
/// nên HUD tự đọc lại icon, không cần wire thêm. Ô trống trong progression = ô trống trên thanh skill.
/// </summary>
[DisallowMultipleComponent]
public class EquippedSkillLoader : MonoBehaviour
{
    [Tooltip("Asset cây kỹ năng - registry tra tên skill -> SkillDefinitionSO.")]
    [SerializeField] private SkillTreeSO skillTree;

    [Tooltip("Caster của người chơi nhận skill đã trang bị (bỏ trống = lấy trên cùng GameObject).")]
    [SerializeField] private PlayerSkillCaster caster;

    private void Awake()
    {
        if (caster == null)
        {
            caster = GetComponent<PlayerSkillCaster>();
        }

        bool canLoad = skillTree != null && caster != null;
        if (!canLoad)
        {
            Debug.LogWarning("[EquippedSkillLoader] Thiếu skillTree/caster - giữ nguyên slot mặc định của scene.");
            return;
        }

        PlayerProgression.EnsureDefaults();
        ApplyEquippedSlots();
    }

    private void ApplyEquippedSlots()
    {
        string[] equippedNames = PlayerProgression.GetEquippedSlots();
        for (int i = 0; i < equippedNames.Length; i++)
        {
            caster.SetSlotSkill(i, skillTree.FindSkillByName(equippedNames[i]));
        }
    }
}
