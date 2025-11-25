using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

public class RemotePlayerBundle
{
    public GameObject obj;
    public RemotePlayerController controller;
    public RemoteWeaponSystem weaponSystem;
}

public class ClientPlayerUDP : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform playerTransform;
    public Transform playerRotation;
    private string playerName = "Player";
    public GameObject remotePlayerPrefab;
    public GameObject localPlayerPrefab;
    private WeaponHolder localWeaponSystem;

    [Header("Network Settings")]
    private string serverIP = "127.0.0.1";
    public int port = 5001;
    public float sendInterval = 0.2f;

    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;
    private bool isRunning = false;

    private string localToken;
    private string ID = null;

    private Dictionary<string, RemotePlayerBundle> remotePlayers = new();

    [Serializable]
    public class BaseMessage
    {
        public string type;
    }

    private async void Start()
    {
        playerName = PlayerPrefs.GetString("playerName", "Player");
        serverIP = PlayerPrefs.GetString("serverIP", "127.0.0.1");

        localToken = Guid.NewGuid().ToString();
        await ConnectToServer();

        projectileManager.OnProjectileSpawnSerialized += SendProjectileData;
    }

    private async Task ConnectToServer()
    {
        try
        {
            udpClient = new UdpClient();
            serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), port);

            Debug.Log($"Conectado al servidor UDP en {serverIP}:{port}");

            PlayerData connect = new PlayerData("", playerName, Vector3.zero, Vector3.zero, "connect");
            connect.token = localToken;

            string firstPacket = JsonUtility.ToJson(connect);
            byte[] data = Encoding.UTF8.GetBytes(firstPacket);
            await udpClient.SendAsync(data, data.Length, serverEndPoint);

            isRunning = true;
            _ = ReceiveMessages();
        }
        catch (Exception e)
        {
            Debug.LogError("Error al conectar al servidor UDP: " + e.Message);
        }
    }

    private async Task SendPlayerDataLoop()
    {
        while (isRunning)
        {
            if (playerTransform != null && ID != null)
            {
                var data = new PlayerData(
                    ID, 
                    playerName, 
                    playerTransform.position, 
                    playerRotation.rotation.eulerAngles, 
                    "update"
                );

                data.weaponIndex = localWeaponSystem.currentWeaponIndex;
                data.aiming = localWeaponSystem.isAiming;
                data.shooting = localWeaponSystem.isShooting;

                string json = JsonUtility.ToJson(data);
                byte[] bytes = Encoding.UTF8.GetBytes(json);

                try
                {
                    await udpClient.SendAsync(bytes, bytes.Length, serverEndPoint);
                    //Debug.Log($"Enviado: {json}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Error enviando datos: " + e.Message);
                }
            }

            await Task.Delay((int)(sendInterval * 1000));
        }
    }

    private async void SendProjectileData(string json)
    {
        if(!isRunning) return;

        byte[] bytes = Encoding.UTF8.GetBytes(json);

        try
        {
            await udpClient.SendAsync(bytes, bytes.Length, serverEndPoint);
        }
        catch(Exception e)
        {
            Debug.LogWarning("Error enviando proyectil: " + e.Message);
        }
    }

    private async Task ReceiveMessages()
    {
        while (isRunning)
        {
            try
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync();
                string msg = Encoding.UTF8.GetString(result.Buffer);

                BaseMessage baseMsg = JsonUtility.FromJson<BaseMessage>(msg);

                if(baseMsg == null || string.IsNullOrEmpty(baseMsg.type)) continue;

                switch (baseMsg.type)
                {
                    case "spawn":
                    case "update":
                    case "disconnect":
                    case "changeTeam":
                        PlayerData player = JsonUtility.FromJson<PlayerData>(msg);
                        HandlePlayerMessage(player);
                        break;

                    case "projectile":
                        ProjectileData proj = JsonUtility.FromJson<ProjectileData>(msg);
                        HandleProjectileMessage(proj);
                        break;

                    default:
                        Debug.LogWarning("Mensaje desconocido: " + baseMsg.type);
                        break;
                }
            }
            catch(Exception e)
            {
                Debug.Log("Error: " + e.Message);
            }
        }
    }

    private void HandlePlayerMessage(PlayerData data)
    {
        if (data == null) return;

        if (ID == null && data.type == "spawn" && !string.IsNullOrEmpty(data.token) && data.token == localToken)
        {
            ID = data.id;
            Debug.Log($"Mi GUID asignado por el servidor: {ID}");

            GameObject local = Instantiate(localPlayerPrefab, data.position, Quaternion.Euler(data.rotation));

            playerTransform = local.GetComponentInChildren<FirstPersonMovement>().transform;
            playerRotation = local.transform;

            localWeaponSystem = local.GetComponentInChildren<WeaponHolder>();

            _ = SendPlayerDataLoop();

            return;
        }

        if (!string.IsNullOrEmpty(data.id) && data.id == ID) return;

        switch (data.type)
        {
            case "spawn":
                SpawnRemotePlayer(data);
                break;

            case "update":
                UpdateRemotePlayer(data);
                break;

            case "changeTeam":
                if (remotePlayers.TryGetValue(data.id, out var bundle))
                {
                    bundle.controller?.SetTarget(data.position, data.rotation);

                    PlayerAppearance appearance = bundle.obj.GetComponentInChildren<PlayerAppearance>();
                    if (appearance != null)
                    {
                        appearance.SetTeamColor(data.team);
                    }
                }
                break;

            case "disconnect":
                RemoveRemotePlayer(data.id);
                break;
        }
    }

    private void HandleProjectileMessage(ProjectileData data)
    {
        if (data == null) return;

        ProjectileManager.Instance.HandleNetworkMessage(data);
    }

    private void SpawnRemotePlayer(PlayerData data)
    {
        if (string.IsNullOrEmpty(data.id)) return;
        if (remotePlayers.ContainsKey(data.id)) return;

        GameObject remote = Instantiate(remotePlayerPrefab, data.position, Quaternion.Euler(data.rotation));

        var bundle = new RemotePlayerBundle();
        bundle.obj = remote;
        bundle.controller = remote.GetComponent<RemotePlayerController>();
        bundle.weaponSystem = remote.GetComponent<RemoteWeaponSystem>();

        PlayerNameTag nameTag = remote.GetComponentInChildren<PlayerNameTag>();
        if(nameTag != null)
        {
            nameTag.SetName(data.playerName);
        }

        PlayerAppearance appearance = remote.GetComponentInChildren<PlayerAppearance>();
        if (appearance != null)
        {
            appearance.SetTeamColor(data.team);
        }


        remotePlayers.Add(data.id, bundle);

        Debug.Log($"Spawn remoto: {data.id} ({data.playerName})");
    }

    private void UpdateRemotePlayer(PlayerData data)
    {
        if (string.IsNullOrEmpty(data.id)) return;
        if (!remotePlayers.TryGetValue(data.id, out var bundle)) return;

        bundle.controller?.SetTarget(data.position, data.rotation);
        bundle.weaponSystem?.SetWeapon(data.weaponIndex);
        bundle.weaponSystem?.SetAiming(data.aiming);
        bundle.weaponSystem?.SetShooting(data.shooting);
    }

    private void RemoveRemotePlayer(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (remotePlayers.TryGetValue(id, out RemotePlayerBundle bundle))
        {
            if (bundle.obj != null)
                Destroy(bundle.obj);

            remotePlayers.Remove(id);
            Debug.Log($"Jugador remoto {id} desconectado y destruido.");
        }
    }

    public async void RequestTeamChange(int newTeam)
    {
        if (ID == null) return;

        PlayerData change = new PlayerData(ID, playerName, Vector3.zero, Vector3.zero, "changeTeam");
        change.team = newTeam;

        string json = JsonUtility.ToJson(change);
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        try
        {
            await udpClient.SendAsync(bytes, bytes.Length, serverEndPoint);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Error enviando solicitud de cambio de equipo: " + e.Message);
        }
    }

    private async void OnApplicationQuit()
    {
        isRunning = false;

        try
        {
            if (udpClient != null)
            {
                PlayerData disconnectData = new PlayerData(
                    ID,
                    playerName,
                    Vector3.zero,
                    Vector3.zero,
                    "disconnect"
                );

                string json = JsonUtility.ToJson(disconnectData);
                byte[] data = Encoding.UTF8.GetBytes(json);

                await udpClient.SendAsync(data, data.Length, serverEndPoint);

                await Task.Delay(100);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Error al cerrar conexión UDP: " + e.Message);
        }
        finally
        {
            udpClient?.Close();
        }
    }
}
