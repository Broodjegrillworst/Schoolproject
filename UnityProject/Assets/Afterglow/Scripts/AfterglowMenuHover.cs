using UnityEngine;
using UnityEngine.EventSystems;

namespace Afterglow
{
    public sealed class AfterglowMenuHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Vector3 targetScale = Vector3.one;
        public void OnPointerEnter(PointerEventData eventData) { targetScale = Vector3.one * 1.035f; }
        public void OnPointerExit(PointerEventData eventData) { targetScale = Vector3.one; }
        private void Update() { transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * 16f); }
    }
}
