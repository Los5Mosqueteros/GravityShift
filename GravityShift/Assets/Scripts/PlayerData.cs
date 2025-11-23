using UnityEngine;
using System;

[Serializable]
public class PlayerData
{
    public string id;
    public string playerName;
    public Vector3 position;
    public Vector3 rotation;

    public string type;         // Spawn, Update, Disconnect
    public string token;

    public int team;

    public int weaponIndex;     // 0 = Knife, 1 = Pistol, 2 = Rifle
    public bool aiming;
    public bool shooting;

    public PlayerData(string id, string playerName, Vector3 position, Vector3 rotation, string type)
    {
        this.id = id;
        this.token = "";
        this.playerName = playerName;
        this.position = position;
        this.rotation = rotation;
        this.type = type;
    }
}
