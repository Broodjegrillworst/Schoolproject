using System;
using UnityEngine;

[Flags]
public enum VesperAbility
{
    None = 0,
    DoubleJump = 1,
    WallJump = 2,
    AirDash = 4,
    SuperDash = 8,
    EnergyWave = 16
}

public enum VesperSkillPath
{
    Strike,
    Reservoir,
    Vitality
}

public static class VesperProgress
{
    private const string Prefix = "VESPER_";
    private const string AbilityKey = Prefix + "Abilities";
    private const string ShardKey = Prefix + "Shards";
    private const string RegionKey = Prefix + "Regions";
    private const string TierKey = Prefix + "SkillTier";
    private const string BossKey = Prefix + "Bosses";
    private const string ReservoirKey = Prefix + "ReservoirTier";
    private const string VitalityKey = Prefix + "VitalityTier";
    private const string VaultsKey = Prefix + "SolvedVaults";
    private const string VaultRewardsKey = Prefix + "ClaimedVaultRewards";
    private static bool loaded;
    private static readonly int[] roomMasks = new int[5];
    private static readonly int[] pickupMasks = new int[5];

    public static event Action Changed;
    public static VesperAbility Abilities { get; private set; }
    public static int Shards { get; private set; }
    public static int DiscoveredRegions { get; private set; }
    public static int SkillTier { get; private set; }
    public static int DefeatedBosses { get; private set; }
    public static int ReservoirTier { get; private set; }
    public static int VitalityTier { get; private set; }
    public static int SolvedVaults { get; private set; }
    public static int ClaimedVaultRewards { get; private set; }
    public static int SpawnFromSide { get; set; } = -1;
    public static bool UseCustomSpawn { get; set; }
    public static Vector2 CustomSpawnPosition { get; set; }
    public static int CurrentRegion { get; set; } = 1;

    public static void Load()
    {
        if (loaded) return;
        loaded = true;
        Abilities = (VesperAbility)PlayerPrefs.GetInt(AbilityKey, 0);
        Shards = PlayerPrefs.GetInt(ShardKey, 0);
        DiscoveredRegions = PlayerPrefs.GetInt(RegionKey, 1);
        SkillTier = Mathf.Clamp(PlayerPrefs.GetInt(TierKey, 0), 0, 3);
        DefeatedBosses = PlayerPrefs.GetInt(BossKey, 0);
        ReservoirTier = Mathf.Clamp(PlayerPrefs.GetInt(ReservoirKey, 0), 0, 3);
        VitalityTier = Mathf.Clamp(PlayerPrefs.GetInt(VitalityKey, 0), 0, 3);
        SolvedVaults = PlayerPrefs.GetInt(VaultsKey, 0);
        ClaimedVaultRewards = PlayerPrefs.GetInt(VaultRewardsKey, 0);
        CurrentRegion = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "CurrentRegion", 1), 1, 5);
        for (int i = 0; i < roomMasks.Length; i++)
        {
            roomMasks[i] = PlayerPrefs.GetInt(Prefix + "RoomMap_" + (i + 1), 0);
            pickupMasks[i] = PlayerPrefs.GetInt(Prefix + "Pickups_" + (i + 1), 0);
        }
    }

    public static bool Has(VesperAbility ability)
    {
        Load();
        return (Abilities & ability) == ability;
    }

    public static bool Unlock(VesperAbility ability)
    {
        Load();
        if (Has(ability)) return false;
        Abilities |= ability;
        Save();
        return true;
    }

    public static void Discover(int region)
    {
        Load();
        DiscoveredRegions |= 1 << Mathf.Clamp(region - 1, 0, 4);
        CurrentRegion = region;
        PlayerPrefs.SetInt(RegionKey, DiscoveredRegions);
        PlayerPrefs.SetInt(Prefix + "CurrentRegion", region);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static void AddShards(int amount)
    {
        Load();
        Shards = Mathf.Max(0, Shards + amount);
        Save();
    }

    public static bool BuyUpgrade()
    {
        return BuyUpgrade(VesperSkillPath.Strike);
    }

    public static int UpgradeLevel(VesperSkillPath path)
    {
        Load();
        return path switch
        {
            VesperSkillPath.Strike => SkillTier,
            VesperSkillPath.Reservoir => ReservoirTier,
            VesperSkillPath.Vitality => VitalityTier,
            _ => 0
        };
    }

    public static int UpgradeCost(VesperSkillPath path) => 3 + UpgradeLevel(path) * 3;

    public static bool BuyUpgrade(VesperSkillPath path)
    {
        Load();
        int level = UpgradeLevel(path);
        int cost = 3 + level * 3;
        if (level >= 3 || Shards < cost) return false;
        Shards -= cost;
        switch (path)
        {
            case VesperSkillPath.Strike: SkillTier++; break;
            case VesperSkillPath.Reservoir: ReservoirTier++; break;
            case VesperSkillPath.Vitality: VitalityTier++; break;
        }
        Save();
        return true;
    }

    public static bool IsDiscovered(int region)
    {
        Load();
        return (DiscoveredRegions & (1 << Mathf.Clamp(region - 1, 0, 4))) != 0;
    }

    public static void DiscoverRoom(int region, int room)
    {
        Load();
        int area = Mathf.Clamp(region - 1, 0, roomMasks.Length - 1);
        int bit = 1 << Mathf.Clamp(room, 0, 30);
        if ((roomMasks[area] & bit) != 0) return;
        roomMasks[area] |= bit;
        PlayerPrefs.SetInt(Prefix + "RoomMap_" + (area + 1), roomMasks[area]);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static bool IsRoomDiscovered(int region, int room)
    {
        Load();
        int area = Mathf.Clamp(region - 1, 0, roomMasks.Length - 1);
        return (roomMasks[area] & (1 << Mathf.Clamp(room, 0, 30))) != 0;
    }

    public static bool IsPickupCollected(int region, int pickup)
    {
        Load();
        int area = Mathf.Clamp(region - 1, 0, pickupMasks.Length - 1);
        return (pickupMasks[area] & (1 << Mathf.Clamp(pickup, 0, 30))) != 0;
    }

    public static void MarkPickupCollected(int region, int pickup)
    {
        Load();
        int area = Mathf.Clamp(region - 1, 0, pickupMasks.Length - 1);
        pickupMasks[area] |= 1 << Mathf.Clamp(pickup, 0, 30);
        PlayerPrefs.SetInt(Prefix + "Pickups_" + (area + 1), pickupMasks[area]);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static bool HasDefeatedBoss(int region)
    {
        Load();
        return (DefeatedBosses & (1 << Mathf.Clamp(region - 1, 0, 4))) != 0;
    }

    public static bool IsVaultSolved(int region)
    {
        Load();
        return (SolvedVaults & (1 << Mathf.Clamp(region - 1, 0, 4))) != 0;
    }

    public static void MarkVaultSolved(int region)
    {
        Load();
        SolvedVaults |= 1 << Mathf.Clamp(region - 1, 0, 4);
        Save();
    }

    public static bool IsVaultRewardClaimed(int region)
    {
        Load();
        return (ClaimedVaultRewards & (1 << Mathf.Clamp(region - 1, 0, 4))) != 0;
    }

    public static void MarkVaultRewardClaimed(int region)
    {
        Load();
        ClaimedVaultRewards |= 1 << Mathf.Clamp(region - 1, 0, 4);
        Save();
    }

    public static void MarkBossDefeated(int region)
    {
        Load();
        DefeatedBosses |= 1 << Mathf.Clamp(region - 1, 0, 4);
        Save();
    }

    public static void Save()
    {
        PlayerPrefs.SetInt(AbilityKey, (int)Abilities);
        PlayerPrefs.SetInt(ShardKey, Shards);
        PlayerPrefs.SetInt(RegionKey, DiscoveredRegions);
        PlayerPrefs.SetInt(TierKey, SkillTier);
        PlayerPrefs.SetInt(BossKey, DefeatedBosses);
        PlayerPrefs.SetInt(ReservoirKey, ReservoirTier);
        PlayerPrefs.SetInt(VitalityKey, VitalityTier);
        PlayerPrefs.SetInt(VaultsKey, SolvedVaults);
        PlayerPrefs.SetInt(VaultRewardsKey, ClaimedVaultRewards);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static void NewGame()
    {
        PlayerPrefs.DeleteKey(AbilityKey);
        PlayerPrefs.DeleteKey(ShardKey);
        PlayerPrefs.DeleteKey(RegionKey);
        PlayerPrefs.DeleteKey(TierKey);
        PlayerPrefs.DeleteKey(BossKey);
        PlayerPrefs.DeleteKey(ReservoirKey);
        PlayerPrefs.DeleteKey(VitalityKey);
        PlayerPrefs.DeleteKey(VaultsKey);
        PlayerPrefs.DeleteKey(VaultRewardsKey);
        PlayerPrefs.DeleteKey(Prefix + "CurrentRegion");
        for (int region = 1; region <= 5; region++)
        {
            PlayerPrefs.DeleteKey("VESPER_CheckpointX_" + region);
            PlayerPrefs.DeleteKey("VESPER_CheckpointY_" + region);
            PlayerPrefs.DeleteKey(Prefix + "RoomMap_" + region);
            PlayerPrefs.DeleteKey(Prefix + "Pickups_" + region);
            roomMasks[region - 1] = 0;
            pickupMasks[region - 1] = 0;
        }
        Abilities = VesperAbility.None;
        Shards = 0;
        DiscoveredRegions = 1;
        SkillTier = 0;
        DefeatedBosses = 0;
        ReservoirTier = 0;
        VitalityTier = 0;
        SolvedVaults = 0;
        ClaimedVaultRewards = 0;
        CurrentRegion = 1;
        SpawnFromSide = -1;
        UseCustomSpawn = false;
        Save();
    }
}
