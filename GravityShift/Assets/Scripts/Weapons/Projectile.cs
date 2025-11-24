using UnityEngine;

public class Projectile : MonoBehaviour
{

    public float speed = 20f;
    public float lifetime = 3f;
    private float spawnTime;
    
    private Vector3 targetPoint;
    private Vector3 finalDirection;
    private bool hasConverged = false;

    void Start()
    {
        spawnTime = Time.time;
    }

    void Update()
    {
        if (!hasConverged && targetPoint != Vector3.zero)
        {
            if (Vector3.Distance(transform.position, targetPoint) < 0.5f)
            {
                hasConverged = true;
                transform.rotation = Quaternion.FromToRotation(Vector3.left, finalDirection);
            }
        }
        
        transform.position += -transform.right * speed * Time.deltaTime;

        if (Time.time - spawnTime > lifetime)
        {
            Destroy(gameObject);
        }
    }
    
    public void SetConvergenceData(Vector3 target, Vector3 direction)
    {
        targetPoint = target;
        finalDirection = direction;
    }
}
