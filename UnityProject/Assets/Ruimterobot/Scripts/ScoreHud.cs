using UnityEngine;
using UnityEngine.UI;

public class ScoreHud : MonoBehaviour
{
    [SerializeField] private Text scoreLabel;
    [SerializeField] private Text summaryLabel;

    private void OnEnable()
    {
        GameSession.CrystalsChanged += UpdateLabel;
        UpdateLabel(GameSession.CrystalsCollected);
        if (summaryLabel != null)
        {
            int totalSeconds = Mathf.FloorToInt(RunStatistics.ElapsedSeconds);
            summaryLabel.text = "TIJD  " + (totalSeconds / 60).ToString("00") + ":" + (totalSeconds % 60).ToString("00") +
                "     VIJANDEN  " + RunStatistics.EnemiesDefeated +
                "     BAZEN  " + RunStatistics.BossesDefeated +
                "     SCHADES  " + RunStatistics.Deaths;
        }
    }

    private void OnDisable()
    {
        GameSession.CrystalsChanged -= UpdateLabel;
    }

    private void UpdateLabel(int count)
    {
        if (scoreLabel != null)
            scoreLabel.text = "SHARDS  " + count.ToString("00");
    }
}

