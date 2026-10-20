using UnityEngine;

namespace Afterglow
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class AfterglowExit : MonoBehaviour
    {
        private int stageIndex;
        private AfterglowUI ui;
        private GameObject barrier;
        private bool unlocked;
        private bool used;

        public void Configure(int stage, AfterglowUI gameUi, bool isUnlocked = false)
        {
            stageIndex = stage;
            ui = gameUi;
            unlocked = isUnlocked;
        }

        public void Unlock()
        {
            unlocked = true;
            if (barrier != null) barrier.SetActive(false);
            var renderer = GetComponentInChildren<SpriteRenderer>();
            if (renderer != null) renderer.color = new Color(.78f, .62f, .36f);
            ui?.Notify("DE DOORGANG IS OPEN", new Color(.78f, .62f, .36f));
        }

        public void SetBarrier(GameObject arenaBarrier) { barrier = arenaBarrier; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (used || other.GetComponentInParent<AfterglowPlayer>() == null) return;
            if (!unlocked)
            {
                ui?.Notify("VERSLA EERST DE WARDEN", new Color(.68f, .31f, .24f));
                return;
            }
            used = true;
            AfterglowSession.CompleteStage(stageIndex);
            ui?.ShowStageComplete(stageIndex);
        }
    }
}
