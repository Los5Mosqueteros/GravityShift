using UnityEngine;
using TMPro;

public class PlayerNameTag : MonoBehaviour
{
    public TextMeshPro nameText;
    public Vector3 offset = new Vector3(0, 2.5f, 0);

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
            transform.position = transform.parent.position + offset;
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }
    }
}
