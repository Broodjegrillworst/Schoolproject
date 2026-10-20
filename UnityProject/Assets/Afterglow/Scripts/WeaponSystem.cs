using UnityEngine;

namespace Afterglow
{
    /// <summary>Oathroot's firing cadence and three narrative relic upgrades.</summary>
    public sealed class WeaponSystem : MonoBehaviour
    {
        private AfterglowPlayer owner;
        private AfterglowUI ui;
        private float nextShotTime;

        public int UpgradeLevel { get; private set; }

        public void Configure(AfterglowPlayer player, AfterglowUI gameUi)
        {
            owner = player;
            ui = gameUi;
            UpgradeLevel = Mathf.Clamp(AfterglowSession.RecoveredMemoryCount, 0, 3);
        }

        public bool SetUpgradeLevel(int level)
        {
            int next = Mathf.Clamp(level, UpgradeLevel, 3);
            if (next == UpgradeLevel) return false;
            UpgradeLevel = next;
            return true;
        }

        public bool Fire(Vector2 origin, Vector2 direction, bool charged)
        {
            if (owner == null || Time.time < nextShotTime) return false;
            float shotsPerSecond = charged ? 2.45f + UpgradeLevel * .18f : 5.2f + UpgradeLevel * .55f;
            nextShotTime = Time.time + 1f / shotsPerSecond;

            int damage = charged ? 3 + (UpgradeLevel >= 1 ? 1 : 0) + (UpgradeLevel >= 3 ? 1 : 0) : 1 + (UpgradeLevel >= 1 ? 1 : 0);
            int pierces = UpgradeLevel >= 2 ? 1 : 0;
            AfterglowPulseProjectile.FirePlayer(origin, direction, owner, charged, damage, pierces);

            AfterglowAudio.Instance?.Play(charged ? "charge" : "shot", charged ? .88f : 1f);
            // Weapon discharge is loud enough to make nearby sentries investigate it.
            EnemyAI.AlertNearby(origin, charged ? 20f : 12f, owner);
            ui?.PlayWeaponMuzzle(origin, direction, charged);
            return true;
        }
    }
}
