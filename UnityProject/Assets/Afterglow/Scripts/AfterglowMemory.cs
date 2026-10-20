using UnityEngine;

namespace Afterglow
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class AfterglowMemory : MonoBehaviour
    {
        private int memoryIndex;
        private string title;
        private AfterglowUI ui;
        private Collider2D trigger;

        public void Configure(int index, string memoryTitle, AfterglowUI gameUi)
        {
            memoryIndex = index;
            title = memoryTitle;
            ui = gameUi;
            trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;
            if (AfterglowSession.HasMemory(memoryIndex)) gameObject.SetActive(false);
        }

        public void TryInteract(AfterglowPlayer player)
        {
            if (player == null || !isActiveAndEnabled) return;
            if (AfterglowSession.CollectMemory(memoryIndex))
            {
                ui?.ShowMemory(memoryIndex, title, AfterglowSession.RecoveredMemoryCount);
                ui?.PlayMemoryEffect(transform.position);
                player.UpgradeWeaponFromMemory(AfterglowSession.RecoveredMemoryCount);
            }
            Destroy(gameObject);
        }
    }
}
