using UnityEngine;
using UnityEngine.EventSystems;

public class UiButtonMotion2D : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private float selectedScale = 1.045f;
    [SerializeField] private float easing = 14f;
    private Vector3 baseScale;
    private bool highlighted;

    private void Awake() => baseScale = transform.localScale;

    private void Update()
    {
        Vector3 target = highlighted ? baseScale * selectedScale : baseScale;
        transform.localScale = Vector3.Lerp(transform.localScale, target,
            1f - Mathf.Exp(-easing * Time.unscaledDeltaTime));
    }

    public void OnPointerEnter(PointerEventData eventData) => highlighted = true;
    public void OnPointerExit(PointerEventData eventData) => highlighted = false;
    public void OnSelect(BaseEventData eventData) => highlighted = true;
    public void OnDeselect(BaseEventData eventData) => highlighted = false;
}
