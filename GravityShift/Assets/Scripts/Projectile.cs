using UnityEngine;

public class Projectile : MonoBehaviour
{

    public float speed = 20f;
    public float lifetime = 3f;
    private float spawnTime;

    void Start()
    {
        spawnTime = Time.time;
    }


    void Update()
    {
        transform.position += -transform.right * speed * Time.deltaTime;

        if (Time.time - spawnTime > lifetime)
        {
            Destroy(gameObject);
        }
    }
}
