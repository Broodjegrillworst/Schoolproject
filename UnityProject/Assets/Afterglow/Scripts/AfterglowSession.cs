using UnityEngine;

namespace Afterglow
{
    [System.Serializable]
    public sealed class PlayerProfile
    {
        public int bestUnlockedStage = 1;
        public int completedStages;
        public int currentStage;
        public int deaths;
        public float bestTime = -1f;
        public float masterVolume = 0.8f;
        public float musicVolume = 0.6f;
        public float effectsVolume = 0.9f;
        public bool screenShake = true;
        public int lastCheckpoint;
        public int checkpointStage = -1;
        public float checkpointX = -1f;
        public float checkpointY;
        public int totalRuns;
        public int recoveredMemoryMask;
    }

    public static class AfterglowSession
    {
        private const string SaveKey = "afterglow_city_of_sparks_v1";
        private const string LegacySaveKey = "pulse_rebound_profile_v2";
        public static PlayerProfile Profile { get; private set; }
        public static float RunSeconds { get; private set; }
        public static int RecoveredMemoryCount
        {
            get
            {
                Load();
                int mask = Profile.recoveredMemoryMask & 0b111;
                return (mask & 1) + ((mask >> 1) & 1) + ((mask >> 2) & 1);
            }
        }

        public static bool HasMemory(int index)
        {
            Load();
            return index >= 0 && index < 3 && (Profile.recoveredMemoryMask & (1 << index)) != 0;
        }

        public static bool CollectMemory(int index)
        {
            Load();
            if (index < 0 || index >= 3 || HasMemory(index)) return false;
            Profile.recoveredMemoryMask |= 1 << index;
            Save();
            return true;
        }

        public static void Load()
        {
            if (Profile != null) return;
            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            bool migrated = false;
            if (string.IsNullOrEmpty(json))
            {
                json = PlayerPrefs.GetString(LegacySaveKey, string.Empty);
                migrated = !string.IsNullOrEmpty(json);
            }
            Profile = string.IsNullOrEmpty(json) ? new PlayerProfile() : JsonUtility.FromJson<PlayerProfile>(json);
            if (Profile == null) Profile = new PlayerProfile();
            Profile.bestUnlockedStage = Mathf.Clamp(Profile.bestUnlockedStage, 1, 3);
            Profile.currentStage = Mathf.Clamp(Profile.currentStage, 0, 2);
            if (migrated) Save();
        }

        public static void Save()
        {
            Load();
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Profile));
            PlayerPrefs.Save();
        }

        public static void StartRun(bool fresh)
        {
            Load();
            RunSeconds = 0f;
            if (fresh)
            {
                Profile.completedStages = 0;
                Profile.deaths = 0;
                Profile.bestTime = -1f;
                Profile.lastCheckpoint = 0;
                Profile.checkpointStage = -1;
                Profile.currentStage = 0;
                Profile.checkpointX = -1f;
                Profile.checkpointY = 0f;
                Profile.totalRuns++;
            }
            Save();
        }

        public static void Tick(float delta) { RunSeconds += Mathf.Max(0f, delta); }
        public static void RecordDeath() { Load(); Profile.deaths++; Save(); }
        public static void SetCheckpoint(int checkpoint, Vector2 position)
        {
            Load();
            Profile.lastCheckpoint = checkpoint;
            Profile.checkpointStage = Profile.currentStage;
            Profile.checkpointX = position.x;
            Profile.checkpointY = position.y;
            Save();
        }

        public static void CompleteStage(int stageIndex)
        {
            Load();
            int stageNumber = stageIndex + 1;
            Profile.completedStages = Mathf.Max(Profile.completedStages, stageNumber);
            Profile.bestUnlockedStage = Mathf.Clamp(Mathf.Max(Profile.bestUnlockedStage, stageNumber + 1), 1, 3);
            Profile.lastCheckpoint = 0;
            Profile.checkpointStage = -1;
            Profile.currentStage = Mathf.Min(stageNumber, 2);
            Profile.checkpointX = -1f;
            Profile.checkpointY = 0f;
            if (stageNumber == 3 && (Profile.bestTime < 0f || RunSeconds < Profile.bestTime)) Profile.bestTime = RunSeconds;
            Save();
        }

        public static void SetCurrentStage(int stage) { Load(); Profile.currentStage = Mathf.Clamp(stage, 0, 2); Save(); }
        public static void SetAudio(float master, float music, float effects)
        {
            Load();
            Profile.masterVolume = Mathf.Clamp01(master);
            Profile.musicVolume = Mathf.Clamp01(music);
            Profile.effectsVolume = Mathf.Clamp01(effects);
            Save();
        }
        public static void SetScreenShake(bool enabled) { Load(); Profile.screenShake = enabled; Save(); }
    }
}
