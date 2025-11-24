using UnityEngine;
using System;

[Serializable]
public class ProjectileData
{
    public string type = "projectile";
    public string shooterID;
    public int weaponIndex;

    public Vector3 position;
    public Vector3 direction;
    public float speed;
    public float lifetime;

    public ProjectileData(int weaponIndex, Vector3 position, Vector3 direction, float speed, float lifetime)
    {
        //this.shooterID = shooterID;
        this.weaponIndex = weaponIndex;
        this.position = position;
        this.direction = direction;
        this.speed = speed;
        this.lifetime = lifetime;
    }
}
