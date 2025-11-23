using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("Spawn Points por Equipo")]
    public List<Transform> team1Spawns;
    public List<Transform> team2Spawns;
    public List<Transform> team3Spawns;
    public List<Transform> team4Spawns;

    public Vector3 GetSpawnPosition(int team)
    {
        List<Transform> spawnList = null;

        switch (team)
        {
            case 1: spawnList = team1Spawns; break;
            case 2: spawnList = team2Spawns; break;
            case 3: spawnList = team3Spawns; break;
            case 4: spawnList = team4Spawns; break;
            default: return Vector3.zero;
        }

        if (spawnList == null || spawnList.Count == 0)
            return Vector3.zero;

        // Elegir spawn aleatorio
        Transform spawnPoint = spawnList[Random.Range(0, spawnList.Count)];
        return spawnPoint.position;
    }
}
