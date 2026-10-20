using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HeroUpgradePickup2D : MonoBehaviour
{
    public enum UpgradeKind
    {
        AegisShield,
        Skybound
    }

    [SerializeField] private UpgradeKind upgradeKind;

    public void Configure(UpgradeKind kind)
    {
        upgradeKind = kind;
    }

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController2D player = other.GetComponentInParent<PlayerController2D>();
        if (player == null)
            return;

        if (upgradeKind == UpgradeKind.AegisShield)
            GameSession.GrantAegisShield();
        else
            player.UnlockSkybound();

        FeedbackVFX2D.Burst(transform.position, new Color(0.3f, 0.94f, 1f), 18);

        if (ArcadeAudioManager.Instance != null)
            ArcadeAudioManager.Instance.PlayPickup();
        Destroy(gameObject);
    }
}
