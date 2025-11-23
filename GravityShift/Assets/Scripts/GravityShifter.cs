using UnityEngine;
using System.Collections;

public class GravityShifter : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public Transform playerRoot;

    [Header("Settings")]
    public float minTime = 5f;
    public float maxTime = 10f;
    public float gravityStrength = 25f;
    public float rotationDuration = 0.6f;
    public float timeBeforeRotation = 0.3f;

    float timer;
    bool isFlipped = false;
    bool rotating = false;

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (playerRoot == null) playerRoot = transform;

        rb.useGravity = false;
        SetNewTimer();
    }

    void Update()
    {
        if (rotating) return; // evita flip mientras rota

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            StartCoroutine(FlipRoutine());
        }
    }

    void FixedUpdate()
    {
        Vector3 gravDir = isFlipped ? Vector3.up : Vector3.down;
        rb.AddForce(gravDir * gravityStrength, ForceMode.Acceleration);
    }

    IEnumerator FlipRoutine()
    {
        rotating = true;

        isFlipped = !isFlipped;

        yield return new WaitForSeconds(timeBeforeRotation);

        rb.transform.SetParent(null);

        playerRoot.position = rb.transform.position;

        rb.transform.SetParent(playerRoot);

        Quaternion startRot = playerRoot.rotation;
        Quaternion targetRot = startRot * Quaternion.Euler(180f, 0f, 0f);

        float t = 0;

        while (t < 1f)
        {
            t += Time.deltaTime / rotationDuration;
            playerRoot.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        SetNewTimer();
        rotating = false;
    }

    void SetNewTimer()
    {
        timer = Random.Range(minTime, maxTime);
    }
}
