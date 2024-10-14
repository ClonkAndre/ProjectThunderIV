using System.Numerics;

using ProjectThunderIV.Extensions;

using IVSDKDotNet;

namespace ProjectThunderIV.Classes
{
    internal class ModSettings
    {
        #region Variables
        // General
        public static bool ReloadTimeCycleWhenModUnloads;
        public static bool AllowLightningBoltsInCutscene;

        // Networking
        public static bool EnableNetworkSync;

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
        public static bool DoNotRenderOutOfViewLightningBolts;

        // Light
        public static float LightIntensity;
        public static float LightRange;
        public static bool EnableLightShadow;
        public static bool DoNotRenderOutOfViewLights;

        // Blackout
        public static bool EnableBlackout;
        public static bool AllowBlackoutInMultiplayer;
        public static bool EnableAdditionalBlackoutDarkness;
        public static float AdditionalBlackoutDarkness;
        public static bool DecreaseCopsVisionOnActiveBlackout;
        public static bool PlayBlackoutSound;
        public static bool CanPlayBlackoutSoundWhenInInterior;
        public static float BlackoutChanceDay;
        public static float BlackoutChanceEvening;
        public static float BlackoutRangeAroundElectricalSubstation;
        public static int BlackoutActiveForMin;
        public static int BlackoutActiveForMax;

        // Sky
        public static float AdditionalCloudBrightness;

        // Danger
        public static bool EnableDangerousHeight;
        public static float DangerousHeight;
        public static float SpawnChanceWhenAboveDangerousHeight;

        public static bool EnableUmbrellaDanger;
        public static float SpawnChanceWhenHoldingUmbrella;

        // Reactions
        public static bool AllowPlayerReactions;
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
            ReloadTimeCycleWhenModUnloads = settings.GetBoolean("General", "ReloadTimeCycleWhenModUnloads", true);
            AllowLightningBoltsInCutscene = settings.GetBoolean("General", "AllowLightningBoltsInCutscene", true);

            // Networking
            EnableNetworkSync = settings.GetBoolean("Networking", "EnableNetworkSync", true);

            // Sound
            GlobalSoundMultiplier =                         settings.GetFloat("Sound", "GlobalMultiplier", 1.0f);
            VolumeIsAllowedToGoAboveOne =                   settings.GetBoolean("Sound", "VolumeIsAllowedToGoAboveOne", false);
            LowerVolumeByCertainAmountWhenInInterior =      settings.GetFloat("Sound", "LowerVolumeByCertainAmountWhenInInterior", 0.25f);
            SoundPackToUse =                                settings.GetValue("Sound", "UseSoundPack", "GTAIVSoundPack");

            // LightningBolt
            SpawnChancePercentageBeginning =        settings.GetFloat("LightningBolt", "SpawnChancePercentageBeginning", 6.0f);
            SpawnChancePercentageOngoing =          settings.GetFloat("LightningBolt", "SpawnChancePercentageOngoing", 10.0f);
            SpawnChancePercentageEnding =           settings.GetFloat("LightningBolt", "SpawnChancePercentageEnding", 4.0f);
            ScriptedSpawnChance =                   settings.GetFloat("LightningBolt", "ScriptedSpawnChance", 30.0f);
            BranchSpawnChance =                     settings.GetFloat("LightningBolt", "BranchSpawnChance", 10.0f);
            DelayThunderSoundMultiplier =           settings.GetFloat("LightningBolt", "DelaySoundMultiplier", 1.0f);
            MinBoltSize =                           settings.GetInteger("LightningBolt", "MinSize", 35);
            MaxBoltSize =                           settings.GetInteger("LightningBolt", "MaxSize", 500);
            MinBoltFadeOutSpeed =                   settings.GetFloat("LightningBolt", "MinFadeOutSpeed", 1.0f);
            MaxBoltFadeOutSpeed =                   settings.GetFloat("LightningBolt", "MaxFadeOutSpeed", 1.0f);
            BoltColor =                             settings.GetVector3("LightningBolt", "ColorRGB", new Vector3(0.766f, 0.9f, 1.0f)).Clamp(0.0f, 1.0f);
            CoronaSize =                            settings.GetFloat("LightningBolt", "CoronaSize", 1.0f);
            SpawnHeight =                           settings.GetFloat("LightningBolt", "Height", 1.0f);
            DoNotRenderOutOfViewLightningBolts =    settings.GetBoolean("LightningBolt", "DoNotRenderOutOfViewLightningBolts", true);

            // Light
            LightIntensity =                settings.GetFloat("Light", "Intensity", 0.11f);
            LightRange =                    settings.GetFloat("Light", "Range", 300f);
            EnableLightShadow =             settings.GetBoolean("Light", "EnableLightShadow", true);
            DoNotRenderOutOfViewLights =    settings.GetBoolean("Light", "DoNotRenderOutOfViewLights", false);

            // Blackout
            EnableBlackout =                            settings.GetBoolean("Blackout", "Enabled", true);
            AllowBlackoutInMultiplayer =                settings.GetBoolean("Blackout", "AllowInMultiplayer", true);

            EnableAdditionalBlackoutDarkness =          settings.GetBoolean("Blackout", "EnableAdditionalDarkness", true);
            AdditionalBlackoutDarkness =                settings.GetFloat("Blackout", "AdditionalDarkness", 0.3f);
            DecreaseCopsVisionOnActiveBlackout =        settings.GetBoolean("Blackout", "DecreaseCopsVisionOnActiveBlackout", true);

            PlayBlackoutSound =                         settings.GetBoolean("Blackout", "PlayBlackoutSound", true);
            CanPlayBlackoutSoundWhenInInterior =        settings.GetBoolean("Blackout", "CanPlaySoundWhenInInterior", false);

            BlackoutChanceDay =                         settings.GetFloat("Blackout", "ChanceDay", 5.0f);
            BlackoutChanceEvening =                     settings.GetFloat("Blackout", "ChanceEvening", 7.5f);
            BlackoutRangeAroundElectricalSubstation =   settings.GetFloat("Blackout", "RangeAroundElectricalSubstation", 30f);
            BlackoutActiveForMin =                      settings.GetInteger("Blackout", "MinActiveFor", 25);
            BlackoutActiveForMax =                      settings.GetInteger("Blackout", "MaxActiveFor", 80);

            // Sky
            AdditionalCloudBrightness = settings.GetFloat("Sky", "AdditionalCloudBrightness", 20f);

            // Danger
            EnableDangerousHeight =               settings.GetBoolean("Danger", "EnableDangerousHeight", true);
            DangerousHeight =                     settings.GetFloat("Danger", "DangerousHeight", 189f);
            SpawnChanceWhenAboveDangerousHeight = settings.GetFloat("Danger", "SpawnChanceWhenAboveDangerousHeight", 3f);

            EnableUmbrellaDanger =              settings.GetBoolean("Danger", "EnableUmbrellaDanger", true);
            SpawnChanceWhenHoldingUmbrella =    settings.GetFloat("Danger", "SpawnChanceWhenHoldingUmbrella", 0.1f);

            // Reactions
            AllowPlayerReactions =          settings.GetBoolean("Reactions", "AllowPlayerReactions", true);
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
