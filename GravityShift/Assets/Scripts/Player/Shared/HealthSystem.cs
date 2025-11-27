using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private HitBox[] hitBoxes;
    private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"{gameObject.name} ha recibido {damage} de daño. Salud actual: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} ha muerto");
    }

    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
}
