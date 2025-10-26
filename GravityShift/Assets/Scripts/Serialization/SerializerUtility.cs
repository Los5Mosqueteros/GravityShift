using UnityEngine;

public static class SerializerUtility
{
    public static string ToJson<T>(T obj)
    {
        return JsonUtility.ToJson(obj, true);
    }

    public static T FromJson<T>(string json)
    {
        return JsonUtility.FromJson<T>(json);
    }
}