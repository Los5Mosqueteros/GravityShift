using UnityEngine;

public class Test : MonoBehaviour
{
    public ClientManagerAsyncUDP clientManager; 
    public string username = "JugadorTest"; 

    public void TestSendPlayerData()
    {
        if (clientManager == null)
        {
            Debug.LogError("No se ha asignado el ClientManagerAsyncUDP en el Inspector.");
            return;
        }
      
        PlayerData data = new PlayerData("1234", username, new Vector3(1, 2, 3));

        clientManager.SendPlayerData(data);

        Debug.Log($"PlayerData enviado para {username}");
    }
}