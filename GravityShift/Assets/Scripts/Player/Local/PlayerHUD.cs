using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections;

public class PlayerHUD : MonoBehaviour
{
    public static PlayerHUD Instance { get; private set; }

    [Header("Health References")]
    public Slider healthBar;
    public Image damageVignette;

    [Header("Vignette Settings")]
    public float vignetteFadeSpeed = 2f;
    [Range(0, 1)] public float maxVignetteAlpha = 0.8f;

    [Header("Death Screen")]
    public GameObject deathPanel;      
    public TextMeshProUGUI deathText;   
    public float respawnTime = 5f;     

    private HealthSystem currentHealthSystem;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (deathPanel != null) deathPanel.SetActive(false);

        if (damageVignette != null)
        {
            Color c = damageVignette.color;
            c.a = 0f;
            damageVignette.color = c;
        }
    }

    public void Initialize(HealthSystem healthSystem)
    {
        if (currentHealthSystem != null)
        {
            currentHealthSystem.OnHealthChanged -= UpdateHealthBar;
            currentHealthSystem.OnDamageTaken -= TriggerHitEffect;
            currentHealthSystem.OnDeath -= HandleDeath;
        }

        currentHealthSystem = healthSystem;

        currentHealthSystem.OnHealthChanged += UpdateHealthBar;
        currentHealthSystem.OnDamageTaken += TriggerHitEffect;
        currentHealthSystem.OnDeath += HandleDeath;

        if (healthBar != null)
        {
            healthBar.maxValue = currentHealthSystem.MaxHealth;
            healthBar.value = currentHealthSystem.CurrentHealth;
        }

        if (deathPanel != null) deathPanel.SetActive(false);

        UpdateHealthBar(currentHealthSystem.CurrentHealth);
    }

    private void OnDestroy()
    {
        if (currentHealthSystem != null)
        {
            currentHealthSystem.OnHealthChanged -= UpdateHealthBar;
            currentHealthSystem.OnDamageTaken -= TriggerHitEffect;
            currentHealthSystem.OnDeath -= HandleDeath;
        }
    }


    private void UpdateHealthBar(float currentHealth)
    {
        if (healthBar != null) healthBar.value = currentHealth;
    }

    private void TriggerHitEffect()
    {
        if (damageVignette == null) return;

        StopAllCoroutines(); 
        StartCoroutine(FlashVignette());
    }

    private IEnumerator FlashVignette()
    {
        Color color = damageVignette.color;

        color.a = maxVignetteAlpha;
        damageVignette.color = color;

        while (damageVignette.color.a > 0f)
        {
            color.a -= Time.deltaTime * vignetteFadeSpeed;
            damageVignette.color = color;
            yield return null;
        }

        color.a = 0f;
        damageVignette.color = color;
    }

    private void HandleDeath()
    {
        if (deathPanel != null)
        {
            deathPanel.SetActive(true);

            StopAllCoroutines();
            if (damageVignette != null)
            {
                Color c = damageVignette.color;
                c.a = 0f;
                damageVignette.color = c;
            }

            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        float timer = respawnTime;

        while (timer > 0)
        {
            if (deathText != null)
                deathText.text = $"YOU DIED\nRespawning in {timer:F0}...";

            yield return new WaitForSeconds(1f);
            timer--;
        }

        if (deathPanel != null) deathPanel.SetActive(false);

        if (currentHealthSystem != null)
        {
            currentHealthSystem.Respawn();
        }
    }
}