using UnityEngine;
using System;

[Serializable]
public class PlayerData
{
    public string playerName;
    public Vector3 position;
    public Vector3 rotation;

    public PlayerData(string name, Vector3 pos, Vector3 rot)
    {
        playerName = name;
        position = pos;
        rotation = rot;
    }
}
