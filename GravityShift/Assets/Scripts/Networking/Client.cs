using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class Client : Networking
{
    public static Client Instance;

    private EndPoint serverEndPoint;

    [Header("Prefabs")]
    public GameObject localPlayerPrefab;
    public GameObject remotePlayerPrefab;

    private GameObject localPlayer;
    private string GUID;
    private int currentTeam = 0;
    private Transform localTransform;
    private Transform localRotation;

    private bool isAlive = true;

    [Header("Network")]
    public float sendRate = 0.1f;

    private Dictionary<string, GameObject> remotePlayers = new();

    private readonly Queue<Action> mainThreadQueue = new Queue<Action>();

    protected override void Start()
    {
        if(Instance == null)
        {
            Instance = this;   
        }
        else
        {
            Destroy(gameObject);
        }

        base.Start();
        Debug.Log("[CLIENT] Iniciando cliente...");

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Any, 0));

        serverEndPoint = new IPEndPoint(IPAddress.Parse(LobbyUI.SelectedIP), port);
        Debug.Log("[CLIENT] Conectando a servidor " + serverEndPoint);

        byte[] joinRequest = Encoding.UTF8.GetBytes("PLAYER_JOIN_REQUEST|" + LobbyUI.PlayerName);
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
            //Debug.Log("[CLIENT] Mensaje recibido: " + msg);

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
            Debug.Log("[CLIENT] Conexión aprobada. GUID: " + proxy.guid + "Team: " + proxy.team);

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

        if (msg.StartsWith("TEAM_CHANGED|"))
        {
            string json = msg.Substring("TEAM_CHANGED|".Length);
            TeamChangeData teamChange = JsonUtility.FromJson<TeamChangeData>(json);
            mainThreadQueue.Enqueue(() => HandleTeamChange(teamChange));
            return;
        }

        if (msg.StartsWith("PLAYER_LEFT|"))
        {
            string guid = msg.Substring("PLAYER_LEFT|".Length);
            mainThreadQueue.Enqueue(() => 
            {
                if(remotePlayers.TryGetValue(guid, out var obj))
                {
                    Destroy(obj);
                    remotePlayers.Remove(guid);
                }
            });
        }

        if (msg.StartsWith("HIT_RESULT|"))
        {
            string json = msg.Substring("HIT_RESULT|".Length);
            HitResult result = JsonUtility.FromJson<HitResult>(json);

            if(result.shooterGuid == GUID)
            {
                if (result.hit)
                {
                    Debug.Log("[CLIENT] Has impactado a " + result.targetGuid);
                }
                else
                {
                    Debug.Log("[CLIENT] Disparo fallado");
                }   
            }

            if(result.targetGuid == GUID && result.hit)
            {
                mainThreadQueue.Enqueue(() =>
                {
                    var hitBox = localPlayer.GetComponentInChildren<HitBox>();
                    if(hitBox != null)
                    {
                        hitBox.OnHit(result.damage);
                        Debug.Log("[CLIENT] Has recibido {result.damage} de daño de {result.shooterGuid}");
                    }
                    else
                    {
                        Debug.LogWarning("[CLIENT] No se encontró HitBox en localPlayer");
                    }
                });
            }

            return;
        }

        if (msg.StartsWith("HEALTH_UPDATE|"))
        {
            string json = msg.Substring("HEALTH_UPDATE|".Length);
            HealthUpdate update = JsonUtility.FromJson<HealthUpdate>(json);

            mainThreadQueue.Enqueue(() =>
            {
               if(update.guid == GUID)
                {
                    var health = localPlayer.GetComponent<HealthSystem>();
                    if (health != null) health.SetHealth(update.health);
                } 
            });
        }

        if (msg.StartsWith("PLAYER_DIED|"))
        {
            string json = msg.Substring("PLAYER_DIED|".Length);
            PlayerDeathData dead = JsonUtility.FromJson<PlayerDeathData>(json);

            mainThreadQueue.Enqueue(() => HandlePlayerDeath(dead.guid));
        }

        if (msg.StartsWith("PLAYER_RESPAWN|"))
        {
            string json = msg.Substring("PLAYER_RESPAWN|".Length);
            PlayerRespawnData data = JsonUtility.FromJson<PlayerRespawnData>(json);

            mainThreadQueue.Enqueue(() => HandleRespawn(data));
        }
    }

    private void HandleServerJoinApproval(ClientProxy proxy)
    {
        GUID = proxy.guid;
        currentTeam = proxy.team;

        localPlayer = Instantiate(localPlayerPrefab, proxy.position, Quaternion.Euler(proxy.rotation));
        Transform controller = localPlayer.transform.Find("First Person Controller");

        if (controller == null)
        {
            Debug.LogError("[CLIENT] No se encontró el hijo 'First Person Controller'");
            return;
        }

        localTransform = controller; 
        localRotation = controller;

        PlayerAppearance appearance = localPlayer.GetComponentInChildren<PlayerAppearance>();
        if (appearance != null)
        {
            appearance.SetTeamColor(currentTeam);
        }
        Debug.Log("[CLIENT] Player local instanciado en " + proxy.position);

        StartStateSyncLoop();
    }

    private void HandlePlayerDeath(string guid)
    {
        if(guid == GUID)
        {
            isAlive = false;
            localPlayer.SetActive(false);
        }
        else if(remotePlayers.TryGetValue(guid, out var obj))
        {
            obj.SetActive(false);
        }
    }

    private void SpawnRemotePlayer(ClientProxy proxy)
    {
        if (remotePlayers.ContainsKey(proxy.guid)) return;
        if (proxy.guid == GUID) return;

        GameObject obj = Instantiate(remotePlayerPrefab, proxy.position, Quaternion.Euler(proxy.rotation));

        var controller = obj.GetComponent<RemotePlayerController>();
        if(controller == null)
        {
            controller = obj.AddComponent<RemotePlayerController>();
            controller.positionLerpSpeed = 10f;
            controller.rotationLerpSpeed = 10f;
            controller.maxExtrapolation = 0.2f;
        }
        controller.SetTarget(proxy.position, proxy.rotation);

        var tag = obj.GetComponentInChildren<PlayerNameTag>();
        if(tag != null) tag.SetName(proxy.name);

        var appearance = obj.GetComponentInChildren<PlayerAppearance>();
        if (appearance != null)
        {
            appearance.SetTeamColor(proxy.team);
        }

        remotePlayers.Add(proxy.guid, obj);

        Debug.Log("[CLIENT] Remote player creado: " + proxy.guid + "Team: " + proxy.team);
    }

    private void ApplyRemotePlayerUpdate(PlayerUpdate update)
    {
        if (!remotePlayers.TryGetValue(update.guid, out GameObject obj)) return;

        var controller = obj.GetComponent<RemotePlayerController>();
        if(controller != null)
        {
            controller.SetTarget(update.position, update.rotation);
        }
        else
        {
            obj.transform.position = update.position;
            obj.transform.rotation = Quaternion.Euler(update.rotation);
        }
    }

    private void HandleTeamChange(TeamChangeData teamChange)
    {
        Debug.Log($"[CLIENT] Jugador {teamChange.guid} cambió al equipo {teamChange.team}");

        if (teamChange.guid == GUID)
        {
            currentTeam = teamChange.team;

            PlayerAppearance appearance = localPlayer?.GetComponentInChildren<PlayerAppearance>();
            if (appearance != null)
            {
                appearance.SetTeamColor(currentTeam);
                Debug.Log("[CLIENT] Tu equipo cambió a: " + currentTeam);
            }
        }
        else if (remotePlayers.TryGetValue(teamChange.guid, out GameObject obj))
        {
            PlayerAppearance appearance = obj.GetComponentInChildren<PlayerAppearance>();
            if (appearance != null)
            {
                appearance.SetTeamColor(teamChange.team);
            }
        }
    }

    private async void StartStateSyncLoop()
    {
        while (true)
        {
            if (localTransform != null) SendPlayerState();
            await System.Threading.Tasks.Task.Delay((int)(sendRate * 1000f));
        }
    }

    private void SendPlayerState()
    {
        if(!isAlive) return;

        PlayerUpdate update = new PlayerUpdate
        {
            guid = GUID,
            position = localTransform.position,
            rotation = localRotation.eulerAngles,
            team = currentTeam,
        };

        string json = JsonUtility.ToJson(update);
        byte[] packet = Encoding.UTF8.GetBytes("UPDATE|" + json);

        SendPacket(packet, serverEndPoint);
    }

    public void RequestTeamChange(int newTeam)
    {
        if (string.IsNullOrEmpty(GUID))
        {
            Debug.LogWarning("[CLIENT] No se puede cambiar de equipo: GUID no asignado");
            return;
        }

        TeamChangeData teamChange = new TeamChangeData
        {
            guid = GUID,
            team = newTeam
        };

        string json = JsonUtility.ToJson(teamChange);
        byte[] packet = Encoding.UTF8.GetBytes("CHANGE_TEAM|" + json);

        SendPacket(packet, serverEndPoint);
        Debug.Log($"[CLIENT] Solicitando cambio al equipo {newTeam}");
    }

    private void HandleRespawn(PlayerRespawnData data)
    {
        if (data.guid == GUID)
        {
            isAlive = true;

            localPlayer.transform.position = data.position;
            localPlayer.SetActive(true);

            var health = localPlayer.GetComponent<HealthSystem>();
            if (health != null) health.ResetHealth();
        }
        else
        {
            if (remotePlayers.TryGetValue(data.guid, out var obj))
            {
                obj.SetActive(true);
                obj.transform.position = data.position;
            }
        }
    }

    protected override void OnConnectionReset(EndPoint fromAddress)
    {
        Debug.Log("[CLIENT] Conexión reseteada por el servidor");
    }

    private void OnApplicationQuit()
    {
        Debug.Log("[CLIENT] Saliendo del juego, enviando disconnect...");
        SendDisconnect();
    }

    private void OnDisable()
    {
        if(socket != null) SendDisconnect();
    }

    private void SendDisconnect()
    {
        try
        {
            byte[] packet = Encoding.UTF8.GetBytes("DISCONNECT|" + GUID);
            SendPacket(packet, serverEndPoint);
        }
        catch{}

        try
        {
            socket?.Close();
        }
        catch{}
    }

    public void PublicSendPacket(byte[] outputPacket, EndPoint toAddress)
    {
        SendPacket(outputPacket, toAddress);
    }

    public string GetGUID() => GUID;
    public int GetCurrentTeam() => currentTeam;
    public EndPoint GetServerEndPoint() => serverEndPoint;
}