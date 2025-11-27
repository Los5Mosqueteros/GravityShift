using UnityEngine;

public class RemotePlayerController : MonoBehaviour
{
    [Header("Smoothing")]
    public float positionLerpSpeed = 20f;
    public float rotationLerpSpeed = 20f;

    [Header("Extrapolation")]
    public float maxExtrapolation = 0.1f;
    public float minTeleportDistance = 5f;

    private Vector3 targetPosition;
    private Vector3 targetRotationEuler;
    private Vector3 lastTargetPosition;
    private float lastTargetTime;

    private Vector3 estimatedVelocity = Vector3.zero;

    private void Awake()
    {
        targetPosition = transform.position;
        targetRotationEuler = transform.rotation.eulerAngles;
        lastTargetPosition = targetPosition;
        lastTargetTime = Time.time;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        float dist = Vector3.Distance(transform.position, targetPosition);
        if(dist > minTeleportDistance)
        {
            transform.position = targetPosition;
        }
        else
        {
            float timeSinceLast = Time.time - lastTargetTime;
            if(timeSinceLast > 0f && timeSinceLast < maxExtrapolation)
            {
                Vector3 extrapolatedTarget = targetPosition + estimatedVelocity * timeSinceLast;
                transform.position = Vector3.Lerp(transform.position, extrapolatedTarget, 1f - Mathf.Exp(-positionLerpSpeed * dt));
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - Mathf.Exp(-positionLerpSpeed * dt));
            }
        }

        Quaternion targetQ = Quaternion.Euler(targetRotationEuler);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetQ, 1f - Mathf.Exp(-rotationLerpSpeed * dt));
    }

    public void SetTarget(Vector3 pos, Vector3 rotEuler)
    {
        float now = Time.time;

        float deltaT = Mathf.Max(0.0001f, now - lastTargetTime);
        estimatedVelocity = (pos - lastTargetPosition) / deltaT;

        lastTargetPosition = pos;
        lastTargetTime = now;

        targetPosition = pos;
        targetRotationEuler = rotEuler;
    }
}
