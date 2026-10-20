using UnityEngine;

/// <summary>Rotates a compound hitbox so the player must read and time a crossing.</summary>
public class RotatingHazard2D : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 82f;
    [SerializeField] private bool reverse;

    public void Configure(float speed, bool reverseDirection)
    {
        degreesPerSecond = Mathf.Abs(speed);
        reverse = reverseDirection;
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        transform.Rotate(0f, 0f, degreesPerSecond * (reverse ? -1f : 1f) * Time.deltaTime);
    }
}
