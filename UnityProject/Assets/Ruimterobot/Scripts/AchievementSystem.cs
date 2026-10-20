public static class AchievementSystem
{
    public const string FirstShard = "first_shard";
    public const string FirstEnemy = "first_enemy";
    public const string FirstBoss = "first_boss";
    public const string UpgradeHero = "upgrade_hero";
    public const string RiftClosed = "rift_closed";
    public const string CleanRun = "clean_run";
    public const string ShardCollector = "shard_collector";

    public static void OnShardCollected(int count)
    {
        ProfileSaveSystem.UnlockAchievement(FirstShard);
        if (count >= 25) ProfileSaveSystem.UnlockAchievement(ShardCollector);
    }

    public static void OnEnemyDefeated() => ProfileSaveSystem.UnlockAchievement(FirstEnemy);
    public static void OnBossDefeated() => ProfileSaveSystem.UnlockAchievement(FirstBoss);
    public static void OnUpgradePurchased() => ProfileSaveSystem.UnlockAchievement(UpgradeHero);

    public static void CheckEndOfRun()
    {
        ProfileSaveSystem.UnlockAchievement(RiftClosed);
        if (RunStatistics.Deaths == 0) ProfileSaveSystem.UnlockAchievement(CleanRun);
        if (RunStatistics.ShardsCollected >= 25) ProfileSaveSystem.UnlockAchievement(ShardCollector);
    }

    public static string GetDisplayName(string id)
    {
        switch (id)
        {
            case FirstShard: return "Eerste vonk";
            case FirstEnemy: return "Metroville verdediger";
            case FirstBoss: return "Baas verslagen";
            case UpgradeHero: return "Kracht ontwaakt";
            case RiftClosed: return "De rift gesloten";
            case CleanRun: return "Onbreekbare held";
            case ShardCollector: return "Shardverzamelaar";
            default: return id;
        }
    }

    public static string GetDescription(string id)
    {
        switch (id)
        {
            case FirstShard: return "Verzamel je eerste energiefragment.";
            case FirstEnemy: return "Versla een Rift-vijand.";
            case FirstBoss: return "Versla een eindbaas.";
            case UpgradeHero: return "Koop een blijvende krachtupgrade.";
            case RiftClosed: return "Voltooi de campagne.";
            case CleanRun: return "Voltooi de campagne zonder dood te gaan.";
            case ShardCollector: return "Verzamel 25 energiefragmenten in één run.";
            default: return "Prestatie ontgrendeld.";
        }
    }
}
