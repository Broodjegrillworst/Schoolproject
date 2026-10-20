using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float followStartX = -2f;
    [SerializeField] private float minimumX;
    [SerializeField] private float maximumX = 27.5f;
    [SerializeField] private float smoothing = 7f;
    [SerializeField] private float baseY = 0.8f;
    [SerializeField] private float verticalFollow = 0.08f;

    private Vector3 shakeOffset;
    private float shakeUntil;
    private float shakeStrength;
    private Camera controlledCamera;
    private float defaultZoom = 5.3f;
    private Transform introBoss;
    private float introStartedAt = -10f;
    private float introDuration = 1.7f;

    public void Configure(Transform followTarget, float leftBound, float rightBound)
    {
        target = followTarget;
        minimumX = leftBound;
        maximumX = rightBound;
    }

    private void Awake()
    {
        controlledCamera = GetComponent<Camera>();
        if (controlledCamera != null) defaultZoom = controlledCamera.orthographicSize;
    }

    public void PlayBossIntro(Transform boss)
    {
        if (boss == null) return;
        introBoss = boss;
        introStartedAt = Time.unscaledTime;
    }

    public void Shake(float strength, float duration)
    {
        shakeStrength = Mathf.Max(shakeStrength, strength);
        shakeUntil = Mathf.Max(shakeUntil, Time.unscaledTime + duration);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            PlayerController2D player = FindAnyObjectByType<PlayerController2D>();
            if (player != null) target = player.transform;
        }
        if (target == null || Time.timeScale == 0f) return;

        Vector3 current = transform.position;
        float introBlend = 0f;
        if (introBoss != null)
        {
            float elapsed = Time.unscaledTime - introStartedAt;
            if (elapsed >= introDuration)
            {
                introBoss = null;
            }
            else
            {
                introBlend = elapsed < 0.65f
                    ? Mathf.SmoothStep(0f, 1f, elapsed / 0.65f)
                    : 1f - Mathf.SmoothStep(0f, 1f, (elapsed - 0.65f) / (introDuration - 0.65f));
            }
        }

        float desiredX = current.x;
        if (target.position.x > followStartX)
            desiredX = Mathf.Clamp(target.position.x, minimumX, maximumX);
        float desiredY = Mathf.Lerp(baseY, target.position.y + baseY, verticalFollow);
        if (introBoss != null && introBlend > 0f)
        {
            Vector3 encounterCenter = (target.position + introBoss.position) * 0.5f;
            desiredX = Mathf.Clamp(Mathf.Lerp(desiredX, encounterCenter.x, introBlend), minimumX, maximumX);
            desiredY = Mathf.Lerp(desiredY, encounterCenter.y + baseY, introBlend * 0.72f);
        }
        Vector3 desired = new Vector3(desiredX, desiredY, current.z);
        Vector3 followed = Vector3.Lerp(current, desired, 1f - Mathf.Exp(-smoothing * Time.deltaTime));

        if (controlledCamera != null)
        {
            float targetZoom = defaultZoom - introBlend * 0.55f;
            controlledCamera.orthographicSize = Mathf.Lerp(controlledCamera.orthographicSize, targetZoom,
                1f - Mathf.Exp(-4.5f * Time.deltaTime));
        }

        if (Time.unscaledTime < shakeUntil)
        {
            shakeOffset = Random.insideUnitCircle * shakeStrength;
            followed += shakeOffset;
        }
        else
        {
            shakeStrength = 0f;
            shakeOffset = Vector3.zero;
        }
        transform.position = followed;
    }
}
