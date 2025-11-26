using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public struct ClientProxy
{
    public EndPoint address;
    public string guid;
    public string name;

    public Vector3 position;
    public Vector3 rotation;

    public int team;
}


public class Client : Networking
{
    private EndPoint serverEndPoint;

    protected override void Start()
    {
        base.Start();
        Debug.Log("[CLIENT] Iniciando cliente");

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), port);

        Debug.Log("[CLIENT] Conectando a servidor " + serverEndPoint);

        byte[] hello = Encoding.UTF8.GetBytes("HELLO");
        SendPacket(hello, serverEndPoint);

        BeginReceive();
    }

    private void BeginReceive()
    {
        ReceiveState state = new ReceiveState();
        state.socket = socket;

        socket.BeginReceiveFrom(
            state.buffer, 0, state.buffer.Length, SocketFlags.None,
            ref state.sender, ReceiveCallback, state
        );
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        ReceiveState state = (ReceiveState)ar.AsyncState;
        EndPoint from = state.sender;

        socket.EndReceiveFrom(ar, ref from);
        Debug.Log("[CLIENT] Packet recibido de: " + from);

        OnPacketReceived(state.buffer, from);

        BeginReceive();
    }

    protected override void OnPacketReceived(byte[] inputPacket, EndPoint fromAddress)
    {
        string msg = Encoding.UTF8.GetString(inputPacket).TrimEnd('\0');
        Debug.Log("[CLIENT] Mensaje recibido: " + msg);

        if (msg.StartsWith("WELCOME|"))
        {
            string json = msg.Substring("WELCOME|".Length);
            Debug.Log("[CLIENT] Bienvenida recibida del server: " + json);
        }
    }

    private void SendHelloPacket()
    {
        var hello = Encoding.UTF8.GetBytes("HELLO");
        SendPacket(hello, serverEndPoint);
    }

    protected override void OnConnectionReset(EndPoint fromAddress)
    {
        Debug.Log("Conexión reseteada por el servidor");
    }
}