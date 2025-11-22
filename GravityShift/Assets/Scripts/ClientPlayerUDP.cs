using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

public class ClientPlayerUDP : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform playerTransform; 
    private string playerName = "Player";
    public GameObject remotePlayerPrefab;
    public GameObject localPlayerPrefab;

    [Header("Network Settings")]
    private string serverIP = "127.0.0.1";
    public int port = 5001;
    public float sendInterval = 0.2f;

    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;
    private bool isRunning = false;

    private string localToken;
    private string ID = null;

    private Dictionary<string, GameObject> remotePlayers = new Dictionary<string, GameObject>();

    private async void Start()
    {
        playerName = PlayerPrefs.GetString("playerName", "Player");
        serverIP = PlayerPrefs.GetString("serverIP", "127.0.0.1");

        localToken = Guid.NewGuid().ToString();
        await ConnectToServer();
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
                    playerTransform.rotation.eulerAngles, 
                    "update"
                );
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

    private async Task ReceiveMessages()
    {
        while (isRunning)
        {
            try
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync();
                string msg = Encoding.UTF8.GetString(result.Buffer);
                //Debug.Log("Servidor: " + msg);

                PlayerData data = JsonUtility.FromJson<PlayerData>(msg);
                HandleMessage(data);
            }
            catch
            {
                isRunning = false;
                Debug.Log("Desconectado del servidor UDP.");
            }
        }
    }

    private void HandleMessage(PlayerData data)
    {
        if (data == null) return;

        if (ID == null && data.type == "spawn" && !string.IsNullOrEmpty(data.token) && data.token == localToken)
        {
            ID = data.id;
            Debug.Log($"Mi GUID asignado por el servidor: {ID}");

            GameObject local = Instantiate(localPlayerPrefab);
            playerTransform = local.transform;

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

            case "disconnect":
                RemoveRemotePlayer(data.id);
                break;
        }
    }

    private void SpawnRemotePlayer(PlayerData data)
    {
        if (string.IsNullOrEmpty(data.id)) return;
        if (remotePlayers.ContainsKey(data.id)) return;

        GameObject remote = Instantiate(remotePlayerPrefab);
        remote.name = "Player_" + data.id;
        remote.transform.position = data.position;
        remote.transform.rotation = Quaternion.Euler(data.rotation);

        remotePlayers.Add(data.id, remote);

        Debug.Log($"Spawn remoto: {data.id} ({data.playerName})");
    }

    private void UpdateRemotePlayer(PlayerData data)
    {
        if (string.IsNullOrEmpty(data.id)) return;

        if (!remotePlayers.TryGetValue(data.id, out GameObject p)) return;

        var remote = p.GetComponent<RemotePlayerController>();
        if(remote != null) remote.SetTarget(data.position, data.rotation);
    }

    private void RemoveRemotePlayer(string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        if (remotePlayers.TryGetValue(id, out GameObject p))
        {
            Destroy(p);
            remotePlayers.Remove(id);
            Debug.Log($"Jugador remoto {id} desconectado y destruido.");
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
