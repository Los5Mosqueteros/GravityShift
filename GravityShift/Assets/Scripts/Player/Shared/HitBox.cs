using UnityEngine;

public class HitBox : MonoBehaviour
{
    [Tooltip("Pon 1 para cuerpo, 2 para cabeza")]
    [SerializeField] private float damageMultiplier = 1.0f;

    private HealthSystem healthSystem;

    void Start()
    {
        healthSystem = GetComponentInParent<HealthSystem>();
    }

    public void TakeDamage(float damage)
    {
        if (healthSystem == null) return;
        
        float finalDamage = damage * damageMultiplier;
        //healthSystem.TakeDamage(finalDamage);
    }

    public void OnHit(float damage)
    {
        TakeDamage(damage);
    }
}