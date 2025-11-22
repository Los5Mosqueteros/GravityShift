using UnityEngine;

public class RemotePlayerController : MonoBehaviour
{
    [Header("Interpolation Settings")]
    public float interpolationSpeed = 10f;
    public float maxExtrapolationTime = 0.25f;

    private Vector3 lastPosition;
    private Vector3 targetPosition;
    private Vector3 velocity;

    private Quaternion lastRotation;
    private Quaternion targetRotation;

    private float lastUpdateTime;

    private void Start()
    {
        lastPosition = transform.position;
        targetPosition = transform.position;

        lastRotation = transform.rotation;
        targetRotation = transform.rotation;

        lastUpdateTime = Time.time;
    }

    private void Update()
    {
        float deltaTime = Time.time - lastUpdateTime;

        transform.position = Vector3.Lerp(transform.position, targetPosition, interpolationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, interpolationSpeed * Time.deltaTime);

        if(deltaTime > 0.2f)
        {
            float extrapolationFactor = Mathf.Min(deltaTime, maxExtrapolationTime);
            transform.position += velocity * extrapolationFactor;
        }
    }

    public void SetTarget(Vector3 newPosition, Vector3 newRotationEuler)
    {
        lastPosition = targetPosition;
        lastRotation = targetRotation;

        targetPosition = newPosition;
        targetRotation = Quaternion.Euler(newRotationEuler);

        float deltaTime = Time.time - lastUpdateTime;
        if(deltaTime > 0) velocity = (targetPosition - lastPosition) / deltaTime;

        lastUpdateTime = Time.time;
    }
}
