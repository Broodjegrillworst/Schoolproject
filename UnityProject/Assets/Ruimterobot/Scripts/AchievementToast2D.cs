using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AchievementToast2D : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Text title;
    [SerializeField] private Text message;
    private Coroutine hideRoutine;

    private void OnEnable() => ProfileSaveSystem.AchievementUnlocked += Show;
    private void OnDisable() => ProfileSaveSystem.AchievementUnlocked -= Show;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Show(string id)
    {
        if (panel == null) return;
        if (title != null) title.text = "PRESTATIE ONTGRENDELD  //  " + AchievementSystem.GetDisplayName(id);
        if (message != null) message.text = AchievementSystem.GetDescription(id);
        panel.SetActive(true);
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(3.2f);
        if (panel != null) panel.SetActive(false);
        hideRoutine = null;
    }
}
