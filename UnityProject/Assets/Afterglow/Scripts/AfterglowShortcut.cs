using UnityEngine;

namespace Afterglow
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class AfterglowShortcut : MonoBehaviour
    {
        private int memoryIndex;
        private Vector2 destination;
        private AfterglowUI ui;
        private SpriteRenderer glyph;

        public void Configure(int requiredMemory, Vector2 exitPoint, AfterglowUI gameUi)
        {
            memoryIndex = requiredMemory;
            destination = exitPoint;
            ui = gameUi;
            GetComponent<Collider2D>().isTrigger = true;
            glyph = GetComponent<SpriteRenderer>();
            RefreshLockState();
        }

        private void Update() => RefreshLockState();

        private void RefreshLockState()
        {
            bool open = AfterglowSession.HasMemory(memoryIndex);
            if (glyph != null) glyph.color = open ? Color.white : new Color(.36f,.37f,.35f,.42f);
        }

        public bool TryUse(AfterglowPlayer player)
        {
            if (player == null || !AfterglowSession.HasMemory(memoryIndex))
            {
                ui?.Notify("DE WORTELDOORGANG IS VERGRENDELD  •  ZOEK ELINS HERINNERING", new Color(.69f,.65f,.56f), 1.5f);
                return false;
            }
            Vector2 arrival = destination + Vector2.up * .08f;
            player.WarpToShortcut(arrival);
            ui?.PlayPulseWave(arrival, 2.8f, new Color(.71f,.75f,.52f));
            ui?.Notify("VERBORGEN WORTELPAD GEVONDEN  •  DE OUDE WEG IS OVERGESLAGEN", new Color(.8f,.78f,.58f), 2.2f);
            AfterglowAudio.Instance?.Play("special", .8f);
            return true;
        }
    }
}
