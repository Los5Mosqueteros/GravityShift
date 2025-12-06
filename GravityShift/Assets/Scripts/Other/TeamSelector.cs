using UnityEngine;

public class TeamSelector : MonoBehaviour
{
    public Client client;

    private void Update()
    {
        if (client == null) return;

        if (Input.GetKeyDown(KeyCode.F1))
        {
            client.RequestTeamChange(1);
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            client.RequestTeamChange(2);
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            client.RequestTeamChange(3);
        }
        if (Input.GetKeyDown(KeyCode.F4))
        {
            client.RequestTeamChange(4);
        }
    }
}
