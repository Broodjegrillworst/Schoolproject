using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class VesperCamera2D : MonoBehaviour
{
    private static VesperCamera2D instance;
    private Transform follow;
    private Vector3 velocity;
    private float baseSize = 6.4f;
    private float introUntil;
    private Transform bossFocus;
    private float shakeEnd;
    private float shakeAmount;
    private float regionWidth = 66f;

    public static void Shake(float amount, float seconds) { if (instance != null) { instance.shakeAmount = Mathf.Max(instance.shakeAmount, amount); instance.shakeEnd = Mathf.Max(instance.shakeEnd, Time.unscaledTime + seconds); } }
    public static void BossIntro(Transform target) { if (instance != null) { instance.bossFocus = target; instance.introUntil = Time.unscaledTime + 2.1f; } }
    public void SetRegionWidth(float width) { regionWidth = width; }

    private void Awake() { instance = this; GetComponent<Camera>().orthographic = true; GetComponent<Camera>().orthographicSize = baseSize; GetComponent<Camera>().backgroundColor = new Color(0.015f, 0.025f, 0.045f); }
    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) follow = player.transform;
        if (follow != null)
        {
            float x = follow.position.x < 30f ? 8f : regionWidth - 8f;
            transform.position = new Vector3(x, Mathf.Max(2.2f, follow.position.y + 2f), -10f);
        }
    }
    private void LateUpdate()
    {
        if (follow == null) return;
        bool intro = bossFocus != null && Time.unscaledTime < introUntil;
        Vector3 target = intro ? (follow.position + bossFocus.position) * 0.5f + Vector3.up * 1.25f : follow.position + Vector3.up * 1.8f;
        target.x = Mathf.Clamp(target.x, 7.4f, regionWidth - 7.4f);
        target.z = -10f;
        float smooth = intro ? 0.45f : 0.19f;
        transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, smooth, 100f, Time.unscaledDeltaTime);
        Camera cam = GetComponent<Camera>();
        float size = intro ? 5.1f : baseSize;
        if (Time.unscaledTime > introUntil) bossFocus = null;
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, size, Time.unscaledDeltaTime * 3.4f);
        if (Time.unscaledTime < shakeEnd)
            transform.position += (Vector3)Random.insideUnitCircle * shakeAmount;
        else shakeAmount = Mathf.MoveTowards(shakeAmount, 0f, Time.unscaledDeltaTime * 2f);
    }
}
