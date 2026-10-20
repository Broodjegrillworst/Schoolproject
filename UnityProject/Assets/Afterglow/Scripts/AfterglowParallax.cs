using UnityEngine;

namespace Afterglow
{
    public sealed class AfterglowParallax : MonoBehaviour
    {
        [SerializeField] private float factor = .18f;
        private Transform cameraTransform;
        private Vector3 home;

        public void Configure(Transform cameraRef, float parallaxFactor)
        {
            cameraTransform = cameraRef;
            factor = parallaxFactor;
            home = transform.position;
        }

        private void Awake()
        {
            home = transform.position;
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (cameraTransform == null) return;
            Vector3 p = home;
            p.x += cameraTransform.position.x * factor;
            transform.position = p;
        }
    }
}
