using UnityEngine;

public class ParallaxBackground2D : MonoBehaviour
{
    [SerializeField, Range(-0.4f, 1.5f)] private float followFactor = 0.18f;
    private Camera targetCamera;
    private Vector3 initialPosition;
    private Vector3 initialCameraPosition;

    public void Configure(float factor) => followFactor = Mathf.Clamp(factor, -0.4f, 1.5f);

    private void Awake()
    {
        targetCamera = Camera.main;
        initialPosition = transform.position;
        if (targetCamera != null) initialCameraPosition = targetCamera.transform.position;
    }

    private void LateUpdate()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;
        if (initialCameraPosition == Vector3.zero)
            initialCameraPosition = targetCamera.transform.position;
        Vector3 cameraDelta = targetCamera.transform.position - initialCameraPosition;
        Vector3 position = initialPosition + cameraDelta * followFactor;
        transform.position = position;
    }
}
