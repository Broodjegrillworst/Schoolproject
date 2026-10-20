using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Owns PULSE's three readable power types and the shared energy meter.</summary>
public class HeroPowerController2D : MonoBehaviour
{
    private enum PowerType { Pulse, Arc, Nova }

    [SerializeField] private Text powerLabel;
    [SerializeField] private Image energyFill;
    [SerializeField] private float maximumEnergy = 100f;
    [SerializeField] private float regenerationPerSecond = 10f;

    private float energy = 100f;
    private PowerType selectedPower;
    private int currentRank;

    public float Energy => energy;

    private void Awake()
    {
        currentRank = ProfileSaveSystem.PowerRank;
        energy = maximumEnergy;
    }

    private void Update()
    {
        RunStatistics.Tick(Time.deltaTime);
        if (Time.timeScale > 0f)
        {
            energy = Mathf.Min(maximumEnergy, energy + regenerationPerSecond * Time.deltaTime);
            ReadPowerInput();
        }
        RefreshHud();
    }

    public void ConfigureHud(Text label, Image fill)
    {
        powerLabel = label;
        energyFill = fill;
        RefreshHud();
    }

    public bool Fire(GameObject prefab, Transform firePoint, int facingDirection)
    {
        if (prefab == null || firePoint == null) return false;
        int rank = ProfileSaveSystem.PowerRank;
        currentRank = rank;
        int mode = (int)selectedPower;
        if ((mode == 1 && rank < 1) || (mode == 2 && rank < 2))
        {
            selectedPower = PowerType.Pulse;
            RefreshHud();
            return false;
        }

        float energyCost = mode == 1 ? 18f : mode == 2 ? 38f : 0f;
        if (energy < energyCost) return false;
        energy -= energyCost;

        if (mode == 1)
        {
            FireShot(prefab, firePoint.position, facingDirection, -13f, 10.5f, 1, new Color(0.35f, 0.72f, 1f), 1f);
            FireShot(prefab, firePoint.position, facingDirection, 0f, 11.5f, 1 + (rank >= 3 ? 1 : 0), new Color(0.35f, 0.9f, 1f), 1.1f);
            FireShot(prefab, firePoint.position, facingDirection, 13f, 10.5f, 1, new Color(0.35f, 0.72f, 1f), 1f);
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayArc();
        }
        else if (mode == 2)
        {
            FireShot(prefab, firePoint.position, facingDirection, 0f, 8f, 3 + (rank >= 3 ? 2 : 0),
                new Color(1f, 0.55f, 0.19f), 1.75f);
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayNova();
            FeedbackVFX2D.Shake(0.11f, 0.1f);
        }
        else
        {
            float speed = 12f + (rank >= 3 ? 1.5f : 0f);
            int damage = 1 + (rank >= 3 ? 1 : 0);
            FireShot(prefab, firePoint.position, facingDirection, 0f, speed, damage,
                new Color(0.28f, 0.92f, 1f), 1f);
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayShot();
        }
        RefreshHud();
        return true;
    }

    public float CooldownMultiplier => selectedPower == PowerType.Nova ? 2.65f :
        selectedPower == PowerType.Arc ? 1.35f : 1f;

    public void AddEnergy(float amount)
    {
        if (amount <= 0f) return;
        energy = Mathf.Min(maximumEnergy, energy + amount);
        RefreshHud();
    }

    private void FireShot(GameObject prefab, Vector3 origin, int direction, float angle,
        float speed, int damage, Color tint, float size)
    {
        float radians = angle * Mathf.Deg2Rad;
        Vector2 vector = new Vector2(Mathf.Cos(radians) * Mathf.Sign(direction), Mathf.Sin(radians) * Mathf.Sign(direction));
        GameObject shotObject = ProjectilePool2D.Spawn(prefab, origin, Quaternion.identity);
        if (shotObject == null) return;
        Projectile2D shot = shotObject.GetComponent<Projectile2D>();
        if (shot == null) { Destroy(shotObject); return; }

        Collider2D heroCollider = GetComponent<Collider2D>();
        Collider2D shotCollider = shotObject.GetComponent<Collider2D>();
        if (heroCollider != null && shotCollider != null)
            Physics2D.IgnoreCollision(heroCollider, shotCollider);

        shot.Launch(vector, speed, damage, tint, size);
    }

    private void ReadPowerInput()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        if (keyboard != null && keyboard.digit1Key.wasPressedThisFrame) SelectPower(PowerType.Pulse);
        if (keyboard != null && keyboard.digit2Key.wasPressedThisFrame && ProfileSaveSystem.PowerRank >= 1) SelectPower(PowerType.Arc);
        if (keyboard != null && keyboard.digit3Key.wasPressedThisFrame && ProfileSaveSystem.PowerRank >= 2) SelectPower(PowerType.Nova);
        if ((keyboard != null && keyboard.qKey.wasPressedThisFrame) || (gamepad != null && gamepad.rightShoulder.wasPressedThisFrame))
        {
            int next = ((int)selectedPower + 1) % (ProfileSaveSystem.PowerRank >= 2 ? 3 :
                ProfileSaveSystem.PowerRank >= 1 ? 2 : 1);
            SelectPower((PowerType)next);
        }
    }

    private void SelectPower(PowerType power)
    {
        if ((power == PowerType.Arc && ProfileSaveSystem.PowerRank < 1) ||
            (power == PowerType.Nova && ProfileSaveSystem.PowerRank < 2)) return;
        selectedPower = power;
        if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayUi();
        RefreshHud();
    }

    private void RefreshHud()
    {
        if (energyFill != null) energyFill.fillAmount = maximumEnergy <= 0f ? 0f : energy / maximumEnergy;
        if (powerLabel == null) return;
        string ability = selectedPower == PowerType.Arc ? "ARC-GOLF" : selectedPower == PowerType.Nova ? "NOVA-KERN" : "PULSE-BURST";
        string unlocks = ProfileSaveSystem.PowerRank < 1 ? "  // UPGRADE VEREIST" :
            ProfileSaveSystem.PowerRank < 2 ? "  // 3 VOOR NOVA" : "";
        powerLabel.text = "KRACHT  " + ability + "     ENERGIE " + Mathf.RoundToInt(energy).ToString("00") + "%" + unlocks;
    }
}
