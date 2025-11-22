using System.Collections.Generic;
using UnityEngine;

public class TeamManager
{
    private const int maxTeams = 4;
    private const int maxPlayersPerTeam = 3;

    private Dictionary<string, int> playerTeams = new Dictionary<string, int>();

    public int AssignTeam(string playerId)
    {
        for (int team = 1; team <= maxTeams; team++)
        {
            if (GetTeamCount(team) < maxPlayersPerTeam)
            {
                playerTeams[playerId] = team;
                return team;
            }
        }

        return -1; // Todos llenos
    }

    public bool ChangeTeam(string playerId, int newTeam)
    {
        if (newTeam < 1 || newTeam > maxTeams) return false;

        if (GetTeamCount(newTeam) >= maxPlayersPerTeam) return false;

        playerTeams[playerId] = newTeam;
        return true;
    }

    public void RemovePlayer(string playerId)
    {
        if (playerTeams.ContainsKey(playerId))
        {
            playerTeams.Remove(playerId);
        }
    }

    public int GetTeam(string playerId)
    {
        return playerTeams.ContainsKey(playerId) ? playerTeams[playerId] : -1;
    }

    public int GetTeamCount(int team)
    {
        int count = 0;
        foreach (var kv in playerTeams)
        {
            if (kv.Value == team) count++;
        }
        return count;
    }

    public bool HasTeam(string playerId)
    {
        return playerTeams.ContainsKey(playerId);
    }
}
