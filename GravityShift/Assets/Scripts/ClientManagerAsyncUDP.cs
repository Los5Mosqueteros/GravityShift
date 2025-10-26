using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using System.Collections.Generic;
using System;

public class ClientManagerAsyncUDP : MonoBehaviour
{
    public TMP_InputField userNameInput;
    public TMP_InputField messageInput;
    public TMP_InputField ipInput;
    public TMP_Text logText;
    public TMP_Text usernameDisplay;

    public List<GameObject> gameObjectsDeactivate;
    public List<GameObject> gameObjectsActivate;

    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;
    private bool isRunning = false;
    private string username;

    public async void ConnectToServer()
    {
        string ip = string.IsNullOrEmpty(ipInput.text) ? "127.0.0.1" : ipInput.text;
        int port = 5001;
        username = userNameInput.text;

        try
        {
            udpClient = new UdpClient();
            serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            Log("Conectado al servidor UDP en " + ip);

            foreach (GameObject go in gameObjectsDeactivate)
                go.SetActive(false);

            byte[] nameData = Encoding.UTF8.GetBytes(username);
            await udpClient.SendAsync(nameData, nameData.Length, serverEndPoint);
            await Task.Delay(100);

            usernameDisplay.text = "USERNAME: " + username;

            foreach (GameObject go in gameObjectsActivate)
                go.SetActive(true);

            isRunning = true;
            _ = ReceiveMessagesAsync();
        }
        catch (System.Exception e)
        {
            Log("Error al conectar: " + e.Message);
        }
    }

    public async void SendMessageToServer(string customMessage = "")
    {
        if (udpClient == null) return;

        string msg = string.IsNullOrEmpty(customMessage)
            ? messageInput.text
            : customMessage;

        string fullMsg = $"[{username}]: {msg}";
        byte[] data = Encoding.UTF8.GetBytes(fullMsg);

        try
        {
            await udpClient.SendAsync(data, data.Length, serverEndPoint);
            messageInput.text = "";
        }
        catch (System.Exception e)
        {
            Log("Error enviando mensaje: " + e.Message);
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
                Log(msg);
            }
            catch
            {
                Log("Desconectado del servidor UDP.");
                break;
            }
        }
    }

    public async void SendPlayerData(PlayerData playerData)
    {
        if (udpClient == null) return;

        try
        {
            string json = SerializerUtility.ToJson(playerData);
            byte[] data = Encoding.UTF8.GetBytes(json);
            await udpClient.SendAsync(data, data.Length, serverEndPoint);
            Log($"Datos del jugador enviados: {playerData.playerName}");
        }
        catch (Exception e)
        {
            Log("Error enviando datos del jugador: " + e.Message);
        }
    }

    private void Log(string message)
    {
        Debug.Log(message);
        if (logText != null)
            logText.text += "\n" + message + "\n";
    }

    private async void OnApplicationQuit()
    {
        isRunning = false;

        try
        {
            if (udpClient != null)
            {
                string msg = username + " se ha ido del chat.";
                byte[] data = Encoding.UTF8.GetBytes(msg);
                await udpClient.SendAsync(data, data.Length, serverEndPoint);
                await Task.Delay(200);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Error al enviar mensaje de salida UDP: " + e.Message);
        }
        finally
        {
            udpClient?.Close();
        }
    }
}
