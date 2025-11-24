using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    [Header("Projectile Setup")]
    public GameObject projectilePrefab;

    [Header("Projectile Settings")]
    public float defaultSpeed = 20f;
    public float defaultLifetime = 3f;

    public Action<string> OnProjectileSpawnSerialized;

    // Llamar esto cuando se dispare una bala de forma local
    public void SpawnProjectile(int weaponIndex, Vector3 position, Vector3 direction)
    {
        ProjectileData proj = new ProjectileData(
            weaponIndex,
            position,
            direction,
            defaultSpeed,
            defaultLifetime
        );

        string json = JsonUtility.ToJson(proj);

        OnProjectileSpawnSerialized?.Invoke(json);
    }

    //Llamar esta funci�n cuando se reciba un paquete
    public void HandleNetworkMessage(ProjectileData data)
    {
        if (data.type == "projectile")
        {
            SpawnLocal(data.position, data.direction, data.speed, data.lifetime);
        }
    }

    public void SpawnLocal(Vector3 position, Vector3 direction, float speed, float lifetime)
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
