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
    public GameObject remotePlayerPrefab;
    private GameObject localPlayer;
    private string GUID;
    private Transform localTransform;
    private Transform localRotation;

    public float sendRate = 0.2f;

    private Dictionary<string, GameObject> remotePlayers = new();

    private Dictionary<string, Vector3> remoteTargetPositions = new();
    private Dictionary<string, Vector3> remoteTargetRotations = new();
    private float positionLerpSpeed = 10f;

    private readonly Queue<Action> mainThreadQueue = new Queue<Action>();

    protected override void Start()
    {
        base.Start();
        Debug.Log("[CLIENT] Iniciando cliente...");

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Any, 0));

        serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), port);
        Debug.Log("[CLIENT] Conectando a servidor " + serverEndPoint);

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

        foreach (var kv in remotePlayers)
        {
            string guid = kv.Key;
            GameObject go = kv.Value;
            if (go == null) continue;

            if (remoteTargetPositions.TryGetValue(guid, out Vector3 targetPos))
            {
                go.transform.position = Vector3.Lerp(go.transform.position, targetPos, Time.deltaTime * positionLerpSpeed);
            }

            if (remoteTargetRotations.TryGetValue(guid, out Vector3 targetRot))
            {
                Quaternion targetQ = Quaternion.Euler(targetRot);
                go.transform.rotation = Quaternion.Slerp(go.transform.rotation, targetQ, Time.deltaTime * positionLerpSpeed);
            }
        }
    }

    private void BeginReceive()
    {
        ReceiveState state = new ReceiveState();
        state.socket = socket;

        socket.BeginReceiveFrom(state.buffer, 0, state.buffer.Length, SocketFlags.None,
            ref state.sender, ReceiveCallback, state);
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        var state = (ReceiveState)ar.AsyncState;
        EndPoint from = state.sender;

        try
        {
            int bytes = socket.EndReceiveFrom(ar, ref from);
            string msg = Encoding.UTF8.GetString(state.buffer, 0, bytes);
            Debug.Log("[CLIENT] Mensaje recibido: " + msg);

            OnPacketReceived(msg, from);
        }
        catch (Exception e)
        {
            Debug.LogError("[CLIENT] ReceiveCallback error: " + e.Message);
        }
        finally
        {
            BeginReceive();
        }
    }

    protected override void OnPacketReceived(string msg, EndPoint fromAddress)
    {
        if (string.IsNullOrEmpty(msg)) return;

        if (msg.StartsWith("PLAYER_JOIN_APPROVED|"))
        {
            string json = msg.Substring("PLAYER_JOIN_APPROVED|".Length);
            ClientProxy proxy = JsonUtility.FromJson<ClientProxy>(json);
            Debug.Log("[CLIENT] Conexión aprobada. GUID: " + proxy.guid);

            mainThreadQueue.Enqueue(() => HandleServerJoinApproval(proxy));
            return;
        }

        if (msg.StartsWith("SPAWN_PLAYER|"))
        {
            string json = msg.Substring("SPAWN_PLAYER|".Length);
            ClientProxy proxy = JsonUtility.FromJson<ClientProxy>(json);
            mainThreadQueue.Enqueue(() => SpawnRemotePlayer(proxy));
            return;
        }

        if (msg.StartsWith("EXISTING_PLAYER|"))
        {
            string json = msg.Substring("EXISTING_PLAYER|".Length);
            ClientProxy proxy = JsonUtility.FromJson<ClientProxy>(json);
            mainThreadQueue.Enqueue(() => SpawnRemotePlayer(proxy));
            return;
        }

        if (msg.StartsWith("PLAYER_UPDATE|"))
        {
            string json = msg.Substring("PLAYER_UPDATE|".Length);
            PlayerUpdate update = JsonUtility.FromJson<PlayerUpdate>(json);
            mainThreadQueue.Enqueue(() => ApplyRemotePlayerUpdate(update));
            return;
        }
    }

    private void HandleServerJoinApproval(ClientProxy proxy)
    {
        GUID = proxy.guid;

        localPlayer = Instantiate(localPlayerPrefab, proxy.position, Quaternion.Euler(proxy.rotation));
        Transform controller = localPlayer.transform.Find("First Person Controller");

        if (controller == null)
        {
            Debug.LogError("[CLIENT] No se encontró el hijo 'First Person Controller'");
            return;
        }

        localTransform = controller; 
        localRotation = controller;

        Debug.Log("[CLIENT] Player local instanciado en " + proxy.position);

        StartStateSyncLoop();
    }

    private void SpawnRemotePlayer(ClientProxy proxy)
    {
        if (remotePlayers.ContainsKey(proxy.guid)) return;
        if (proxy.guid == GUID) return;

        GameObject obj = Instantiate(remotePlayerPrefab, proxy.position, Quaternion.Euler(proxy.rotation));
        remotePlayers.Add(proxy.guid, obj);

        Debug.Log("[CLIENT] Remote player creado: " + proxy.guid);
    }

    private void ApplyRemotePlayerUpdate(PlayerUpdate update)
    {
        if (remotePlayers.TryGetValue(update.guid, out GameObject obj))
        {
            remoteTargetPositions[update.guid] = update.position;
            remoteTargetRotations[update.guid] = update.rotation;
        }
    }

    private async void StartStateSyncLoop()
    {
        while (true)
        {
            if (localTransform != null)
                SendPlayerState();
            await System.Threading.Tasks.Task.Delay((int)(sendRate * 1000f));
        }
    }

    private void SendPlayerState()
    {
        PlayerUpdate update = new PlayerUpdate
        {
            type = "update",
            guid = GUID,
            position = localTransform.position,
            rotation = localRotation.eulerAngles
        };

        string json = JsonUtility.ToJson(update);
        byte[] packet = Encoding.UTF8.GetBytes("UPDATE|" + json);

        SendPacket(packet, serverEndPoint);
    }

    protected override void OnConnectionReset(EndPoint fromAddress)
    {
        Debug.Log("[CLIENT] Conexión reseteada por el servidor");
    }
}