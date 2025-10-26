using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System;

public class ClientPlayerUDP : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform playerTransform; 
    public string playerName = "Player";

    [Header("Network Settings")]
    public string serverIP = "127.0.0.1";
    public int port = 5001;
    public float sendInterval = 0.20f;

    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;
    private bool isRunning = false;

    private async void Start()
    {
        await ConnectToServer();
    }

    private async Task ConnectToServer()
    {
        try
        {
            udpClient = new UdpClient();
            serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), port);

            Debug.Log($"Conectado al servidor UDP en {serverIP}:{port}");

            byte[] nameData = Encoding.UTF8.GetBytes(playerName);
            await udpClient.SendAsync(nameData, nameData.Length, serverEndPoint);

            isRunning = true;
            _ = ReceiveMessagesAsync();
            _ = SendPlayerDataLoop();
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
            if (playerTransform != null)
            {
                var data = new PlayerData(
                    playerName,
                    playerTransform.position,
                    playerTransform.rotation.eulerAngles
                );

                string json = JsonUtility.ToJson(data);
                byte[] bytes = Encoding.UTF8.GetBytes(json);

                try
                {
                    await udpClient.SendAsync(bytes, bytes.Length, serverEndPoint);
                    Debug.Log($"Enviado: {json}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Error enviando datos: " + e.Message);
                }
            }

            await Task.Delay((int)(sendInterval * 1000));
        }
    }

    private async Task ReceiveMessagesAsync()
    {
        while (isRunning)
        {
            try
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync();
                string msg = Encoding.UTF8.GetString(result.Buffer);
                Debug.Log("Servidor: " + msg);
            }
            catch
            {
                Debug.Log("Desconectado del servidor UDP.");
                isRunning = false;
            }
        }
    }

    private async void OnApplicationQuit()
    {
        isRunning = false;

        try
        {
            if (udpClient != null)
            {
                string msg = playerName + " se ha desconectado.";
                byte[] data = Encoding.UTF8.GetBytes(msg);
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
