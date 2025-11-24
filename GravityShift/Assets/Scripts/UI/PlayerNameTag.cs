using UnityEngine;
using TMPro;

public class PlayerNameTag : MonoBehaviour
{
    public TextMeshPro nameText;

    public void SetName(string playerName)
    {
        if(nameText != null)
        {
            nameText.text = playerName;
        }
    }

    private void LateUpdate()
    {
        if(Camera.main != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }
    }
}
