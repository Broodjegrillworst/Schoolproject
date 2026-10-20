using UnityEngine;

namespace Afterglow
{
    public sealed class AfterglowCameraRig : MonoBehaviour
    {
        private Transform target;
        private float maxX = 100f;
        private Vector3 dampVelocity;
        private float shakeTime;
        private float shakeStrength;
        private Camera sceneCamera;
        private float bossCinematicTimer;
        private float zoomVelocity;

        public void Follow(Transform followTarget, float worldEnd)
        {
            target = followTarget;
            maxX = worldEnd;
            sceneCamera = GetComponent<Camera>();
            if (sceneCamera != null) sceneCamera.orthographicSize = 5.15f;
        }

        public void Shake(float strength, float duration)
        {
            if (AfterglowSession.Profile != null && !AfterglowSession.Profile.screenShake) return;
            shakeStrength = Mathf.Max(shakeStrength, strength);
            shakeTime = Mathf.Max(shakeTime, duration);
        }

        public void BeginBossCinematic(float duration = 2.4f) => bossCinematicTimer = Mathf.Max(bossCinematicTimer, duration);

        private void LateUpdate()
        {
            if (target == null) return;
            if (sceneCamera == null) sceneCamera = GetComponent<Camera>();
            bossCinematicTimer -= Time.unscaledDeltaTime;
            float cinematic = bossCinematicTimer > 0f ? 1f : 0f;
            if (sceneCamera != null)
                sceneCamera.orthographicSize = Mathf.SmoothDamp(sceneCamera.orthographicSize, Mathf.Lerp(5.15f, 6.15f, cinematic), ref zoomVelocity, .32f, 999f, Time.unscaledDeltaTime);
            float width = sceneCamera != null ? sceneCamera.orthographicSize * 2f * sceneCamera.aspect : 18f;
            float min = width * .48f;
            Rigidbody2D targetBody = target.GetComponent<Rigidbody2D>();
            float lookAhead = targetBody != null ? Mathf.Clamp(targetBody.linearVelocity.x * .25f, -1.2f, 1.8f) : 0f;
            float desiredX = Mathf.Clamp(target.position.x + Mathf.Lerp(2.6f + lookAhead, 1.3f, cinematic), min, Mathf.Max(min, maxX - min));
            Vector3 goal = new Vector3(desiredX, Mathf.Clamp(target.position.y + Mathf.Lerp(.45f, .85f, cinematic), 2.7f, 6.2f), -10f);
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref dampVelocity, .23f, 999f, Time.unscaledDeltaTime);
            if (shakeTime > 0f)
            {
                shakeTime -= Time.unscaledDeltaTime;
                float fade = Mathf.Clamp01(shakeTime / .3f);
                transform.position += (Vector3)Random.insideUnitCircle * shakeStrength * fade;
                if (shakeTime <= 0f) shakeStrength = 0f;
            }
        }
    }

}
