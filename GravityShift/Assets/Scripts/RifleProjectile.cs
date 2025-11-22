using UnityEngine;

public class RifleProjectile : Projectile
{
    [SerializeField] private float damage = 25f;

    void OnTriggerEnter(Collider collision)
    {
        Destroy(gameObject);
    }

    public float GetDamage() => damage;
}
