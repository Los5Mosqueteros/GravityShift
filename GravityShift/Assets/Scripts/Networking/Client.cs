using System;
using System.Collections.Generic;
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

[Serializable]
public class PlayerUpdate
{
    public string type;
    public string guid;
    public Vector3 position;
    public Vector3 rotation;
}

public class Client : Networking
{
    private EndPoint serverEndPoint;

    public GameObject localPlayerPrefab;
    private GameObject localPlayer;
    private string GUID;
    private Transform localTransform;
    private Transform localRotation;

    public float sendRate = 0.2f;

    private readonly Queue<Action> mainThreadQueue = new Queue<Action>();

    protected override void Start()
    {
        base.Start();
        Debug.Log("[CLIENT] Iniciando cliente");

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Any, 0));
        
        serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), port);

        Debug.Log("[CLIENT][NET] Conectando a servidor " + serverEndPoint);

        byte[] joinRequest = Encoding.UTF8.GetBytes("PLAYER_JOIN_REQUEST");
        SendPacket(joinRequest, serverEndPoint);

        BeginReceive();
    }

    private void Update()
    {
        while (mainThreadQueue.Count > 0)
        {
            var action = mainThreadQueue.Dequeue();
            action.Invoke();
        }
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

        int bytes = socket.EndReceiveFrom(ar, ref from);
        string msg = Encoding.UTF8.GetString(state.buffer, 0, bytes);
        Debug.Log("[CLIENT][NET] Mensaje recibido: " + msg);

        OnPacketReceived(msg, from);

        BeginReceive();
    }

    protected override void OnPacketReceived(string msg, EndPoint fromAddress)
    {
        Debug.Log("[CLIENT][NET] Mensaje recibido: " + msg);

        if (msg.StartsWith("PLAYER_JOIN_APPROVED|"))
        {
            string json = msg.Substring("PLAYER_JOIN_APPROVED|".Length);

            ClientProxy proxy = JsonUtility.FromJson<ClientProxy>(json);
            Debug.Log("[CLIENT][NET] Conexion aprobada por el servidor. GUID: " + proxy.guid);

            mainThreadQueue.Enqueue(() => HandleServerJoinApproval(proxy));
            return;
        }
    }

    private void HandleServerJoinApproval(ClientProxy proxy)
    {
        GUID = proxy.guid;

        localPlayer = Instantiate(
            localPlayerPrefab,
            proxy.position,
            Quaternion.Euler(proxy.rotation)
        );

        localTransform = localPlayer.transform;
        localRotation = localPlayer.transform;

        Debug.Log("[CLIENT] Player local instanciado en " + proxy.position);

        BeginStateSyncLoop();
    }

    private async void BeginStateSyncLoop()
    {
        while (true)
        {
            if (localTransform != null)
            {
                SendPlayerState();
            }

            await System.Threading.Tasks.Task.Delay((int)(sendRate * 1000f));
        }
    }

    private void SendPlayerState()
    {
        PlayerUpdate update = new PlayerUpdate();
        update.type = "update";
        update.guid = GUID;
        update.position = localTransform.position;
        update.rotation = localRotation.eulerAngles;

        string json = JsonUtility.ToJson(update);
        byte[] packet = Encoding.UTF8.GetBytes("UPDATE|" + json);

        SendPacket(packet, serverEndPoint);
    }

    private void SendPingPacket()
    {
        var ping = Encoding.UTF8.GetBytes("PING");
        SendPacket(ping, serverEndPoint);
    }

    protected override void OnConnectionReset(EndPoint fromAddress)
    {
        Debug.Log("Conexi�n reseteada por el servidor");
    }
}