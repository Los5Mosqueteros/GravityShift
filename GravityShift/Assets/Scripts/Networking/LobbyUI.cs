using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    public TMP_InputField ipInput;
    public TMP_InputField nameInput;

    public static string SelectedIP = "127.0.0.1";
    public static string PlayerName = "Player";

    public GameObject menuCanvas;
    public GameObject clientPrefab;
    public GameObject lobbyCamera;

    public void Connect()
    {
        SelectedIP = ipInput.text;
        PlayerName = nameInput.text;

        if (string.IsNullOrWhiteSpace(SelectedIP)) SelectedIP = "127.0.0.1";

        if (string.IsNullOrWhiteSpace(PlayerName)) PlayerName = "Player";

        clientPrefab.SetActive(true);

        Destroy(lobbyCamera);
        menuCanvas.SetActive(false);

        Debug.Log($"[CLIENT] Conectando a {SelectedIP} como {PlayerName}");
    }
}
