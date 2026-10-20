using UnityEngine;
using UnityEngine.UI;

public class TutorialOverlayController : MonoBehaviour
{
    private static readonly string[] Titles =
    {
        "JE EERSTE NACHT IN METROVILLE",
        "DASH DOOR DE BREUK",
        "VECHT ALS EEN HELD",
        "VERZAMEL EN ONTWAAK",
        "BLIJF OP DE ROUTE"
    };

    private static readonly string[] Steps =
    {
        "A/D of de pijltjestoetsen bewegen. SPATIE springt; druk nogmaals voor de dubbele sprong.",
        "SHIFT of K voert een snelle dash uit. Tijdens de dash kan contactschade je niet raken.",
        "J of de linkermuisknop gebruikt je Pulse-aanval. Spring op een vijand om hem te verslaan.",
        "Energiefragmenten vullen je krachtmeter én je shardbank. Koop in het startscherm Arc en Nova.",
        "Raak het checkpoint aan. Versla daarna de baas om het portaal te openen. ESC pauzeert het spel."
    };

    [SerializeField] private GameObject panel;
    [SerializeField] private Text title;
    [SerializeField] private Text body;
    [SerializeField] private Text stepLabel;
    private int step;
    private bool showing;

    private void Start()
    {
        if (panel == null) return;
        if (ProfileSaveSystem.TutorialCompleted)
        {
            panel.SetActive(false);
            return;
        }
        showing = true;
        step = 0;
        panel.SetActive(true);
        Time.timeScale = 0f;
        Refresh();
    }

    public void NextStep()
    {
        if (!showing) return;
        step++;
        if (step >= Steps.Length) { Finish(); return; }
        Refresh();
    }

    public void Skip()
    {
        if (showing) Finish();
    }

    private void Finish()
    {
        showing = false;
        if (panel != null) panel.SetActive(false);
        Time.timeScale = 1f;
        ProfileSaveSystem.MarkTutorialComplete();
    }

    private void Refresh()
    {
        if (title != null) title.text = Titles[step];
        if (body != null) body.text = Steps[step];
        if (stepLabel != null) stepLabel.text = "BRIEFING  " + (step + 1) + " / " + Steps.Length;
    }

    private void OnDisable()
    {
        if (showing) Time.timeScale = 1f;
    }
}
