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
    private Dictionary<EndPoint, ClientProxy> clients = new();

    protected override void Start()
    {
        base.Start();
        Debug.Log("[SERVER] Iniciando...");

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Any, port));

        Debug.Log("[SERVER] Socket UDP listo en puerto " + port);
        BeginReceive();
    }

    private void BeginReceive()
    {
        var buffer = new byte[1024];
        EndPoint remote = new IPEndPoint(IPAddress.Any, 0);

        ReceiveState state = new ReceiveState();
        state.socket = socket;
        socket.BeginReceiveFrom(
            state.buffer, 0, state.buffer.Length, SocketFlags.None,
            ref state.sender, new AsyncCallback(ReceiveCallback), state
        );
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        ReceiveState state = (ReceiveState)ar.AsyncState;
        EndPoint from = state.sender;

        int bytes = socket.EndReceiveFrom(ar, ref from);
        string msg = Encoding.UTF8.GetString(state.buffer, 0, bytes);
        Debug.Log("[SERVER] Paquete recibido de: " + from);

        OnPacketReceived(msg, from);

        BeginReceive();
    }

    protected override void OnPacketReceived(string msg, EndPoint fromAddress)
    {
        Debug.Log("[SERVER] Mensaje: " + msg);

        if (msg == "PLAYER_JOIN_REQUEST")
        {
            RegisterNewClient(fromAddress);
            return;
        }

        if (msg.StartsWith("UPDATE|"))
        {
            string json = msg.Substring("UPDATE|".Length);
            PlayerUpdate update = JsonUtility.FromJson<PlayerUpdate>(json);

            if (clients.TryGetValue(fromAddress, out var proxy))
            {
                proxy.position = update.position;
                proxy.rotation = update.rotation;
                clients[fromAddress] = proxy;
            }

            return;
        }
    }

    private void RegisterNewClient(EndPoint address)
    {
        string guid = Guid.NewGuid().ToString();

        ClientProxy proxy = new ClientProxy
        {
            address = address,
            guid = guid,
            name = "Player" + UnityEngine.Random.Range(0, 999),
            position = Vector3.zero,
            rotation = Vector3.zero,
            team = 0
        };

        clients[address] = proxy;

        SendJoinApprovalPacket(proxy);
    }


    private void SendJoinApprovalPacket(ClientProxy proxy)
    {
        string json = JsonUtility.ToJson(proxy);
        byte[] packet = Encoding.UTF8.GetBytes("PLAYER_JOIN_APPROVED|" + json);

        SendPacket(packet, proxy.address);
    }

    protected override void OnConnectionReset(EndPoint fromAddress)
    {
        clients.Remove(fromAddress);
    }
}

