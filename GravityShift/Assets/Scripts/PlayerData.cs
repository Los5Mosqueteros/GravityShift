using UnityEngine;
using System;

[Serializable]
public class PlayerData
{
    public string id;
    public string token;
    public string playerName;
    public Vector3 position;
    public Vector3 rotation;
    public string type;         // Spawn, Update, Disconnect

    public int team;

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
