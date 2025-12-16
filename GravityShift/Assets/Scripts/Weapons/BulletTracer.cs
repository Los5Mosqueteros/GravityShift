using UnityEngine;

public class BulletTracer : MonoBehaviour
{
    private Vector3 targetPosition;
    private float speed;
    private bool initialized = false;
    public void Init(Vector3 target, float speedVal)
    {
        targetPosition = target;
        speed = speedVal;
        initialized = true;

        transform.LookAt(targetPosition);

        Destroy(gameObject, 3f);
    }

    void Update()
    {
        if (!initialized) return;

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            Destroy(gameObject);
        }
    }
}