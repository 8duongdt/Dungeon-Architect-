using UnityEngine;

[DisallowMultipleComponent]
public class AttackState : MonoBehaviour
{
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private CharacterAnimationController animationController;

    /// <summary>Sát thương mỗi đòn đánh (chỉ đọc) - để HUD hiển thị chỉ số ATTACK.</summary>
    public float AttackDamage => attackDamage;

    private UnitAI ai;
    private UnitMovement movement;
    private AttackAreaBase attackArea;
    private float nextAttackTime;
    private bool hasDealtDamageThisSwing;
    private float attackAnimationLength;

    public void Initialize(UnitAI unitAI, UnitMovement unitMovement, AttackAreaBase unitAttackArea)
    {
        ai = unitAI;
        movement = unitMovement;
        attackArea = unitAttackArea;
        animationController = GetAnimationController();
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

            nextAttackTime = Time.time + Mathf.Max(attackCooldown, attackAnimationLength);
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
    }

    private void TryDealDamage(Transform target)
    {
        if (!attackArea.TryGetDamageTarget(ai, target, out UnitHealth targetHealth))
        {
            return;
        }

        hasDealtDamageThisSwing = true;
        targetHealth.TakeDamage(attackDamage);
        Debug.Log($"{gameObject.name} attacks {targetHealth.name} for {attackDamage} damage");

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
