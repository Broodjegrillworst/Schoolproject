using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RapidFirePickup2D : MonoBehaviour
{
    [SerializeField] private float durationSeconds = 8f;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController2D player = other.GetComponentInParent<PlayerController2D>();
        if (player == null)
            return;
        player.ActivateRapidFire(durationSeconds);
        FeedbackVFX2D.Burst(transform.position, new Color(1f, 0.42f, 0.82f), 12);
        if (ArcadeAudioManager.Instance != null)
            ArcadeAudioManager.Instance.PlayPickup();
        Destroy(gameObject);
    }
}
