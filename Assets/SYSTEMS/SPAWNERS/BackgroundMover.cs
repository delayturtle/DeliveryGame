using UnityEngine;

public class BackgroundMover : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 10f;
    public float maxLifetime = 20f;

    [Header("Optional Effects")]
    public bool randomizeScale = false;
    public float minScale = 0.8f;
    public float maxScale = 1.3f;

    public bool randomizeYaw = false;
    public float yawRange = 10f;

    // Travel direction locked at spawn so the object never drifts
    private Vector3 _travelDirection;

    void Start()
    {
        Destroy(gameObject, maxLifetime);

        // Lock the forward direction at the moment of spawning
        _travelDirection = transform.forward;

        if (randomizeScale)
        {
            float scale = Random.Range(minScale, maxScale);
            transform.localScale *= scale;
        }

        if (randomizeYaw)
        {
            transform.Rotate(0f, Random.Range(-yawRange, yawRange), 0f);
            _travelDirection = transform.forward;
        }
    }

    void Update()
    {
        // Move in the locked forward direction every frame
        transform.position += _travelDirection * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DestroyPlane"))
            Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("DestroyPlane"))
            Destroy(gameObject);
    }
}