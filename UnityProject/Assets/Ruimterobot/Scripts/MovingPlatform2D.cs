using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform2D : MonoBehaviour
{
    [SerializeField] private Vector2 pointA;
    [SerializeField] private Vector2 pointB;
    [SerializeField] private float speed = 0.65f;
    private Rigidbody2D body;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
    }

    private void Start()
    {
        body.position = pointA;
        transform.position = pointA;
    }

    private void FixedUpdate()
    {
        if (Time.timeScale == 0f)
            return;
        float progress = Mathf.PingPong(Time.time * speed, 1f);
        body.MovePosition(Vector2.Lerp(pointA, pointB, progress));
    }
}
