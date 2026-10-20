using UnityEngine;

namespace EchoProtocol
{
    public sealed class EchoProtocolParallaxLayer2D : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float factor = 0.2f;
        private Vector3 origin;
        private Vector3 cameraOrigin;

        public void Configure(Transform cameraRig, float scrollFactor)
        {
            cameraTransform = cameraRig;
            factor = scrollFactor;
            origin = transform.position;
            cameraOrigin = cameraTransform != null ? cameraTransform.position : Vector3.zero;
        }

        private void LateUpdate()
        {
            if (cameraTransform == null) return;
            Vector3 delta = cameraTransform.position - cameraOrigin;
            transform.position = origin + new Vector3(delta.x * factor, delta.y * factor, 0f);
        }
    }
}
