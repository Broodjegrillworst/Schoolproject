using UnityEngine;
using UnityEngine.InputSystem;

namespace Afterglow
{
    /// <summary>Owns combat input and charge timing; movement remains in AfterglowPlayer.</summary>
    public sealed class PlayerCombat : MonoBehaviour
    {
        private const float ChargedThreshold = .48f;
        private const float FullChargeSeconds = 1.15f;
        private AfterglowPlayer player;
        private WeaponSystem weapon;
        private AfterglowUI ui;
        private float heldSeconds;

        public bool IsCharging { get; private set; }
        public float ChargePercent => IsCharging ? Mathf.Clamp01(heldSeconds / FullChargeSeconds) : 0f;

        public void Configure(AfterglowPlayer owner, WeaponSystem weaponSystem, AfterglowUI gameUi)
        {
            player = owner;
            weapon = weaponSystem;
            ui = gameUi;
        }

        private void Update()
        {
            if (player == null || player.IsDead || AfterglowUI.IsPaused) return;
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            bool pressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
            bool held = mouse != null && mouse.leftButton.isPressed;
            bool released = mouse != null && mouse.leftButton.wasReleasedThisFrame;

            if (pressed)
            {
                IsCharging = true;
                heldSeconds = 0f;
                player.StartCombat();
            }
            if (IsCharging && held)
            {
                heldSeconds = Mathf.Min(FullChargeSeconds, heldSeconds + Time.deltaTime);
                ui?.UpdateWeaponCharge(ChargePercent);
            }
            if (IsCharging && released)
            {
                bool charged = heldSeconds >= ChargedThreshold;
                player.FirePrimary(charged);
                IsCharging = false;
                heldSeconds = 0f;
                ui?.UpdateWeaponCharge(0f);
            }

            if (mouse != null && mouse.rightButton.wasPressedThisFrame)
            {
                player.StartCombat();
                player.FireSecondary();
            }
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                player.FireUltimate();
        }
    }
}
