using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField nameField;
    public TMP_InputField ipField;

    public void OnPlayButton()
    {
        if (string.IsNullOrEmpty(nameField.text))
        {
            Debug.LogWarning("Debes poner un nombre.");
            return;
        }

        if (string.IsNullOrEmpty(ipField.text))
        {
            Debug.LogWarning("Debes poner una IP.");
            return;
        }

        PlayerPrefs.SetString("playerName", nameField.text);
        PlayerPrefs.SetString("serverIP", ipField.text);
        PlayerPrefs.Save();

        Debug.Log("Datos guardados. Cargando partida...");

        SceneManager.LoadScene("Gameplay");
    }
}
