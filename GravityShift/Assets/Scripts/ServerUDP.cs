using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

public class ServerUDP : MonoBehaviour
{
    [Header("Server Settings")]
    public int port = 5001;
    public bool showLogs = true;

    [Header("Log Settings")]
    public int maxLogMessages = 20; 
    private Queue<string> logQueue = new Queue<string>();


    [Header("UI")]
    public TMPro.TMP_Text logText;

    private UdpClient udpServer;
    private bool isRunning = false;

    private Dictionary<IPEndPoint, string> clientIDs = new Dictionary<IPEndPoint, string>();

    private class PlayerInfo
    {
        public string guid;
        public string name;
        public Vector3 pos;
        public Vector3 rot;
    }
    private Dictionary<string, PlayerInfo> clientInfos = new Dictionary<string, PlayerInfo>();

    private async void Start()
    {
        StartServer();
    }

    private async void StartServer()
    {
        try
        {
            udpServer = new UdpClient(port);
            isRunning = true;

            Log($"Servidor UDP iniciado en puerto {port}");

            await ListenForClients();
        }
        catch (Exception e)
        {
            Log("Error al iniciar servidor: " + e.Message);
        }
    }

    private async Task ListenForClients()
    {
        while (isRunning)
        {
            try
            {
                UdpReceiveResult result = await udpServer.ReceiveAsync();
                string msg = Encoding.UTF8.GetString(result.Buffer);
                IPEndPoint sender = result.RemoteEndPoint;

                PlayerData data = null;
                try
                {
                    data = JsonUtility.FromJson<PlayerData>(msg);
                }
                catch { }

                if (!clientIDs.ContainsKey(sender))
                {
                    if (data == null)
                    {
                        Log($"Cliente nuevo desde {sender} envió mensaje no JSON: {msg}");
                        continue;
                    }

                    string guid = Guid.NewGuid().ToString();
                    clientIDs[sender] = guid;

                    Log($"Nuevo cliente conectado desde {sender.Address}:{sender.Port} -> GUID {guid}");

                    PlayerData spawnForSelf = new PlayerData(guid, data.playerName, Vector3.zero, Vector3.zero, "spawn");
                    spawnForSelf.token = data.token;
                    await SendTo(sender, JsonUtility.ToJson(spawnForSelf));

                    foreach (var kv in clientInfos)
                    {
                        var info = kv.Value;
                        PlayerData existing = new PlayerData(info.guid, info.name, info.pos, info.rot, "spawn");
                        await SendTo(sender, JsonUtility.ToJson(existing));
                    }

                    PlayerData spawnForOthers = new PlayerData(guid, data.playerName, Vector3.zero, Vector3.zero, "spawn");
                    await BroadcastExcept(JsonUtility.ToJson(spawnForOthers), sender);

                    PlayerInfo pi = new PlayerInfo() { guid = guid, name = data.playerName, pos = Vector3.zero, rot = Vector3.zero };
                    clientInfos[guid] = pi;

                    continue;
                }

                if (data != null)
                {
                    string guid = clientIDs[sender];

                    if (data.type == "update")
                    {
                        if (clientInfos.TryGetValue(guid, out PlayerInfo info))
                        {
                            info.pos = data.position;
                            info.rot = data.rotation;
                        }
                        else
                        {
                            clientInfos[guid] = new PlayerInfo() { guid = guid, name = data.playerName, pos = data.position, rot = data.rotation };
                        }

                        data.id = guid;
                        data.type = "update";
                        string jsonUpdate = JsonUtility.ToJson(data);
                        await Broadcast(jsonUpdate);
                    }
                    else if (data.type == "disconnect")
                    {
                        Log($"Cliente {guid} solicita desconexión.");

                        PlayerData disc = new PlayerData(guid, data.playerName, Vector3.zero, Vector3.zero, "disconnect");
                        string jsonDisc = JsonUtility.ToJson(disc);
                        await BroadcastExcept(jsonDisc, sender);

                        clientInfos.Remove(guid);
                        clientIDs.Remove(sender);
                    }
                }
                else
                {
                    Log($"Mensaje recibido (texto): {msg}");
                }
            }
            catch (Exception e)
            {
                Log("Error en servidor UDP: " + e.Message);
            }
        }
    }

    private async Task Broadcast(string msg)
    {
        byte[] data = Encoding.UTF8.GetBytes(msg);

        foreach (var client in clientIDs.Keys)
        {
            try
            {
                await udpServer.SendAsync(data, data.Length, client);
            }
            catch (Exception e)
            {
                Log($"Error enviando a {client.Address}: {e.Message}");
            }
        }
    }

    private async Task BroadcastExcept(string msg, IPEndPoint except)
    {
        byte[] data = Encoding.UTF8.GetBytes(msg);

        foreach (var client in clientIDs.Keys)
        {
            if (client.Equals(except)) continue;
            try
            {
                await udpServer.SendAsync(data, data.Length, client);
            }
            catch (Exception e)
            {
                Log($"Error enviando a {client.Address}:{client.Port} -> {e.Message}");
            }
        }
    }

    private async Task SendTo(IPEndPoint end, string msg)
    {
        byte[] data = Encoding.UTF8.GetBytes(msg);
        try
        {
            await udpServer.SendAsync(data, data.Length, end);
        }
        catch (Exception e)
        {
            Log($"Error enviando a {end.Address}:{end.Port} -> {e.Message}");
        }
    }

    private void Log(string msg)
    {
        if (!showLogs) return;

        Debug.Log(msg);

        if (logText != null)
        {
            logQueue.Enqueue(msg);

            while (logQueue.Count > maxLogMessages)
            {
                logQueue.Dequeue();
            }

            logText.text = string.Join("\n", logQueue);
        }
    }


    private void OnApplicationQuit()
    {
        isRunning = false;
        udpServer?.Close();
        Log("Servidor UDP cerrado.");
    }
}
