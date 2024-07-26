using System.Numerics;

using ProjectThunderIV.Extensions;

using IVSDKDotNet;

namespace ProjectThunderIV.Classes
{
    internal class ModSettings
    {
        #region Variables
        // General
        public static bool EnableDebug;
        public static bool ReloadTimeCycleWhenModUnloads;
        public static bool AllowLightningBoltsInCutscene;

        // Sound
        public static float GlobalSoundMultiplier;
        public static bool VolumeIsAllowedToGoAboveOne;
        public static float LowerVolumeByCertainAmountWhenInInterior;
        public static string SoundPackToUse;

        // LightningBolt
        public static float SpawnChancePercentageBeginning;
        public static float SpawnChancePercentageOngoing;
        public static float SpawnChancePercentageEnding;
        public static float ScriptedSpawnChance;
        public static float BranchSpawnChance;
        public static float DelayThunderSoundMultiplier;
        public static int MinBoltSize;
        public static int MaxBoltSize;
        public static float MinBoltFadeOutSpeed;
        public static float MaxBoltFadeOutSpeed;
        public static Vector3 BoltColor;
        public static float CoronaSize;
        public static float SpawnHeight;

        // Light
        public static float LightIntensity;
        public static float LightRange;
        public static bool EnableLightShadow;

        // Sky
        public static float AdditionalCloudBrightness;

        // Danger
        public static bool EnableDangerousHeight;
        public static float DangerousHeight;
        public static float SpawnChanceWhenAboveDangerousHeight;

        public static bool EnableUmbrellaDanger;
        public static float SpawnChanceWhenHoldingUmbrella;

        // Reactions
        public static bool AllowPedReactions;
        public static float ReactionChance;

        // Explosion
        public static bool CreateExplosions;
        public static int ExplosionType;
        public static float ExplosionRadius;
        public static float ExplosionCamShake;
        #endregion

        public static void Load(SettingsFile settings)
        {
            // General
#if DEBUG
            EnableDebug = true;
#else
            EnableDebug =                   settings.GetBoolean("General", "EnableDebug", false);
#endif
            ReloadTimeCycleWhenModUnloads = settings.GetBoolean("General", "ReloadTimeCycleWhenModUnloads", true);
            AllowLightningBoltsInCutscene = settings.GetBoolean("General", "AllowLightningBoltsInCutscene", true);

            // Sound
            GlobalSoundMultiplier =                         settings.GetFloat("Sound", "GlobalMultiplier", 1.0f);
            VolumeIsAllowedToGoAboveOne =                   settings.GetBoolean("Sound", "VolumeIsAllowedToGoAboveOne", false);
            LowerVolumeByCertainAmountWhenInInterior =      settings.GetFloat("Sound", "LowerVolumeByCertainAmountWhenInInterior", 0.25f);
            SoundPackToUse =                                settings.GetValue("Sound", "UseSoundPack", "GTAIVSoundPack");

            // LightningBolt
            SpawnChancePercentageBeginning =    settings.GetFloat("LightningBolt", "SpawnChancePercentageBeginning", 6.0f);
            SpawnChancePercentageOngoing =      settings.GetFloat("LightningBolt", "SpawnChancePercentageOngoing", 10.0f);
            SpawnChancePercentageEnding =       settings.GetFloat("LightningBolt", "SpawnChancePercentageEnding", 4.0f);
            ScriptedSpawnChance =               settings.GetFloat("LightningBolt", "ScriptedSpawnChance", 30.0f);
            BranchSpawnChance =                 settings.GetFloat("LightningBolt", "BranchSpawnChance", 10.0f);
            DelayThunderSoundMultiplier =       settings.GetFloat("LightningBolt", "DelaySoundMultiplier", 1.0f);
            MinBoltSize =                       settings.GetInteger("LightningBolt", "MinSize", 35);
            MaxBoltSize =                       settings.GetInteger("LightningBolt", "MaxSize", 500);
            MinBoltFadeOutSpeed =               settings.GetFloat("LightningBolt", "MinFadeOutSpeed", 1.0f);
            MaxBoltFadeOutSpeed =               settings.GetFloat("LightningBolt", "MaxFadeOutSpeed", 1.0f);
            BoltColor =                         settings.GetVector3("LightningBolt", "ColorRGB", new Vector3(0.766f, 0.9f, 1.0f)).Clamp(0.0f, 1.0f);
            CoronaSize =                        settings.GetFloat("LightningBolt", "CoronaSize", 1.0f);
            SpawnHeight =                       settings.GetFloat("LightningBolt", "Height", 1.0f);

            // Light
            LightIntensity =                settings.GetFloat("Light", "Intensity", 0.11f);
            LightRange =                    settings.GetFloat("Light", "Range", 300f);
            EnableLightShadow =             settings.GetBoolean("Light", "EnableLightShadow", true);

            // Sky
            AdditionalCloudBrightness =     settings.GetFloat("Sky", "AdditionalCloudBrightness", 20f);

            // Danger
            EnableDangerousHeight =               settings.GetBoolean("Danger", "EnableDangerousHeight", true);
            DangerousHeight =                     settings.GetFloat("Danger", "DangerousHeight", 189f);
            SpawnChanceWhenAboveDangerousHeight = settings.GetFloat("Danger", "SpawnChanceWhenAboveDangerousHeight", 3f);

            EnableUmbrellaDanger =              settings.GetBoolean("Danger", "EnableUmbrellaDanger", true);
            SpawnChanceWhenHoldingUmbrella =    settings.GetFloat("Danger", "SpawnChanceWhenHoldingUmbrella", 0.1f);

            // Reactions
            AllowPedReactions =             settings.GetBoolean("Reactions", "AllowPedReactions", true);
            ReactionChance =                settings.GetFloat("Reactions", "ReactionChance", 25f);

            // Explosion
            CreateExplosions =              settings.GetBoolean("Explosion", "CreateExplosions", true);
            ExplosionType =                 settings.GetInteger("Explosion", "Type", 2);
            ExplosionRadius =               settings.GetFloat("Explosion", "Radius", 15f);
            ExplosionCamShake =             settings.GetFloat("Explosion", "CamShake", 1.0f);
        }
    }
}
