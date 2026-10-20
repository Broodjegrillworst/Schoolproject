using System.Collections;
using UnityEngine;

namespace Afterglow
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class AfterglowCollapsingPlatform : MonoBehaviour
    {
        private Collider2D platformCollider;
        private SpriteRenderer renderer;
        private Vector3 home;
        private bool collapsing;

        private void Awake()
        {
            platformCollider = GetComponent<Collider2D>();
            renderer = GetComponent<SpriteRenderer>();
            home = transform.position;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!collapsing && collision.collider.GetComponentInParent<AfterglowPlayer>() != null) StartCoroutine(Collapse());
        }

        private IEnumerator Collapse()
        {
            collapsing = true;
            float timer = .55f;
            while (timer > 0f)
            {
                timer -= Time.deltaTime;
                if (renderer != null) renderer.color = Mathf.Sin(Time.time * 26f) > 0f ? new Color(1f, .44f, .34f) : Color.white;
                yield return null;
            }
            if (renderer != null) renderer.color = new Color(.35f, .46f, .58f);
            platformCollider.enabled = false;
            float fall = 0f;
            while (fall < .6f)
            {
                fall += Time.deltaTime;
                transform.position = home + Vector3.down * (fall * fall * 13f);
                yield return null;
            }
            yield return new WaitForSeconds(2.2f);
            transform.position = home;
            if (renderer != null) renderer.color = Color.white;
            platformCollider.enabled = true;
            collapsing = false;
        }
    }
}
