using UnityEngine;

[DisallowMultipleComponent]
public class AttackState : MonoBehaviour
{
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private CharacterAnimationController animationController;
    [SerializeField] private UnitAudioPlayer audioPlayer;

    /// <summary>Sát thương mỗi đòn đánh GỐC (chỉ đọc) - để HUD hiển thị chỉ số ATTACK.</summary>
    public float AttackDamage => attackDamage;

    // Hệ số nhân runtime (buff aura portal...). 1 = gốc. Sát thương và nhịp đánh đều tính TỪ giá trị
    // gốc nhân hệ số nên đặt lại nhiều lần không cộng dồn. cooldownMultiplier < 1 = đánh nhanh hơn.
    private float damageMultiplier = 1f;
    private float attackCooldownMultiplier = 1f;

    private UnitAI ai;
    private UnitMovement movement;
    private AttackAreaBase attackArea;
    private UnitProjectileLauncher projectileLauncher;
    private float nextAttackTime;
    private bool hasDealtDamageThisSwing;
    private float attackAnimationLength;

    /// <summary>Áp chỉ số chiến đấu GỐC từ bộ chỉ số trung tâm (<see cref="UnitStatsSO"/>).</summary>
    public void ApplyCombatStats(float damage, float cooldown)
    {
        attackDamage = Mathf.Max(0f, damage);
        attackCooldown = Mathf.Max(0f, cooldown);
    }

    /// <summary>Hệ số nhân sát thương (buff aura portal). 1 = gốc.</summary>
    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = Mathf.Max(0f, multiplier);
    }

    /// <summary>Hệ số nhân thời gian hồi đòn (nhỏ hơn 1 = đánh nhanh hơn). 1 = gốc.</summary>
    public void SetAttackCooldownMultiplier(float multiplier)
    {
        attackCooldownMultiplier = Mathf.Max(0.01f, multiplier);
    }

    public void Initialize(UnitAI unitAI, UnitMovement unitMovement, AttackAreaBase unitAttackArea)
    {
        ai = unitAI;
        movement = unitMovement;
        attackArea = unitAttackArea;
        projectileLauncher = GetComponent<UnitProjectileLauncher>();
        animationController = GetAnimationController();
        audioPlayer = GetComponent<UnitAudioPlayer>();
        CacheAttackAnimationLength();
    }

    public void Tick()
    {
        if (!ai.HasValidTarget())
        {
            ai.ClearTarget();
            return;
        }

        Transform target = ai.TargetEnemy;
        if (!attackArea.CanSee(target))
        {
            ai.ClearTarget();
            return;
        }

        if (!attackArea.IsInAttackRange(target))
        {
            ai.SetState(UnitAI.UnitState.Chase);
            return;
        }

        movement.Stop();

        bool isAnimationPlaying = animationController != null && animationController.IsAttacking;

        if (isAnimationPlaying)
        {
            // Try to land the hit each frame until the hitbox connects (once per swing)
            if (!hasDealtDamageThisSwing)
            {
                TryDealDamage(target);
            }
            return;
        }

        // Animation finished — reset flag and check cooldown before next swing
        hasDealtDamageThisSwing = false;

        if (Time.time >= nextAttackTime)
        {
            StartSwing(target);
            // Nhịp đánh không bao giờ ngắn hơn thời lượng animation -> animation
            // luôn chạy xong trước khi bắt đầu đòn kế tiếp.
            if (attackAnimationLength <= 0f)
            {
                CacheAttackAnimationLength();
            }

            nextAttackTime = Time.time + Mathf.Max(attackCooldown * attackCooldownMultiplier, attackAnimationLength);
        }
    }

    private void CacheAttackAnimationLength()
    {
        if (animationController != null)
        {
            attackAnimationLength = animationController.GetLongestClipLengthContaining("attack");
        }
    }

    private void StartSwing(Transform target)
    {
        hasDealtDamageThisSwing = false;
        Vector2 attackDirection = target.position - transform.position;
        if (animationController != null)
        {
            animationController.SetFacingDirection(attackDirection);
            animationController.PlayAttack();
        }
        audioPlayer?.PlaySwing();
    }

    private void TryDealDamage(Transform target)
    {
        if (!attackArea.TryGetDamageTarget(ai, target, out UnitHealth targetHealth))
        {
            return;
        }

        hasDealtDamageThisSwing = true;
        float dealtDamage = attackDamage * damageMultiplier;

        // Unit tầm xa có bệ phóng: đạn bay tới nơi mới gây sát thương, không trừ máu tức thời.
        if (projectileLauncher != null && projectileLauncher.TryLaunch(targetHealth, dealtDamage))
        {
            return;
        }

        targetHealth.TakeDamage(dealtDamage);
        audioPlayer?.PlayHit();
        Debug.Log($"{gameObject.name} attacks {targetHealth.name} for {dealtDamage} damage");

        if (targetHealth.IsDead)
        {
            ai.ClearTarget();
        }
    }

    private void OnValidate()
    {
        attackCooldown = Mathf.Max(0f, attackCooldown);
        attackDamage = Mathf.Max(0f, attackDamage);
    }

    private CharacterAnimationController GetAnimationController()
    {
        if (animationController != null)
        {
            return animationController;
        }

        CharacterAnimationController controller = GetComponent<CharacterAnimationController>();
        if (controller == null && GetComponentInChildren<Animator>() != null)
        {
            controller = gameObject.AddComponent<CharacterAnimationController>();
        }

        return controller;
    }
}
