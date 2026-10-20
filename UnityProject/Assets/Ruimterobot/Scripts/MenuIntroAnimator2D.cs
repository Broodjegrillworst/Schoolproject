using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MenuIntroAnimator2D : MonoBehaviour
{
    [SerializeField] private Transform hero;
    [SerializeField] private Text title;
    [SerializeField] private Text introText;
    [SerializeField] private Color titleColorA = new Color(0.94f, 0.93f, 0.87f);
    [SerializeField] private Color titleColorB = new Color(0.60f, 0.83f, 0.84f);

    private Vector3 heroStartPosition;
    private Quaternion heroStartRotation;

    public void Configure(Transform heroTransform, Text titleText, Text messageText)
    {
        hero = heroTransform;
        title = titleText;
        introText = messageText;
    }

    private void Start()
    {
        if (hero != null)
        {
            heroStartPosition = hero.localPosition;
            heroStartRotation = hero.localRotation;
        }
        if (introText != null)
            StartCoroutine(PlayIntroLines());
    }

    private void Update()
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.15f);
        if (title != null)
            title.color = Color.Lerp(titleColorA, titleColorB, pulse * 0.52f);
        if (hero != null)
        {
            hero.localPosition = heroStartPosition + Vector3.up * (Mathf.Sin(Time.unscaledTime * 1.35f) * 0.085f);
            hero.localRotation = heroStartRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 0.8f) * 1.7f);
        }
    }

    private IEnumerator PlayIntroLines()
    {
        string[] lines = { "METROVILLE WORDT AANGEVALLEN", "DRIE SUPERSCHURKEN ZIJN GESPOT", "PULSE IS ONDERWEG" };
        for (int i = 0; i < lines.Length; i++)
        {
            introText.text = lines[i];
            if (ArcadeAudioManager.Instance != null)
                ArcadeAudioManager.Instance.PlayUi();
            yield return new WaitForSecondsRealtime(1.15f);
        }
        introText.text = "MISSIE: RED DE STAD";
    }
}
