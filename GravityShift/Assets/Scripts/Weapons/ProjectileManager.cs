using System;
using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    [Header("Projectile Setup")]
    public GameObject projectilePrefab;

    [Header("Projectile Settings")]
    public float defaultSpeed = 20f;
    public float defaultLifetime = 3f;

    public Action<string> OnProjectileSpawnSerialized;

    [Serializable]
    private class ProjectileMessage
    {
        public string type = "spawn_projectile";
        public Vector3 position;
        public Vector3 direction;
        public float speed;
        public float lifetime;
    }

    // Llamar esto cuando se dispare una bala de forma local
    public void SpawnProjectile(Vector3 position, Vector3 direction)
    {

        SpawnLocal(position, direction, defaultSpeed, defaultLifetime);

        ProjectileMessage msg = new ProjectileMessage
        {
            position = position,
            direction = direction,
            speed = defaultSpeed,
            lifetime = defaultLifetime
        };

        string json = JsonUtility.ToJson(msg);

        OnProjectileSpawnSerialized?.Invoke(json);
    }

    // Llamar esta función cuando se reciba un paquete
    public void HandleNetworkMessage(string json)
    {
        ProjectileMessage msg = JsonUtility.FromJson<ProjectileMessage>(json);

        if (msg.type == "spawn_projectile")
        {
            SpawnLocal(msg.position, msg.direction, msg.speed, msg.lifetime);
        }
    }

    private void SpawnLocal(Vector3 position, Vector3 direction, float speed, float lifetime)
    {
        GameObject proj = Instantiate(projectilePrefab, position, Quaternion.LookRotation(direction));
        Projectile projectile = proj.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.speed = speed;
            projectile.lifetime = lifetime;
        }
    }
}
