using System;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class UnitHealth : MonoBehaviour, IMaxHealthModifiable
{
    // Đổi tên từ "maxHealth" - FormerlySerializedAs giữ lại giá trị đã tinh chỉnh trên prefab cũ.
    [FormerlySerializedAs("maxHealth")]
    [SerializeField] private float baseMaxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [Tooltip("Giáp: trừ thẳng vào mọi nguồn sát thương trong TakeDamage.")]
    [SerializeField] private float defense = 0f;
    [SerializeField] private Bar healthBar;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float deathDestroyDelay = 1f;

    private bool isDead;
    private float maxHealthMultiplier = 1f;
    private float shieldAmount;
    private float shieldExpireTime;

    public event Action<UnitHealth, float> Damaged;
    public event Action<UnitHealth> Died;

    // Máu tối đa hiệu lực = gốc x hệ số hiệu ứng (vd 50% khi ra khỏi lãnh thổ pha lê đen).
    public float MaxHealth => baseMaxHealth * maxHealthMultiplier;
    public float CurrentHealth => currentHealth;
    public float Defense => defense;
    public bool IsDead => isDead;

    // Khiên máu ảo còn lại (0 nếu hết hạn) - hấp thụ sát thương trước khi trừ vào máu.
    public float CurrentShield => Time.time < shieldExpireTime ? shieldAmount : 0f;

    public float MaxHealthMultiplier
    {
        get => maxHealthMultiplier;
        set
        {
            maxHealthMultiplier = Mathf.Max(0.01f, value);
            currentHealth = Mathf.Min(currentHealth, MaxHealth);
            RefreshHealthBar(true);
        }
    }

    private void Awake()
    {
        ResolveHealthBar();
        currentHealth = Mathf.Clamp(currentHealth, 0f, MaxHealth);
        RefreshHealthBar(false);
    }

    /// <summary>
    /// Áp chỉ số GỐC từ bộ chỉ số trung tâm (<see cref="UnitStatsSO"/>) - sinh ra với máu đầy.
    /// Hệ số nhân runtime (hiệu ứng) vẫn chồng lên qua <see cref="MaxHealthMultiplier"/> như cũ.
    /// </summary>
    public void ApplyBaseStats(float newMaxHealth, float newDefense)
    {
        baseMaxHealth = Mathf.Max(1f, newMaxHealth);
        defense = Mathf.Max(0f, newDefense);
        currentHealth = MaxHealth;
        RefreshHealthBar(false);
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead || damageAmount <= 0f)
        {
            return;
        }

        // Giáp giảm trừ tập trung tại đây để mọi nguồn sát thương (đòn thường, kỹ năng,
        // bẫy...) đều được tính như nhau; khiên máu ảo hấp thụ phần còn lại trước máu thật.
        float mitigatedDamage = CombatFormulas.MitigateByDefense(damageAmount, defense);
        ApplyDamageThroughShield(mitigatedDamage);
    }

    /// <summary>
    /// Sát thương xuyên giáp (bỏ qua DEF), vẫn qua khiên rồi tới máu. Dùng cho đòn kết liễu
    /// muốn chắc chắn hạ gục bất kể chỉ số phòng thủ của mục tiêu.
    /// </summary>
    public void TakeTrueDamage(float damageAmount)
    {
        if (isDead || damageAmount <= 0f)
        {
            return;
        }

        ApplyDamageThroughShield(damageAmount);
    }

    private void ApplyDamageThroughShield(float damageAfterDefense)
    {
        float remainingDamage = AbsorbWithShield(damageAfterDefense);
        if (remainingDamage > 0f)
        {
            SetHealth(currentHealth - remainingDamage);
        }
    }

    /// <summary>Tạo khiên máu ảo trong duration giây (lấy giá trị lớn hơn nếu khiên cũ còn).</summary>
    public void AddShield(float amount, float duration)
    {
        if (isDead || amount <= 0f)
        {
            return;
        }

        shieldAmount = Mathf.Max(CurrentShield, amount);
        shieldExpireTime = Time.time + duration;
    }

    private float AbsorbWithShield(float incomingDamage)
    {
        float activeShield = CurrentShield;
        if (activeShield <= 0f)
        {
            return incomingDamage;
        }

        float absorbed = Mathf.Min(activeShield, incomingDamage);
        shieldAmount = activeShield - absorbed;
        return incomingDamage - absorbed;
    }

    public void Heal(float healAmount)
    {
        if (isDead || healAmount <= 0f)
        {
            return;
        }

        SetHealth(currentHealth + healAmount);
    }

    public void SetHealth(float newHealth)
    {
        if (isDead)
        {
            return;
        }

        float previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(newHealth, 0f, MaxHealth);
        RefreshHealthBar(true);

        if (previousHealth > 0f && currentHealth <= 0f)
        {
            Die();
            return;
        }

        float damageTaken = previousHealth - currentHealth;
        if (damageTaken > 0f)
        {
            Damaged?.Invoke(this, damageTaken);
        }
    }

    private void Die()
    {
        isDead = true;
        Died?.Invoke(this);

        if (destroyOnDeath)
        {
            Destroy(gameObject, deathDestroyDelay);
        }
    }

    private void ResolveHealthBar()
    {
        if (healthBar != null)
        {
            return;
        }

        healthBar = GetComponentInChildren<Bar>(true);
        if (healthBar == null)
        {
            healthBar = GetComponentInParent<Bar>();
        }
    }

    private void RefreshHealthBar(bool animate)
    {
        if (healthBar == null)
        {
            return;
        }

        int roundedCurrentHealth = Mathf.RoundToInt(currentHealth);
        int roundedMaxHealth = Mathf.RoundToInt(MaxHealth);
        if (animate && healthBar.MaxValue == roundedMaxHealth)
        {
            healthBar.Change(roundedCurrentHealth - healthBar.Value);
            return;
        }

        healthBar.UpdateHealth(currentHealth, MaxHealth);
    }

    private void OnValidate()
    {
        baseMaxHealth = Mathf.Max(1f, baseMaxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, MaxHealth);
        defense = Mathf.Max(0f, defense);
        deathDestroyDelay = Mathf.Max(0f, deathDestroyDelay);
    }
}
