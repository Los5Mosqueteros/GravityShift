using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
}

[Serializable]
public class PlayerUpdate
{
    public string guid;
    public Vector3 position;
    public Vector3 rotation;
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

public class Server : Networking
{
    private Dictionary<string, ClientProxy> clients = new();

    private string EndpointKey(EndPoint ep) => ep.ToString();

    private ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();

    [SerializeField] private TMPro.TextMeshProUGUI logText;

    protected override void Start()
    {
        base.Start();
        Log("[SERVIDOR] Iniciando servidor UDP...");

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Any, port));

        Log($"[SERVIDOR] Socket UDP listo en puerto {port}");
        BeginReceive();
    }

    private void Update()
    {
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
            Log($"[SERVIDOR] Paquete recibido de {from}: {msg}");

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
        Log("[SERVIDOR] Procesando mensaje: " + msg);

        if (msg.StartsWith("PLAYER_JOIN_REQUEST|"))
        {
            string playerName = msg.Substring("PLAYER_JOIN_REQUEST|".Length);
            RegisterNewClient(fromAddress, playerName);
            return;
        }

        if (msg.StartsWith("UPDATE|"))
        {
            string json = msg.Substring("UPDATE|".Length);
            PlayerUpdate update = JsonUtility.FromJson<PlayerUpdate>(json);

            string key = EndpointKey(fromAddress);
            if (clients.TryGetValue(key, out var proxy))
            {
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

            ProcessShoot(shoot);
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

        ClientProxy proxy = new ClientProxy
        {
            address = address,
            guid = guid,
            name = string.IsNullOrWhiteSpace(playerName) ? "Player" + new System.Random().Next(0, 999) : playerName,
            position = Vector3.zero,
            rotation = Vector3.zero,
            team = 0,
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
        Log("[SERVIDOR] Enviada confirmación a " + proxy.address);
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
            rotation = proxy.rotation
        };

        string json = JsonUtility.ToJson(update);
        byte[] packet = Encoding.UTF8.GetBytes("PLAYER_UPDATE|" + json);

        foreach (var kv in clients.Values)
        {
            if (kv.guid == proxy.guid) continue;

            SendPacket(packet, kv.address);
        }
    }

    private void HandleDisconnect(string guid)
    {
        foreach(var kv in clients)
        {
            if(kv.Value.guid == guid)
            {
                Log("[SERVIDOR] Cliente desconectado: " + guid);
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

    private void ProcessShoot(PlayerShoot shoot)
    {
        Log("[SERVIDOR] Procesando disparo de " + shoot.shooterGuid);

        ClientProxy shooter = default;
        foreach(var c in clients.Values)
        {
            if(c.guid == shoot.shooterGuid) shooter = c;
        }

        foreach(var target in clients.Values)
        {
            if(target.guid == shooter.guid) continue;

            Vector3 toTarget = target.position - shoot.origin;

            float dot = Vector3.Dot(shoot.direction.normalized, toTarget.normalized);
            if(dot < 0.90f) continue;

            float distance = toTarget.magnitude;
            if(distance > shoot.maxDistance) continue;

            Log("[SERVIDOR] Hit validado a " + target.guid);

            SendHitResult(shooter.guid, target.guid, true, shoot.damage);
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

    protected override void OnConnectionReset(EndPoint fromAddress)
    {
        string key = EndpointKey(fromAddress);
        clients.Remove(key);
    }

    private void Log(string msg)
    {
        logQueue.Enqueue(msg);
    }
}