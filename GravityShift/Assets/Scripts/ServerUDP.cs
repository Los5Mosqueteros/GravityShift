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

    private List<IPEndPoint> connectedClients = new List<IPEndPoint>();

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

                if (!connectedClients.Exists(c => c.Address.Equals(sender.Address)))
                {
                    connectedClients.Add(sender);
                    Log($"Nuevo cliente conectado: {sender.Address}:{sender.Port}");
                }

                PlayerData data = null;
                try
                {
                    data = JsonUtility.FromJson<PlayerData>(msg);
                }
                catch { }

                if (data != null && !string.IsNullOrEmpty(data.playerName))
                {
                    Log($"{data.playerName} Pos: {data.position} Rot: {data.rotation}");

                    await BroadcastMessageAsync(msg);
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

    private async Task BroadcastMessageAsync(string msg)
    {
        byte[] data = Encoding.UTF8.GetBytes(msg);

        foreach (var client in connectedClients)
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

    private void Log(string message)
    {
        if (!showLogs) return;

        Debug.Log(message);

        if (logText != null)
        {
            logQueue.Enqueue(message);

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
