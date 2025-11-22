using UnityEngine;

public class PistolProjectile : Projectile
{
    [SerializeField] private float damage = 15f;

    void OnTriggerEnter(Collider collision)
    {
        Destroy(gameObject);
        //test
    }

    public float GetDamage() => damage;
}
