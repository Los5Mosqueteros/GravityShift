using UnityEngine;
using System;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;
    private bool isDead;

    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;
    public float MaxHealth => maxHealth;

    // Eventos
    public event Action<float> OnHealthChanged;
    public event Action OnDamageTaken;
    public event Action OnDeath;
    public event Action OnRespawn;

    [Header("Death Settings")]
    public Renderer[] bodyRenderers;
    public Collider[] bodyColliders;
    public MonoBehaviour[] scriptsToDisable;

    void Start()
    {
        if (bodyRenderers == null || bodyRenderers.Length == 0) bodyRenderers = GetComponentsInChildren<Renderer>();
        if (bodyColliders == null || bodyColliders.Length == 0) bodyColliders = GetComponentsInChildren<Collider>();

        ResetHealth();

        bool isLocalPlayer = GetComponent<RemotePlayerController>() == null;

        if (PlayerHUD.Instance != null)
        {
            PlayerHUD.Instance.Initialize(this);
        }
    }

    public void SetHealth(float value)
    {
        if (isDead) return;

        float previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(value, 0, maxHealth);

        if (currentHealth < previousHealth)
        {
            OnDamageTaken?.Invoke();
        }

        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    public void TakeDamage(float amount)
    {
        SetHealth(currentHealth - amount);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        OnHealthChanged?.Invoke(currentHealth);
    }

    private void Die()
    {
        isDead = true;
        OnDeath?.Invoke(); 
        TogglePlayerState(false);
    }

    public void Respawn()
    {
        isDead = false;
        ResetHealth();
        TogglePlayerState(true);
        OnRespawn?.Invoke();
    }

    private void TogglePlayerState(bool state)
    {
        if (bodyRenderers != null)
            foreach (var r in bodyRenderers) if (r) r.enabled = state;

        if (bodyColliders != null)
            foreach (var c in bodyColliders) if (c) c.enabled = state;

        if (scriptsToDisable != null)
            foreach (var s in scriptsToDisable) if (s) s.enabled = state;
    }
}