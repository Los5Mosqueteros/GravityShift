using UnityEngine;

public class RifleProjectile : Projectile
{
    [SerializeField] private float damage = 25f;

    // 1. Variable de control para que solo impacte UNA vez
    private bool hasHit = false;

    void OnTriggerEnter(Collider collider)
    {
        // 2. Si la bala ya ha chocado con algo en este frame, NO hacemos nada más
        if (hasHit) return;

        HitBox hitbox = collider.GetComponent<HitBox>();

        if (hitbox != null)
        {
            // 3. Marcamos el impacto INMEDIATAMENTE
            hasHit = true;

            hitbox.OnHit(damage);
            Destroy(gameObject);
        }
        else if (!collider.isTrigger)
        {
            // También marcamos impacto si chocamos con una pared
            hasHit = true;
            Destroy(gameObject);
        }
    }

    public float GetDamage() => damage;
}