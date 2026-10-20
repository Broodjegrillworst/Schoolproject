using UnityEngine;

namespace EchoProtocol
{
    public sealed class EchoProtocolSecretCache2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        private bool collected;

        public void Configure(SpriteRenderer sprite) => visual = sprite;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || other.GetComponentInParent<EchoProtocolController2D>() == null) return;
            collected = true;
            if (visual != null) visual.enabled = false;
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null) collider.enabled = false;
            EchoProtocolHUD2D.ShowMessage("ARCHIVE CACHE 01 // FRAGMENT COPIED TO LOCAL TRACE", 2.4f);
        }
    }
}
