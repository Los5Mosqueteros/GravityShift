using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public abstract class Networking : MonoBehaviour
{
    protected Socket socket;
    protected int port = 5001;

    protected virtual void Start() { }

    protected virtual void OnPacketReceived(string msg, EndPoint fromAddress) { }
    protected virtual void OnConnectionReset(EndPoint fromAddress) { }
    protected virtual void OnDisconnect() { }
    protected virtual void OnUpdate() { }
    protected virtual void ReportError(string error) { }

    protected void SendPacket(byte[] outputPacket, EndPoint toAddress)
    {
        try
        {
            socket.SendTo(outputPacket, outputPacket.Length, SocketFlags.None, toAddress);
        }
        catch (Exception e)
        {
            Debug.LogError("[NETWORKING] SendPacket ERROR: " + e.Message);
        }
    }
}

