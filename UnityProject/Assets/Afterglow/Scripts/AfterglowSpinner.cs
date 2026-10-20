using UnityEngine;

namespace Afterglow
{
    public sealed class AfterglowSpinner : MonoBehaviour
    {
        private float degreesPerSecond;
        public void Configure(float speed) { degreesPerSecond = speed; }
        private void Update() { transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime); }
    }
}
