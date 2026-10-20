using System;
using UnityEngine;

public static class GameSession
{
    public static int CrystalsCollected { get; private set; }
    public static int LivesRemaining { get; private set; }
    public static int MaximumLives { get; private set; }
    public static bool HasActiveRun { get; private set; }
    public static bool HasAegisShield { get; private set; }
    public static bool SkyboundUnlocked { get; private set; }
    public static event Action<int> CrystalsChanged;
    public static event Action<int, int> LivesChanged;

    private static int startingLivesForRun;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForNewPlaySession()
    {
        CrystalsCollected = 0;
        LivesRemaining = 0;
        MaximumLives = 0;
        startingLivesForRun = 0;
        HasActiveRun = false;
        HasAegisShield = false;
        SkyboundUnlocked = ProfileSaveSystem.SkyboundUnlocked;
        CrystalsChanged = null;
        LivesChanged = null;
    }

    public static void StartNewGame()
    {
        CrystalsCollected = 0;
        LivesRemaining = 0;
        MaximumLives = 0;
        startingLivesForRun = 0;
        HasActiveRun = false;
        HasAegisShield = false;
        SkyboundUnlocked = false;
        ProfileSaveSystem.ClearCheckpoint();
        RunStatistics.BeginRun();
        CrystalsChanged?.Invoke(CrystalsCollected);
    }

    public static void StartContinuation()
    {
        CrystalsCollected = 0;
        LivesRemaining = 0;
        MaximumLives = 0;
        startingLivesForRun = 0;
        HasActiveRun = false;
        HasAegisShield = false;
        SkyboundUnlocked = ProfileSaveSystem.SkyboundUnlocked;
        RunStatistics.BeginRun();
        CrystalsChanged?.Invoke(CrystalsCollected);
    }

    public static int InitializePlayerLives(int startingLives, int maximumLives)
    {
        int safeMaximum = Mathf.Max(1, maximumLives);
        startingLives = ProfileSaveSystem.StartingLives(startingLives);
        if (!HasActiveRun)
        {
            MaximumLives = safeMaximum;
            startingLivesForRun = Mathf.Clamp(startingLives, 1, safeMaximum);
            LivesRemaining = startingLivesForRun;
            HasActiveRun = true;
        }
        else
        {
            MaximumLives = safeMaximum;
            LivesRemaining = Mathf.Clamp(LivesRemaining, 0, safeMaximum);
        }

        LivesChanged?.Invoke(LivesRemaining, MaximumLives);
        return LivesRemaining;
    }

    public static void SetLivesRemaining(int value)
    {
        if (!HasActiveRun)
            return;

        LivesRemaining = Mathf.Clamp(value, 0, Mathf.Max(1, MaximumLives));
        LivesChanged?.Invoke(LivesRemaining, MaximumLives);
    }

    public static void PrepareForLevelRetry()
    {
        if (!HasActiveRun)
            return;

        LivesRemaining = Mathf.Clamp(startingLivesForRun, 1, Mathf.Max(1, MaximumLives));
        LivesChanged?.Invoke(LivesRemaining, MaximumLives);
    }

    public static void CollectCrystal()
    {
        CrystalsCollected++;
        RunStatistics.RecordShard();
        ProfileSaveSystem.AddShards(1);
        AchievementSystem.OnShardCollected(CrystalsCollected);
        CrystalsChanged?.Invoke(CrystalsCollected);
    }

    public static void RecordEnemyDefeated()
    {
        RunStatistics.RecordEnemy();
        AchievementSystem.OnEnemyDefeated();
    }

    public static void RecordBossDefeated()
    {
        RunStatistics.RecordBoss();
        AchievementSystem.OnBossDefeated();
    }

    public static void GrantAegisShield()
    {
        HasAegisShield = true;
    }

    public static bool ConsumeAegisShield()
    {
        if (!HasAegisShield)
            return false;

        HasAegisShield = false;
        return true;
    }

    public static void UnlockSkybound()
    {
        SkyboundUnlocked = true;
        ProfileSaveSystem.UnlockSkybound();
    }
}
