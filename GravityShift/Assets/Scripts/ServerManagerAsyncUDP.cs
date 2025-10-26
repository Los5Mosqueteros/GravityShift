using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using TMPro;

public class ServerManagerAsyncUDP : MonoBehaviour
{
    public TMP_Text logText;

    private UdpClient udpServer;
    private bool isRunning = false;
    private int port = 5001;

    private Dictionary<IPEndPoint, string> connectedClients = new Dictionary<IPEndPoint, string>();

    private async void Start()
    {
        udpServer = new UdpClient(port);
        isRunning = true;

        Log($"Servidor UDP escuchando en puerto {port}...");
        _ = ListenForClientsAsync();
    }

    private async Task ListenForClientsAsync()
    {
        while (isRunning)
        {
            try
            {
                UdpReceiveResult result = await udpServer.ReceiveAsync();
                string msg = Encoding.UTF8.GetString(result.Buffer);
                IPEndPoint clientEP = result.RemoteEndPoint;

                try
                {
                    PlayerData playerData = JsonUtility.FromJson<PlayerData>(msg);
                    if (!string.IsNullOrEmpty(playerData.playerId))
                    {
                        Log($"[Servidor] Datos recibidos de {playerData.playerName}: Pos {playerData.position.x}, {playerData.position.y}, {playerData.position.z}");
                        await BroadcastAsync($"[Servidor]: {playerData.playerName} está en {playerData.position.x}, {playerData.position.y}, {playerData.position.z}");
                        continue;
                    }
                }
                catch
                {
                    // No es JSON válido de PlayerData, tonses seguimos como mensaje normal :D
                }

                if (!connectedClients.ContainsKey(clientEP))
                {
                    connectedClients[clientEP] = msg;
                    Log($"[Servidor]: {msg} se ha unido al chat. ({clientEP})");
                    await BroadcastAsync($"[Servidor]: {msg} se ha unido al chat.");
                    continue;
                }

                Log(msg);

                if (msg.Contains("se ha ido del chat"))
                {
                    string username = connectedClients[clientEP];
                    Log($"[Servidor]: {username} se ha desconectado.");
                    connectedClients.Remove(clientEP);
                    await BroadcastAsync($"[Servidor]: {username} se ha desconectado.");
                }
                else
                {
                    await BroadcastAsync(msg);
                }
            }
            catch (System.Exception e)
            {
                Log("Error UDP: " + e.Message);
            }
        }
    }

    private async Task BroadcastAsync(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        foreach (var client in connectedClients.Keys)
        {
            try
            {
                await udpServer.SendAsync(data, data.Length, client);
            }
            catch { }
        }
    }

    private void Log(string message)
    {
        Debug.Log(message);
        if (logText != null)
            logText.text += "\n" + message + "\n";
    }

    private void OnApplicationQuit()
    {
        isRunning = false;
        udpServer?.Close();
        connectedClients.Clear();
    }
}
