using UnityEngine;

/// <summary>Keeps a soft contact shadow on the floor while the hero jumps.</summary>
public class GroundShadow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private SpriteRenderer shadow;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float rayLength = 24f;
    [SerializeField] private float maximumAlpha = 0.34f;

    public void Configure(Transform followTarget, SpriteRenderer renderer, LayerMask floorMask)
    {
        target = followTarget;
        shadow = renderer;
        groundLayer = floorMask;
    }

    private void LateUpdate()
    {
        if (target == null || shadow == null) return;
        RaycastHit2D hit = Physics2D.Raycast(target.position, Vector2.down, rayLength, groundLayer);
        if (!hit.collider)
        {
            shadow.enabled = false;
            return;
        }

        shadow.enabled = true;
        float height = Mathf.Max(0f, target.position.y - hit.point.y);
        float scale = Mathf.Clamp(1f - height * 0.075f, 0.34f, 1f);
        transform.position = new Vector3(target.position.x, hit.point.y + 0.025f, 0.5f);
        transform.localScale = new Vector3(0.74f * scale, 0.14f * scale, 1f);
        Color color = shadow.color;
        color.a = Mathf.Clamp(maximumAlpha - height * 0.055f, 0.07f, maximumAlpha);
        shadow.color = color;
    }
}
