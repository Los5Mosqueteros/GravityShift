using UnityEngine;

public class SerializationDemo : MonoBehaviour
{
    void Start()
    {
        PlayerData player = new PlayerData("p1", "Maria", new Vector3(2, 1, 0));

        string json = SerializerUtility.ToJson(player);
        Debug.Log("Serialized Json:\n" + json);

        string receivedJson = json;

        PlayerData restored = SerializerUtility.FromJson<PlayerData>(receivedJson);
        Debug.Log($"Deserialized: {restored.playerName} | Pos: {restored.position.x}, {restored.position.y}, {restored.position.z}");
    }
}