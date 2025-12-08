using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("UI Input")]
    public TMP_InputField ipInput;
    public TMP_InputField nameInput;
    public Button connectButton; 

    [Header("Feedback")]
    public TextMeshProUGUI errorText;

    [Header("Scene Objects")]
    public GameObject menuCanvas;
    public GameObject clientPrefab;
    public GameObject lobbyCamera;

    public static string SelectedIP = "127.0.0.1";
    public static string PlayerName = "Player";

    private Coroutine errorCoroutine;

    private void Start()
    {
        if (errorText != null) errorText.alpha = 0;
    }

    public void Connect()
    {
        SelectedIP = ipInput.text;
        PlayerName = nameInput.text;

        if (string.IsNullOrWhiteSpace(SelectedIP)) SelectedIP = "127.0.0.1";
        if (string.IsNullOrWhiteSpace(PlayerName)) PlayerName = "Player";

        connectButton.interactable = false;

        clientPrefab.SetActive(true);

        Debug.Log($"[CLIENT] Attempting connection to {SelectedIP}...");

        StartCoroutine(ConnectionTimeoutRoutine());
    }

    public void OnConnectionSuccess()
    {
        Debug.Log("Connection Successful! Hiding Menu.");

        menuCanvas.SetActive(false);
        Destroy(lobbyCamera);
    }

    public void OnConnectionFailed(string reason)
    {
        Debug.Log("Connection Failed.");

        connectButton.interactable = true;

        clientPrefab.SetActive(false);

        ShowError(reason);
    }

    public void ShowError(string message)
    {
        if (errorCoroutine != null) StopCoroutine(errorCoroutine);
        errorCoroutine = StartCoroutine(FadeErrorSequence(message));
    }

    IEnumerator FadeErrorSequence(string message)
    {
        if (errorText == null) yield break;

        errorText.text = message;
        errorText.alpha = 0;

        float timer = 0;
        while (timer < 0.5f) 
        {
            timer += Time.deltaTime;
            errorText.alpha = Mathf.Lerp(0, 1, timer / 0.5f);
            yield return null;
        }
        errorText.alpha = 1;

        yield return new WaitForSeconds(3f); 

        timer = 0;
        while (timer < 0.5f) 
        {
            timer += Time.deltaTime;
            errorText.alpha = Mathf.Lerp(1, 0, timer / 0.5f);
            yield return null;
        }
        errorText.alpha = 0;
    }

    IEnumerator ConnectionTimeoutRoutine()
    {
        yield return new WaitForSeconds(5f);

        if (menuCanvas.activeSelf && connectButton.interactable == false)
        {
            OnConnectionFailed("Connection Timed Out");
        }
    }
}