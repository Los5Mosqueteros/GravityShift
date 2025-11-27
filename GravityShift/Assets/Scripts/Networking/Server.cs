using System;
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

public class Server : Networking
{
    private Dictionary<string, ClientProxy> clients = new();

    private string EndpointKey(EndPoint ep) => ep.ToString();

    protected override void Start()
    {
        base.Start();
        Debug.Log("[SERVIDOR] Iniciando servidor UDP...");

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Any, port));

        Debug.Log($"[SERVIDOR] Socket UDP listo en puerto {port}");
        BeginReceive();
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
            Debug.Log($"[SERVIDOR] Paquete recibido de {from}: {msg}");

            OnPacketReceived(msg, from);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SERVIDOR] Error en ReceiveCallback: {e.Message}");
        }
        finally
        {
            BeginReceive();
        }
    }

    protected override void OnPacketReceived(string msg, EndPoint fromAddress)
    {
        Debug.Log("[SERVIDOR] Procesando mensaje: " + msg);

        if (msg == "PLAYER_JOIN_REQUEST")
        {
            RegisterNewClient(fromAddress);
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
                Debug.LogWarning("[SERVIDOR] UPDATE recibido de cliente no registrado: " + fromAddress);
            }

            return;
        }

        if (msg.StartsWith("DISCONNECT|"))
        {
            string guid = msg.Substring("DISCONNECT|".Length);
            HandleDisconnect(guid);
            return;
        }
    }

    private void RegisterNewClient(EndPoint address)
    {
        string key = EndpointKey(address);
        if (clients.ContainsKey(key))
        {
            Debug.Log("[SERVIDOR] Cliente ya registrado: " + key);
            return;
        }

        string guid = Guid.NewGuid().ToString();

        ClientProxy proxy = new ClientProxy
        {
            address = address,
            guid = guid,
            name = "Player" + new System.Random().Next(0, 999),
            position = Vector3.zero,
            rotation = Vector3.zero,
            team = 0
        };

        clients[key] = proxy;

        Debug.Log("[SERVIDOR] Cliente registrado: " + key + " GUID: " + guid);

        SendExistingPlayers(proxy, key);
        SendJoinApprovalPacket(proxy);
        BroadcastNewPlayer(proxy, key);
    }

    private void SendJoinApprovalPacket(ClientProxy proxy)
    {
        string json = JsonUtility.ToJson(proxy);
        byte[] packet = Encoding.UTF8.GetBytes("PLAYER_JOIN_APPROVED|" + json);
        SendPacket(packet, proxy.address);
        Debug.Log("[SERVIDOR] Enviada confirmación a " + proxy.address);
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
            type = "update",
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
                Debug.Log("[SERVIDOR] Cliente desconectado: " + guid);
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

    protected override void OnConnectionReset(EndPoint fromAddress)
    {
        string key = EndpointKey(fromAddress);
        clients.Remove(key);
    }
}