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
    [SerializeField] private Bar healthBar;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float deathDestroyDelay = 1f;

    private bool isDead;
    private float maxHealthMultiplier = 1f;

    public event Action<UnitHealth, float> Damaged;
    public event Action<UnitHealth> Died;

    // Máu tối đa hiệu lực = gốc x hệ số hiệu ứng (vd 50% khi ra khỏi lãnh thổ pha lê đen).
    public float MaxHealth => baseMaxHealth * maxHealthMultiplier;
    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;

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

    public void TakeDamage(float damageAmount)
    {
        if (isDead || damageAmount <= 0f)
        {
            return;
        }

        SetHealth(currentHealth - damageAmount);
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
        deathDestroyDelay = Mathf.Max(0f, deathDestroyDelay);
    }
}
