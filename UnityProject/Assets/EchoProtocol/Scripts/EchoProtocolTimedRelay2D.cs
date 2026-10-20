using UnityEngine;

namespace EchoProtocol
{
    public sealed class EchoProtocolTimedRelay2D : EchoProtocolInteractable2D
    {
        [SerializeField] private EchoProtocolSecurityDoor2D door;
        [SerializeField] private SpriteRenderer indicator;
        [SerializeField] private float openDuration = 14f;

        public void Configure(EchoProtocolSecurityDoor2D target, SpriteRenderer light, float duration)
        {
            door = target;
            indicator = light;
            openDuration = duration;
        }

        public override void OnPlayerInteract() => EchoProtocolHUD2D.ShowMessage("LIVE ACCESS DENIED // RECORD THE CONSOLE ACTION", 1.6f);
        public override void OnEchoInteract() => Activate();

        private void Activate()
        {
            if (door != null) door.OpenForSeconds(openDuration);
            if (indicator != null) indicator.color = new Color(0.62f, 0.9f, 0.73f);
            EchoProtocolHUD2D.ShowMessage("REMOTE ECHO AUTH // ACCESS GRANTED", 1.5f);
        }
    }
}
