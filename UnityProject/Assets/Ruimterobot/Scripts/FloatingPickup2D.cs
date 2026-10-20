using UnityEngine;

public class FloatingPickup2D : MonoBehaviour
{
    [SerializeField] private float bobHeight = 0.12f;
    [SerializeField] private float bobSpeed = 2.5f;
    [SerializeField] private float spinSpeed = 70f;

    private Vector3 startPosition;
    private float phase;

    private void Start()
    {
        startPosition = transform.localPosition;
        phase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        Vector3 position = startPosition;
        position.y += Mathf.Sin(Time.time * bobSpeed + phase) * bobHeight;
        transform.localPosition = position;
        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
    }
}
