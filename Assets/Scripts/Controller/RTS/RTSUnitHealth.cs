using System;
using UnityEngine;

[DisallowMultipleComponent]
public class RTSUnitHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private Bar healthBar;
    [SerializeField] private bool destroyOnDeath = true;

    private bool isDead;

    public event Action<RTSUnitHealth> Died;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        ResolveHealthBar();
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
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
        currentHealth = Mathf.Clamp(newHealth, 0f, maxHealth);
        RefreshHealthBar(true);

        if (previousHealth > 0f && currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        Died?.Invoke(this);

        if (destroyOnDeath)
        {
            Destroy(gameObject);
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
        int roundedMaxHealth = Mathf.RoundToInt(maxHealth);
        if (animate && healthBar.MaxValue == roundedMaxHealth)
        {
            healthBar.Change(roundedCurrentHealth - healthBar.Value);
            return;
        }

        healthBar.UpdateHealth(currentHealth, maxHealth);
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }
}
