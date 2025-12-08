using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;
    private bool isDead;

    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    void Start()
    {
        currentHealth = maxHealth;
        isDead = false;
    }

    public void SetHealth(float value)
    {
        if (isDead) return;

        currentHealth = Mathf.Clamp(value, 0, maxHealth);

        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            gameObject.SetActive(false);
        }
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
    }
}
