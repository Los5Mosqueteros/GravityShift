using System;
using UnityEngine;

[Serializable]
public class PlayerData
{
    public string playerId;
    public string playerName;
    public Vector3Serializable position;
    public int health;
    public bool isGravityInverted;

    public PlayerData(string id, string name, Vector3 pos)
    {
        playerId = id;
        playerName = name;
        position = new Vector3Serializable(pos);
        health = 100;
        isGravityInverted = false;
    }

    public void ToggleGravity()
    {
        isGravityInverted = !isGravityInverted;
    }
}
