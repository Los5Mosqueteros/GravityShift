using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private HitBox[] hitBoxes;
    [SerializeField] private float respawnDelay = 0.1f;
    private float currentHealth;
    private bool isDead = false;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
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
        if (isDead) return;
        isDead = true;

        Debug.Log($"{gameObject.name} ha muerto");


        Invoke(nameof(RequestRespawn), respawnDelay);
    }

    private void RequestRespawn()
    {
        Client client = Client.Instance;
        if (client != null)
        {
            client.RequestRespawn();
        }
        else
        {
            Debug.LogError("[HealthSystem] No se encontró instancia de Client");
        }
    }
    public void Respawn(Vector3 position)
    {
        Debug.Log($"[HealthSystem] Respawneando en {position}");
        
        isDead = false;
        currentHealth = maxHealth;       

        Transform controller = transform.Find("First Person Controller");
        if (controller != null)
        {
            controller.position = position;
        }
        else
        {
            transform.position = position;
        }
    }

    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }


}
