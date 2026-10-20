using UnityEngine;

namespace EchoProtocol
{
    public sealed class EchoProtocolCamera2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.22f;
        [SerializeField] private Vector2 offset = new Vector2(5.8f, 0.4f);
        [SerializeField] private float minimumX;
        [SerializeField] private float maximumX = 38f;
        private Vector3 velocity;
        private float shakeUntil;
        private float shakeStrength;

        public void Configure(Transform follow, float minX, float maxX)
        {
            target = follow;
            minimumX = minX;
            maximumX = maxX;
        }

        public void Shake(float duration, float strength)
        {
            shakeUntil = Mathf.Max(shakeUntil, Time.time + duration);
            shakeStrength = Mathf.Max(shakeStrength, strength);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 targetPosition = new Vector3(Mathf.Clamp(target.position.x + offset.x, minimumX, maximumX), target.position.y + offset.y, -10f);
            Vector3 smooth = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
            if (Time.time < shakeUntil)
            {
                float amount = shakeStrength * Mathf.Clamp01((shakeUntil - Time.time) / 0.3f);
                smooth += (Vector3)Random.insideUnitCircle * amount;
            }
            transform.position = smooth;
            if (Time.time >= shakeUntil) shakeStrength = 0f;
        }
    }
}
