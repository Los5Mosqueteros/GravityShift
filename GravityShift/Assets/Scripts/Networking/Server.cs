using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Collections;
using UnityEngine;

public class ReceiveState
{
    public Socket socket;
    public byte[] buffer = new byte[4096];
    public EndPoint sender = new IPEndPoint(IPAddress.Any, 0);
}

public struct ClientProxy
{
    public EndPoint address;
    public string guid;
    public string name;

    public Vector3 position;
    public Vector3 rotation;

    public int team;
    public float health;
    public bool isDead;
}

[Serializable]
public class PlayerUpdate
{
    public string guid;
    public Vector3 position;
    public Vector3 rotation;
    public int team;
}

[Serializable]
public class PlayerShoot
{
    public string shooterGuid;

    public Vector3 origin;
    public Vector3 direction;
    public float maxDistance;
    public float damage;

    public float timestamp;
}

[Serializable]
public class HitResult
{
    public bool hit;
    public string shooterGuid;
    public string targetGuid;
    public float damage;
}

[Serializable]
public class HealthUpdate
{
    public string guid;
    public float health;
}

[Serializable]
public class TeamChangeData
{
    public string guid;
    public int team;
    public Vector3 position;
}

[Serializable]
public class PlayerDeathData
{
    public string guid;
}

[Serializable]
public class PlayerRespawnData
{
    public string guid;
    public Vector3 position;
    public int team;
}

public class Server : Networking
{
    private Dictionary<string, ClientProxy> clients = new();

    private TeamManager teamManager = new TeamManager();

    private string EndpointKey(EndPoint ep) => ep.ToString();

    private ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();
    private readonly Queue<Action> mainThreadActions = new Queue<Action>();

    [SerializeField] private TMPro.TextMeshProUGUI logText;
    [Header("Spawn Manager")]
    [SerializeField] private SpawnManager spawnManager;

    protected override void Start()
    {
        base.Start();
        spawnManager = FindFirstObjectByType<SpawnManager>();
        if (spawnManager == null) Debug.LogError("[SERVER] SpawnManager NO encontrado en la escena");

        Log("[SERVIDOR] Iniciando servidor UDP...");

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Any, port));

        Log($"[SERVIDOR] Socket UDP listo en puerto {port}");
        BeginReceive();
    }

    private void Update()
    {
        while(mainThreadActions.Count > 0)
        {
            mainThreadActions.Dequeue()?.Invoke();
        }

        while(logQueue.TryDequeue(out string msg))
        {
            Debug.Log(msg);

            if(logText != null) logText.text += msg + "\n";
        }
    }

    private void BeginReceive()
    {
        ReceiveState state = new ReceiveState();
        state.socket = socket;

        socket.BeginReceiveFrom(state.buffer, 0, state.buffer.Length, SocketFlags.None, ref state.sender, ReceiveCallback, state);
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        var state = (ReceiveState)ar.AsyncState;
        EndPoint from = state.sender;

        try
        {
            int bytes = socket.EndReceiveFrom(ar, ref from);
            string msg = Encoding.UTF8.GetString(state.buffer, 0, bytes);
            //Log($"[SERVIDOR] Paquete recibido de {from}: {msg}");

            OnPacketReceived(msg, from);
        }
        catch (Exception e)
        {
            Log($"[SERVIDOR] Error en ReceiveCallback: {e.Message}");
        }
        finally
        {
            BeginReceive();
        }
    }

    protected override void OnPacketReceived(string msg, EndPoint fromAddress)
    {
        //Log("[SERVIDOR] Procesando mensaje: " + msg);

        if (msg.StartsWith("PLAYER_JOIN_REQUEST|"))
        {
            string playerName = msg.Substring("PLAYER_JOIN_REQUEST|".Length);
            mainThreadActions.Enqueue(() => RegisterNewClient(fromAddress, playerName));
            return;
        }

        if (msg.StartsWith("UPDATE|"))
        {
            string json = msg.Substring("UPDATE|".Length);
            PlayerUpdate update = JsonUtility.FromJson<PlayerUpdate>(json);

            string key = EndpointKey(fromAddress);
            if (clients.TryGetValue(key, out var proxy))
            {
                if(proxy.isDead) return;

                proxy.position = update.position;
                proxy.rotation = update.rotation;
                clients[key] = proxy;

                BroadcastPlayerUpdate(proxy);
            }
            else
            {
                Log("[SERVIDOR] UPDATE recibido de cliente no registrado: " + fromAddress);
            }

            return;
        }

        if (msg.StartsWith("CHANGE_TEAM|"))
        {
            string json = msg.Substring("CHANGE_TEAM|".Length);
            TeamChangeData teamChange = JsonUtility.FromJson<TeamChangeData>(json);
            HandleTeamChange(teamChange.guid, teamChange.team);
            return;
        }

        if (msg.StartsWith("DISCONNECT|"))
        {
            string guid = msg.Substring("DISCONNECT|".Length);
            HandleDisconnect(guid);
            return;
        }

        if (msg.StartsWith("SHOOT|"))
        {
            string json = msg.Substring("SHOOT|".Length);
            PlayerShoot shoot = JsonUtility.FromJson<PlayerShoot>(json);

            HandleShoot(shoot);
            return;
        }
    }

    private void RegisterNewClient(EndPoint address, string playerName)
    {
        string key = EndpointKey(address);
        if (clients.ContainsKey(key))
        {
            Log("[SERVIDOR] Cliente ya registrado: " + key);
            return;
        }

        string guid = Guid.NewGuid().ToString();
        int assignedTeam = teamManager.AssignTeam(guid);
        Vector3 spawnPosition = GetSpawnPosition(assignedTeam);

        ClientProxy proxy = new ClientProxy
        {
            address = address,
            guid = guid,
            name = string.IsNullOrWhiteSpace(playerName) ? "Player" + new System.Random().Next(0, 999) : playerName,
            position = spawnPosition,
            rotation = Vector3.zero,
            team = assignedTeam,
            health = 100
        };

        clients[key] = proxy;

        Log("[SERVIDOR] Cliente registrado: " + key + " GUID: " + guid);

        SendExistingPlayers(proxy, key);
        SendJoinApprovalPacket(proxy);
        BroadcastNewPlayer(proxy, key);
    }

    private void SendJoinApprovalPacket(ClientProxy proxy)
    {
        string json = JsonUtility.ToJson(proxy);
        byte[] packet = Encoding.UTF8.GetBytes("PLAYER_JOIN_APPROVED|" + json);
        SendPacket(packet, proxy.address);
        Log("[SERVIDOR] Enviada confirmación a " + proxy.address + ", TEAM " + proxy.team);
    }

    private void SendExistingPlayers(ClientProxy newProxy, string newKey)
    {
        foreach (var kv in clients)
        {
            if (kv.Key == newKey) continue;
            string json = JsonUtility.ToJson(kv.Value);
            byte[] packet = Encoding.UTF8.GetBytes("EXISTING_PLAYER|" + json);
            SendPacket(packet, newProxy.address);
        }
    }

    private void BroadcastNewPlayer(ClientProxy newPlayer, string newKey)
    {
        string json = JsonUtility.ToJson(newPlayer);
        byte[] packet = Encoding.UTF8.GetBytes("SPAWN_PLAYER|" + json);

        foreach (var kv in clients)
        {
            if (kv.Key == newKey) continue;
            SendPacket(packet, kv.Value.address);
        }
    }

    private void BroadcastPlayerUpdate(ClientProxy proxy)
    {
        PlayerUpdate update = new PlayerUpdate
        {
            guid = proxy.guid,
            position = proxy.position,
            rotation = proxy.rotation,
            team = proxy.team
        };

        string json = JsonUtility.ToJson(update);
        byte[] packet = Encoding.UTF8.GetBytes("PLAYER_UPDATE|" + json);

        foreach (var kv in clients.Values)
        {
            if (kv.guid == proxy.guid) continue;

            SendPacket(packet, kv.address);
        }
    }

    private void HandleTeamChange(string guid, int newTeam)
    {
        string targetKey = null;
        ClientProxy targetProxy = default;

        foreach (var kv in clients)
        {
            if (kv.Value.guid == guid)
            {
                targetKey = kv.Key;
                targetProxy = kv.Value;
                break;
            }
        }

        if (targetKey == null)
        {
            Log($"[SERVIDOR] No se encontró cliente con GUID {guid} para cambio de equipo");
            return;
        }

        teamManager.ChangeTeam(guid, newTeam);
        Vector3 newSpawnPosition = GetSpawnPosition(newTeam);

        targetProxy.team = newTeam;
        targetProxy.position = newSpawnPosition;
        clients[targetKey] = targetProxy;

        Log($"[SERVIDOR] Jugador {guid} ({targetProxy.name}) cambió al equipo {newTeam}");

        BroadcastTeamChange(guid, newTeam, newSpawnPosition);
    }

    private void BroadcastTeamChange(string guid, int team, Vector3 spawnPosition)
    {
        TeamChangeData teamChange = new TeamChangeData
        {
            guid = guid,
            team = team,
            position = spawnPosition
        };

        string json = JsonUtility.ToJson(teamChange);
        byte[] packet = Encoding.UTF8.GetBytes("TEAM_CHANGED|" + json);

        foreach (var c in clients.Values)
        {
            SendPacket(packet, c.address);
        }
    }

    private void HandleDisconnect(string guid)
    {
        foreach(var kv in clients)
        {
            if(kv.Value.guid == guid)
            {
                Log("[SERVIDOR] Cliente desconectado: " + guid);
                teamManager.RemovePlayer(guid);
                BroadcastPlayerRemoval(guid);
                clients.Remove(kv.Key);
                break;
            }
        }
    }

    private void BroadcastPlayerRemoval(string guid)
    {
        byte[] packet = Encoding.UTF8.GetBytes("PLAYER_LEFT|" + guid);

        foreach(var c in clients.Values)
        {
            SendPacket(packet, c.address);
        }
    }

    private void HandleShoot(PlayerShoot shoot)
    {
        Log("[SERVIDOR] Procesando disparo de " + shoot.shooterGuid);

        ClientProxy shooter = default;
        foreach(var c in clients.Values)
        {
            if(c.guid == shoot.shooterGuid) shooter = c;
        }

        foreach(var kv in clients)
        {
            var target = kv.Value;
            if(target.guid == shooter.guid) continue;

            Vector3 toTarget = target.position - shoot.origin;

            float dot = Vector3.Dot(shoot.direction.normalized, toTarget.normalized);
            if(dot < 0.90f) continue;

            float distance = toTarget.magnitude;
            if(distance > shoot.maxDistance) continue;

            target.health -= shoot.damage;

            Log("[SERVIDOR] {target.guid} vida: {target.health}");

            clients[kv.Key] = target;

            SendHitResult(shooter.guid, target.guid, true, shoot.damage);

            if(target.health <= 0 && !target.isDead)
            {
                HandlePlayerDeath(target);
            }
            else
            {
                SendHealthUpdate(target);
            }
            
            return;
        }

        SendHitResult(shooter.guid, "", false, 0);
    }

    private void SendHitResult(string shooterGuid, string targetGuid, bool hit, float damage)
    {
        HitResult result = new HitResult
        {
            hit = hit,
            shooterGuid = shooterGuid,
            targetGuid = targetGuid,
            damage = damage
        };

        string json = JsonUtility.ToJson(result);
        byte[] packet = Encoding.UTF8.GetBytes("HIT_RESULT|" + json);

        foreach(var c in clients.Values)
        {
            if(c.guid == shooterGuid) SendPacket(packet, c.address);

            if(hit && c.guid == targetGuid) SendPacket(packet, c.address);
        }
    }

    private void SendHealthUpdate(ClientProxy proxy)
    {
        HealthUpdate update = new HealthUpdate
        {
            guid = proxy.guid,
            health = proxy.health
        };

        string json = JsonUtility.ToJson(update);
        byte[] packet = Encoding.UTF8.GetBytes("HEALTH_UPDATE|" + json);

        foreach(var c in clients.Values)
        {
            SendPacket(packet, c.address);
        }
    }

    private void HandlePlayerDeath(ClientProxy deadPlayer)
    {
        Log($"[SERVIDOR] Jugador {deadPlayer.guid} ha muerto.");

        deadPlayer.isDead = true;
        deadPlayer.health = 0;

        foreach(var kv in clients)
        {
            if(kv.Value.guid == deadPlayer.guid)
            {
                clients[kv.Key] = deadPlayer;
                break;
            }
        }

        SendPlayerDeath(deadPlayer.guid);

        mainThreadActions.Enqueue(() => StartCoroutine(RespawnAfterDelay(deadPlayer.guid, 5f)));
    }

    private void SendPlayerDeath(string guid)
    {
        PlayerDeathData data = new PlayerDeathData { guid = guid };
        string json = JsonUtility.ToJson(data);
        byte[] packet = Encoding.UTF8.GetBytes("PLAYER_DIED|" + json);

        foreach (var c in clients.Values) SendPacket(packet, c.address);
    }

    private IEnumerator RespawnAfterDelay(string guid, float delay)
    {
        yield return new WaitForSeconds(delay);

        string key = null;
        ClientProxy proxy = default;

        foreach (var kv in clients)
        {
            if (kv.Value.guid == guid)
            {
                key = kv.Key;
                proxy = kv.Value;
                break;
            }
        }

        if (key == null) yield break;

        proxy.isDead = false;
        proxy.health = 100;
        proxy.position = GetSpawnPosition(proxy.team);

        clients[key] = proxy;

        BroadcastPlayerRespawn(proxy);
    }

    protected override void OnConnectionReset(EndPoint fromAddress)
    {
        string key = EndpointKey(fromAddress);
        if (clients.TryGetValue(key, out var proxy))
        {
            Log($"[SERVIDOR] Conexión reseteada: {fromAddress} | GUID: {proxy.guid}");
            teamManager.RemovePlayer(proxy.guid);
        }
        clients.Remove(key);
    }

    private void Log(string msg)
    {
        logQueue.Enqueue(msg);
    }
    
    private Vector3 GetSpawnPosition(int team)
    {
        if (spawnManager == null)
        {
            Log($"[SERVIDOR] Warning: SpawnManager no asignado, usando posición por defecto");
            return Vector3.zero;
        }

        Vector3 spawnPos = spawnManager.GetSpawnPosition(team);

        if (spawnPos == Vector3.zero)
        {
            Log($"[SERVIDOR] Warning: No hay spawns configurados para el equipo {team}");
        }

        return spawnPos;
    }

    private void BroadcastPlayerRespawn(ClientProxy proxy)
    {
        PlayerRespawnData data = new PlayerRespawnData
        {
            guid = proxy.guid,
            position = proxy.position,
            team = proxy.team
        };

        string json = JsonUtility.ToJson(data);
        byte[] packet = Encoding.UTF8.GetBytes("PLAYER_RESPAWN|" + json);

        foreach (var c in clients.Values)
        {
            SendPacket(packet, c.address);
        }
    }
}