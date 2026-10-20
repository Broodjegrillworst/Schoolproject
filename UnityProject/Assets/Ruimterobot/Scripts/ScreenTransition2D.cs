using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScreenTransition2D : MonoBehaviour
{
    private static ScreenTransition2D instance;
    private CanvasGroup group;
    private bool transitioning;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        if (instance == null) CreateRuntimeOverlay();
        if (!instance.transitioning) instance.StartCoroutine(instance.LoadRoutine(sceneName));
    }

    private static void CreateRuntimeOverlay()
    {
        GameObject root = new GameObject("PULSE Scene Transition");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        root.AddComponent<GraphicRaycaster>();
        GameObject black = new GameObject("Fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        black.transform.SetParent(root.transform, false);
        RectTransform rect = black.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        black.GetComponent<Image>().color = Color.black;
        instance = root.AddComponent<ScreenTransition2D>();
        instance.group = black.GetComponent<CanvasGroup>();
        instance.group.alpha = 0f;
        instance.group.blocksRaycasts = false;
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        transitioning = true;
        if (group == null) CreateRuntimeOverlay();
        group.blocksRaycasts = true;
        yield return Fade(0f, 1f);
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        if (load == null)
        {
            group.blocksRaycasts = false;
            transitioning = false;
            yield break;
        }
        while (!load.isDone) yield return null;
        yield return null;
        yield return Fade(1f, 0f);
        group.blocksRaycasts = false;
        transitioning = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        const float duration = 0.22f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        group.alpha = to;
    }
}
