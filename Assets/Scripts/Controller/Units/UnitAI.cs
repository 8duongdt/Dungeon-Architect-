using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitFaction))]
[RequireComponent(typeof(UnitHealth))]
[RequireComponent(typeof(UnitDamageAnimation))]
[RequireComponent(typeof(UnitMovement))]
[RequireComponent(typeof(UnitLineOfSight))]
[RequireComponent(typeof(IdleState))]
[RequireComponent(typeof(ChaseState))]
[RequireComponent(typeof(AttackState))]
public class UnitAI : MonoBehaviour
{
    public enum UnitState { Idle, Chase, Attack }

    [Header("State Management")]
    public UnitState currentState = UnitState.Idle;

    [SerializeField] private Transform targetEnemy;
    [Tooltip("Khi bị kẹt (không tiến được, vd gặp vật cản) quá số giây này thì kết thúc lệnh di chuyển để unit không bị treo.")]
    [SerializeField] private float moveCommandStuckTimeout = 1.5f;

    private const float MoveProgressThreshold = 0.05f;

    private UnitHealth health;
    private UnitFaction faction;
    private UnitMovement movement;
    private AttackAreaBase attackArea;
    private IdleState idleState;
    private ChaseState chaseState;
    private AttackState attackState;
    private bool ignoringCombatForMoveCommand;
    private bool manualMoveCommandActive;
    private Vector3 lastMoveProgressPosition;
    private float lastMoveProgressTime;

    public Transform TargetEnemy => targetEnemy;
    public UnitHealth Health => health;
    public UnitFaction Faction => faction;
    public bool IsDead => health != null && health.IsDead;
    public float MaxHealth => health != null ? health.MaxHealth : 0f;
    public float CurrentHealth => health != null ? health.CurrentHealth : 0f;
    public bool IsManualMoveCommandActive => manualMoveCommandActive;
    public bool HasActiveCombatTarget => !ignoringCombatForMoveCommand && HasValidTarget() && currentState != UnitState.Idle;

    private void Awake()
    {
        ResolveComponents();
        idleState.Initialize(this, movement, attackArea);
        chaseState.Initialize(this, movement, attackArea);
        attackState.Initialize(this, movement, attackArea);
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.Died += HandleDied;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.Died -= HandleDied;
        }
    }

    private void Update()
    {
        if (IsDead)
        {
            movement.Stop(false);
            return;
        }

        UpdateManualMoveCommand();
        if (ignoringCombatForMoveCommand)
        {
            return;
        }

        switch (currentState)
        {
            case UnitState.Idle:
                idleState.Tick();
                break;
            case UnitState.Chase:
                chaseState.Tick();
                break;
            case UnitState.Attack:
                attackState.Tick();
                break;
        }
    }

    public void SetTarget(Transform target, UnitState nextState)
    {
        targetEnemy = target;
        currentState = nextState;
        manualMoveCommandActive = false;
        ignoringCombatForMoveCommand = false;
    }

    public void SetState(UnitState state)
    {
        currentState = state;
    }

    public void ClearTarget()
    {
        targetEnemy = null;
        currentState = UnitState.Idle;
        movement.Stop(false);
    }

    public void HandleMoveCommand()
    {
        ClearTarget();
        manualMoveCommandActive = true;
        ignoringCombatForMoveCommand = true;
        lastMoveProgressPosition = transform.position;
        lastMoveProgressTime = Time.time;
    }

    public void CompleteMoveCommand()
    {
        manualMoveCommandActive = false;
        ignoringCombatForMoveCommand = false;
    }

    public bool HasValidTarget()
    {
        if (targetEnemy == null)
        {
            return false;
        }

        UnitHealth targetHealth = targetEnemy.GetComponentInParent<UnitHealth>();
        return targetHealth != null && !targetHealth.IsDead && attackArea.IsAttackableTarget(this, targetEnemy);
    }

    public void TakeDamage(float damageAmount)
    {
        health.TakeDamage(damageAmount);
    }

    public void Heal(float healAmount)
    {
        health.Heal(healAmount);
    }

    public void SetHealth(float newHealth)
    {
        health.SetHealth(newHealth);
    }

    private void HandleDied(UnitHealth deadHealth)
    {
        targetEnemy = null;
        currentState = UnitState.Idle;
        movement.Stop();
    }

    private void ResolveComponents()
    {
        health = GetOrAdd<UnitHealth>();
        GetOrAdd<UnitDamageAnimation>();
        faction = GetOrAdd<UnitFaction>();
        movement = GetOrAdd<UnitMovement>();
        attackArea = GetComponent<MeleeAttackArea>();
        if (attackArea == null)
        {
            attackArea = GetComponent<RangedAttackArea>();
        }

        if (attackArea == null)
        {
            attackArea = gameObject.AddComponent<MeleeAttackArea>();
        }
        idleState = GetOrAdd<IdleState>();
        chaseState = GetOrAdd<ChaseState>();
        attackState = GetOrAdd<AttackState>();
        GetOrAdd<UnitLineOfSight>();
    }

    private T GetOrAdd<T>() where T : Component
    {
        T component = GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    // Lệnh di chuyển của người chơi có ưu tiên cao nhất: bỏ qua hoàn toàn combat
    // cho tới khi unit tới đích (Unit.MoveTowardsTarget gọi CompleteMoveCommand).
    // Chỉ dừng sớm nếu bị kẹt (không tiến được) để tránh treo vĩnh viễn khi gặp vật cản.
    private void UpdateManualMoveCommand()
    {
        if (!ignoringCombatForMoveCommand)
        {
            return;
        }

        float movedDistance = Vector2.Distance(transform.position, lastMoveProgressPosition);
        if (movedDistance >= MoveProgressThreshold)
        {
            lastMoveProgressPosition = transform.position;
            lastMoveProgressTime = Time.time;
        }
        else if (Time.time - lastMoveProgressTime >= moveCommandStuckTimeout)
        {
            CompleteMoveCommand();
        }
    }

    private void OnValidate()
    {
        moveCommandStuckTimeout = Mathf.Max(0f, moveCommandStuckTimeout);
    }
}
