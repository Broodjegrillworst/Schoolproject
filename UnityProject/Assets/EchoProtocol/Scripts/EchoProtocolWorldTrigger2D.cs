using UnityEngine;

namespace EchoProtocol
{
    public sealed class EchoProtocolWorldTrigger2D : MonoBehaviour
    {
        [SerializeField] private string eventText;
        [SerializeField] private EchoProtocolAnomaly2D apparition;
        private bool consumed;

        public void Configure(string text, EchoProtocolAnomaly2D anomaly = null)
        {
            eventText = text;
            apparition = anomaly;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed || other.GetComponentInParent<EchoProtocolController2D>() == null) return;
            consumed = true;
            EchoProtocolHUD2D.ShowMessage(eventText, 3.2f);
            if (apparition != null) apparition.Appear();
            EchoProtocolCamera2D rig = Camera.main != null ? Camera.main.GetComponent<EchoProtocolCamera2D>() : null;
            if (rig != null) rig.Shake(0.38f, 0.13f);
        }
    }
}
