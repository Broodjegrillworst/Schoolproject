using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Checkpoint2D : MonoBehaviour
{
    private bool activated;
    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player != null)
        {
            if (activated) return;
            activated = true;
            player.SetCheckpoint(transform.position + Vector3.up * 0.72f);
            SpriteRenderer marker = GetComponent<SpriteRenderer>();
            if (marker != null) marker.color = new Color(1f, 0.78f, 0.32f);
            FeedbackVFX2D.Burst(transform.position, new Color(0.33f, 0.96f, 0.94f), 18);
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayCheckpoint();
        }
    }
}

