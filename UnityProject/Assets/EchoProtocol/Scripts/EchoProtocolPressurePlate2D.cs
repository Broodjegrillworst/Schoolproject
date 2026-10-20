using System.Collections.Generic;
using UnityEngine;

namespace EchoProtocol
{
    public sealed class EchoProtocolPressurePlate2D : MonoBehaviour
    {
        [SerializeField] private EchoProtocolSecurityDoor2D door;
        [SerializeField] private SpriteRenderer surface;
        private readonly HashSet<Collider2D> occupants = new HashSet<Collider2D>();
        private bool echoOnly;

        public void Configure(EchoProtocolSecurityDoor2D target, SpriteRenderer plateSurface, bool acceptEchoesOnly)
        {
            door = target;
            surface = plateSurface;
            echoOnly = acceptEchoesOnly;
        }

        private void OnTriggerEnter2D(Collider2D other) => Register(other, true);
        private void OnTriggerExit2D(Collider2D other) => Register(other, false);

        private void Register(Collider2D other, bool entering)
        {
            bool isEcho = other.GetComponentInParent<EchoProtocolActor2D>() != null;
            bool isPlayer = other.GetComponentInParent<EchoProtocolController2D>() != null;
            bool valid = echoOnly ? isEcho : (isPlayer || isEcho);
            if (!valid) return;
            if (entering) occupants.Add(other);
            else occupants.Remove(other);
            bool active = occupants.Count > 0;
            if (door != null) door.SetHeldOpen(active);
            if (surface != null) surface.color = active ? new Color(0.62f, 0.9f, 0.73f) : new Color(0.8f, 0.34f, 0.18f);
        }
    }
}

