using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CrystalPickup2D : MonoBehaviour
{
    private bool collected;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || other.GetComponentInParent<PlayerController2D>() == null)
            return;

        collected = true;
        GameSession.CollectCrystal();
        HeroPowerController2D powers = other.GetComponentInParent<HeroPowerController2D>();
        if (powers != null) powers.AddEnergy(16f);
        FeedbackVFX2D.Burst(transform.position, new Color(1f, 0.82f, 0.32f), 10);
        if (ArcadeAudioManager.Instance != null)
            ArcadeAudioManager.Instance.PlayPickup();
        Destroy(gameObject);
    }
}
