using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum GameDifficulty
{
    Story = 0,
    Hero = 1,
    Legend = 2
}

/// <summary>Small versioned JSON profile for unlocks, achievements, settings and the last run.</summary>
public static class ProfileSaveSystem
{
    [Serializable]
    public sealed class ProfileData
    {
        public int version = 1;
        public int difficulty = (int)GameDifficulty.Hero;
        public int bankedShards;
        public int powerRank;
        public bool skyboundUnlocked;
        public int highestUnlockedLevel = 1;
        public bool tutorialCompleted;
        public List<string> achievements = new List<string>();
        public int totalRuns;
        public int bestScore;
        public float lastRunSeconds;
        public int lastRunDeaths;
        public int lastRunEnemies;
        public int lastRunBosses;
        public int lastRunShards;
        public string checkpointScene = string.Empty;
        public float checkpointX;
        public float checkpointY;
    }

    private static ProfileData data;
    private static bool loaded;
    private static string SavePath => Path.Combine(Application.persistentDataPath, "PULSE_profile.json");

    public static ProfileData Data { get { EnsureLoaded(); return data; } }
    public static GameDifficulty Difficulty => (GameDifficulty)Mathf.Clamp(Data.difficulty, 0, 2);
    public static int PowerRank => Mathf.Clamp(Data.powerRank, 0, 3);
    public static int BankedShards => Mathf.Max(0, Data.bankedShards);
    public static int HighestUnlockedLevel => Mathf.Clamp(Data.highestUnlockedLevel, 1, 3);
    public static bool TutorialCompleted => Data.tutorialCompleted;
    public static bool SkyboundUnlocked => Data.skyboundUnlocked;
    public static event Action<string> AchievementUnlocked;
    public static event Action ProfileChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        data = null;
        loaded = false;
        AchievementUnlocked = null;
        ProfileChanged = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadBeforeFirstScene() => Load();

    public static void Load()
    {
        if (loaded) return;
        loaded = true;
        try
        {
            if (File.Exists(SavePath))
                data = JsonUtility.FromJson<ProfileData>(File.ReadAllText(SavePath));
        }
        catch (Exception exception)
        {
            Debug.LogWarning("PULSE-profiel kon niet worden gelezen. Standaardinstellingen worden gebruikt: " + exception.Message);
        }
        if (data == null) data = new ProfileData();
        if (data.achievements == null) data.achievements = new List<string>();
        data.version = 1;
        data.difficulty = Mathf.Clamp(data.difficulty, 0, 2);
        data.powerRank = Mathf.Clamp(data.powerRank, 0, 3);
        data.highestUnlockedLevel = Mathf.Clamp(data.highestUnlockedLevel, 1, 3);
    }

    public static void Save()
    {
        EnsureLoaded();
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            PlayerPrefs.Save();
        }
        catch (Exception exception)
        {
            Debug.LogError("PULSE-profiel kon niet worden opgeslagen: " + exception.Message);
        }
        ProfileChanged?.Invoke();
    }

    public static string SaveLocation => SavePath;

    public static string GetDifficultyLabel()
    {
        switch (Difficulty)
        {
            case GameDifficulty.Story: return "VERHAAL — 5 levens, minder vijanddruk";
            case GameDifficulty.Legend: return "LEGENDE — 2 levens, agressieve vijanden";
            default: return "HELD — 3 levens, standaard uitdaging";
        }
    }

    public static int StartingLives(int fallback) => Difficulty == GameDifficulty.Story ? 5 :
        Difficulty == GameDifficulty.Legend ? 2 : Mathf.Max(1, fallback);

    public static float EnemyHealthMultiplier => Difficulty == GameDifficulty.Story ? 0.8f :
        Difficulty == GameDifficulty.Legend ? 1.35f : 1f;

    public static float EnemySpeedMultiplier => Difficulty == GameDifficulty.Story ? 0.85f :
        Difficulty == GameDifficulty.Legend ? 1.18f : 1f;

    public static float BossHealthMultiplier => Difficulty == GameDifficulty.Story ? 0.82f :
        Difficulty == GameDifficulty.Legend ? 1.3f : 1f;

    public static void CycleDifficulty()
    {
        EnsureLoaded();
        data.difficulty = (data.difficulty + 1) % 3;
        Save();
    }

    public static void AddShards(int amount)
    {
        if (amount <= 0) return;
        EnsureLoaded();
        data.bankedShards = Mathf.Max(0, data.bankedShards + amount);
        Save();
    }

    public static bool TryPurchasePowerRank(out int newRank, out int remainingShards)
    {
        EnsureLoaded();
        int[] prices = { 4, 8, 12 };
        int rank = Mathf.Clamp(data.powerRank, 0, 3);
        if (rank >= prices.Length || data.bankedShards < prices[rank])
        {
            newRank = rank;
            remainingShards = Mathf.Max(0, data.bankedShards);
            return false;
        }
        data.bankedShards -= prices[rank];
        data.powerRank = rank + 1;
        newRank = data.powerRank;
        remainingShards = data.bankedShards;
        Save();
        return true;
    }

    public static int NextPowerPrice => PowerRank >= 3 ? 0 : new[] { 4, 8, 12 }[PowerRank];

    public static bool HasAchievement(string id)
    {
        EnsureLoaded();
        return !string.IsNullOrEmpty(id) && data.achievements.Contains(id);
    }

    public static void UnlockAchievement(string id)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(id) || data.achievements.Contains(id)) return;
        data.achievements.Add(id);
        Save();
        if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayAchievement();
        AchievementUnlocked?.Invoke(id);
    }

    public static void MarkTutorialComplete()
    {
        EnsureLoaded();
        data.tutorialCompleted = true;
        Save();
    }

    public static void UnlockSkybound()
    {
        EnsureLoaded();
        data.skyboundUnlocked = true;
        Save();
    }

    public static void RecordLevelUnlocked(int levelNumber)
    {
        EnsureLoaded();
        data.highestUnlockedLevel = Mathf.Max(data.highestUnlockedLevel, Mathf.Clamp(levelNumber, 1, 3));
        Save();
    }

    public static string GetContinueSceneName() => "Level" + HighestUnlockedLevel;

    public static void SetCheckpoint(string sceneName, Vector2 position)
    {
        EnsureLoaded();
        data.checkpointScene = sceneName;
        data.checkpointX = position.x;
        data.checkpointY = position.y;
        Save();
    }

    public static bool TryGetCheckpoint(string sceneName, out Vector2 position)
    {
        EnsureLoaded();
        position = new Vector2(data.checkpointX, data.checkpointY);
        return !string.IsNullOrEmpty(data.checkpointScene) && data.checkpointScene == sceneName;
    }

    public static void ClearCheckpoint()
    {
        EnsureLoaded();
        data.checkpointScene = string.Empty;
        Save();
    }

    public static void RecordRun(float seconds, int deaths, int enemies, int bosses, int shards, int score)
    {
        EnsureLoaded();
        data.totalRuns++;
        data.lastRunSeconds = Mathf.Max(0f, seconds);
        data.lastRunDeaths = Mathf.Max(0, deaths);
        data.lastRunEnemies = Mathf.Max(0, enemies);
        data.lastRunBosses = Mathf.Max(0, bosses);
        data.lastRunShards = Mathf.Max(0, shards);
        data.bestScore = Mathf.Max(data.bestScore, score);
        Save();
    }

    private static void EnsureLoaded()
    {
        if (!loaded) Load();
        if (data == null) data = new ProfileData();
    }
}
