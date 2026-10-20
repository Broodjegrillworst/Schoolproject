using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LifePickup2D : MonoBehaviour
{
    [SerializeField] private int livesToRestore = 1;
    private bool collected;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected)
            return;

        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player == null)
            return;

        collected = true;
        player.AddLife(livesToRestore);
        if (ArcadeAudioManager.Instance != null)
            ArcadeAudioManager.Instance.PlayPickup();
        Destroy(gameObject);
    }
}
