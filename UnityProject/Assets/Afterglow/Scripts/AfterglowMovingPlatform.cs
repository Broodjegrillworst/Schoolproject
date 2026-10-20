using UnityEngine;

namespace Afterglow
{
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public sealed class AfterglowMovingPlatform : MonoBehaviour
    {
        private Vector2 home;
        private Vector2 offset = new Vector2(0f, 3f);
        private float duration = 2.6f;
        private Rigidbody2D body;

        public void Configure(Vector2 travel, float seconds)
        {
            home = transform.position;
            offset = travel;
            duration = Mathf.Max(.5f, seconds);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            if (home == Vector2.zero) home = transform.position;
        }

        private void FixedUpdate()
        {
            float t = Mathf.PingPong(Time.time / duration, 1f);
            float eased = t * t * (3f - 2f * t);
            body.MovePosition(home + offset * eased);
        }
    }
}
