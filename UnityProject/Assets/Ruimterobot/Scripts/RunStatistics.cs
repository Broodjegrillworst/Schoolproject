using UnityEngine;

/// <summary>Run-wide counters survive level changes, then the final summary is written to the profile.</summary>
public static class RunStatistics
{
    public static float ElapsedSeconds { get; private set; }
    public static int Deaths { get; private set; }
    public static int EnemiesDefeated { get; private set; }
    public static int BossesDefeated { get; private set; }
    public static int ShardsCollected { get; private set; }
    public static int DashUses { get; private set; }
    public static bool IsActive { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        ElapsedSeconds = 0f;
        Deaths = 0;
        EnemiesDefeated = 0;
        BossesDefeated = 0;
        ShardsCollected = 0;
        DashUses = 0;
        IsActive = false;
    }

    public static void BeginRun()
    {
        ElapsedSeconds = 0f;
        Deaths = EnemiesDefeated = BossesDefeated = ShardsCollected = DashUses = 0;
        IsActive = true;
    }

    public static void Tick(float deltaTime)
    {
        if (IsActive && deltaTime > 0f) ElapsedSeconds += deltaTime;
    }

    public static void RecordDeath() { if (IsActive) Deaths++; }
    public static void RecordEnemy() { if (IsActive) EnemiesDefeated++; }
    public static void RecordBoss() { if (IsActive) BossesDefeated++; }
    public static void RecordShard() { if (IsActive) ShardsCollected++; }
    public static void RecordDash() { if (IsActive) DashUses++; }

    public static void CompleteRun(int score)
    {
        if (!IsActive) return;
        IsActive = false;
        ProfileSaveSystem.RecordRun(ElapsedSeconds, Deaths, EnemiesDefeated, BossesDefeated,
            ShardsCollected, score);
        AchievementSystem.CheckEndOfRun();
    }
}
