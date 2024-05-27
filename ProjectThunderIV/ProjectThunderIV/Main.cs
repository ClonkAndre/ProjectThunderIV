using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;

using ManagedBass;
using Newtonsoft.Json;
using CCL.GTAIV;

using ProjectThunderIV.API;
using ProjectThunderIV.Classes;

using IVSDKDotNet;
using IVSDKDotNet.Enums;
using static IVSDKDotNet.Native.Natives;

namespace ProjectThunderIV
{
    public class Main : Script
    {
        #region Variables
        private List<DelayedCall> delayedCalls;
        private List<SoundConfiguration> soundConfigurations;
        private List<ScriptedLightning> scriptedLighting;
        private List<LightningBolt> lightningBolts;
        private List<SoundStream> currentSoundStreams;
        private Vector3 playerPos;

        public bool MenuOpen;

        // Test Thunder
        private bool debugKeysEnabled;
        private float distanceToCamera = 300f;
        private bool overrideThunderPos;
        private Vector3 testThunderPos = new Vector3(729.2668f, -765.0374f, 500f);

        // Settings
        private bool disableRandomLightningBolts;
        private bool disableBranches;
        private bool setLigtningBoltAlwaysGrowFromGroundUp;
        private bool forcePedsToReact;
        private bool forceUmbrellaCheckToAlwaysPass;
        private bool overrideFadeOutSpeed;
        private float newFadeOutSpeed = 1.0f;

        private float segmentSpacing = 0.2f;
        private float zOffset = 0.2f;

        // Lightning Debug
        private int lastLightningSize;
        private bool didLastLightningHitGround;
        private Vector3 lastLightningPosition;

        // Timecycle Cloud Stuff
        private IVTimeCycleParams lightningTimecycParams;

        private bool storedPreviousCloudsBrightness;
        private float previousCloudsBrightness;
        private Vector3 previousCloudsColor1;
        private Vector3 previousCloudsColor2;
        private Vector3 previousCloudsColor3;

        private float averageFadeOutSpeed = 1.0f;
        private bool wasAnythingCloudRelatedChanged;
        private bool hasFadingReachedTargetCloudValues;
        #endregion

        #region Methods
        private void ReloadSettings(string[] args)
        {
            if (Settings.Load())
            {
                ModSettings.Load(Settings);
                Logging.Log("Settings file of Project Thunder was reloaded!");
            }
            else
                Logging.LogWarning("Could not reload the settings file of Project Thunder! File might not exists.");
        }
        private void ReloadSoundPackCommand(string[] args)
        {
            LoadSoundConfigurations();
        }
        private void ReloadScriptedLightningCommand(string[] args)
        {
            LoadScriptedLightning();
        }
        private void SummonLightningboltCommand(string[] args)
        {
            UIntPtr playerPtr = IVPlayerInfo.FindThePlayerPed();

            if (playerPtr == UIntPtr.Zero)
                return;

            if (args.Length == 3)
            {
                bool res1 = float.TryParse(args[0], out float fX);
                bool res2 = float.TryParse(args[0], out float fY);
                bool res3 = float.TryParse(args[0], out float fZ);

                if (res1 && res2 && res3)
                {
                    SummonLightningbolt(new Vector3(fX, fY, fZ));
                    return;
                }
            }

            IVPed playerPed = IVPed.FromUIntPtr(playerPtr);
            SummonLightningbolt(playerPed.Matrix.Pos);
        }

        private void AddDelayedCall(double seconds, Action actionToExecute)
        {
            delayedCalls.Add(new DelayedCall(DateTime.UtcNow.AddSeconds(seconds), actionToExecute));
        }

        private void LoadSoundConfigurations()
        {
            try
            {
                string fileName = string.Format("{0}\\{1}.json", ScriptResourceFolder, ModSettings.SoundPackToUse);

                if (!File.Exists(fileName))
                {
                    Logging.LogWarning("Could not find sound pack configuration file {0}! There will be no sound with thunder.", ModSettings.SoundPackToUse);
                    return;
                }

                if (soundConfigurations != null)
                    soundConfigurations.Clear();

                soundConfigurations = JsonConvert.DeserializeObject<List<SoundConfiguration>>(File.ReadAllText(fileName));

                if (soundConfigurations.Count == 0)
                    Logging.LogWarning("Loaded 0 sound configurations! There will be no sound with thunder.");
                else
                    Logging.Log("Loaded {0} sound configurations!", soundConfigurations.Count);

            }
            catch (Exception ex)
            {
                Logging.LogError("An error occured while trying to load sound configurations! There will be no sound with thunder. Details: {0}", ex);
            }
        }
        private void LoadScriptedLightning()
        {
            try
            {
                string fileName = string.Format("{0}\\ScriptedLightning.json", ScriptResourceFolder);

                if (!File.Exists(fileName))
                    return;

                scriptedLighting.Clear();
                scriptedLighting = JsonConvert.DeserializeObject<List<ScriptedLightning>>(File.ReadAllText(fileName));
                Logging.Log("Loaded {0} scripted lightnings!", scriptedLighting.Count);
            }
            catch (Exception ex)
            {
                Logging.LogError("An error occured while trying to load scripted lightning! Details: {0}", ex);
            }
        }
        private void SaveScriptedLightning()
        {
            try
            {
                string fileName = string.Format("{0}\\ScriptedLightning.json", ScriptResourceFolder);
                File.WriteAllText(fileName, JsonConvert.SerializeObject(scriptedLighting, Formatting.Indented));
                Logging.Log("Saved {0} scripted lightnings!", scriptedLighting.Count);
            }
            catch (Exception ex)
            {
                Logging.LogError("An error occured while trying to save scripted lightning! Details: {0}", ex);
            }
        }

        private void PrepareForLightnigboltSummoning(ThunderstormProgress progress)
        {
            // Check if can spawn any lightning bolt at all
            float rnd = GENERATE_RANDOM_FLOAT_IN_RANGE(0.0f, 1.0f) * 100.0f;

            switch (progress)
            {
                case ThunderstormProgress.Starting:

                    if (rnd > ModSettings.SpawnChancePercentageBeginning)
                        return;

                    break;
                case ThunderstormProgress.Ongoing:

                    if (rnd > ModSettings.SpawnChancePercentageOngoing)
                        return;

                    break;
                case ThunderstormProgress.Ending:

                    if (rnd > ModSettings.SpawnChancePercentageEnding)
                        return;

                    break;
            }

            // Check if can spawn a scripted lightning bolt
            if ((GENERATE_RANDOM_FLOAT_IN_RANGE(0.0f, 1.0f) * 100.0f) < ModSettings.ScriptedSpawnChance)
            {
                for (int i = 0; i < scriptedLighting.Count; i++)
                {
                    ScriptedLightning item = scriptedLighting[i];

                    // Generate new random number for spawn chance
                    rnd = GENERATE_RANDOM_FLOAT_IN_RANGE(0.0f, 1.0f) * 100.0f;

                    // If spawn should be forced, ignore the trigger type and the spawn chance
                    if (item.ForceSpawn)
                    {
                        // Spawn lightning bolt forced
                        SummonLightningbolt(item.SpawnPosition.Around(GENERATE_RANDOM_FLOAT_IN_RANGE(item.SpawnRadiusMin, item.SpawnRadiusMax)),
                            item.CanHaveBranches,
                            item.LightningGrowsFromBottomToTop,
                            item.OverrideLightningSize,
                            item.OverrideLightningColor,
                            item.OverrideSkyColor,
                            item.OverrideSkyBrightness);

                        item.ForceSpawn = false;
                        return;
                    }

                    switch ((TriggerType)item.TheTrigger)
                    {
                        case TriggerType.LookAt:

                            // Check if the position is visible
                            if (!NativeWorld.IsPositionVisibleOnScreen(item.SpawnPosition))
                                continue;

                            // Check if can spawn lightning bolt there
                            if (!(rnd <= item.SpawnChance))
                                continue;

                            // Spawn lightning bolt
                            SummonLightningbolt(item.SpawnPosition.Around(GENERATE_RANDOM_FLOAT_IN_RANGE(item.SpawnRadiusMin, item.SpawnRadiusMax)),
                                item.CanHaveBranches,
                                item.LightningGrowsFromBottomToTop,
                                item.OverrideLightningSize,
                                item.OverrideLightningColor,
                                item.OverrideSkyColor,
                                item.OverrideSkyBrightness);

                            return;
                        case TriggerType.BeAt:

                            // Check if player is near the trigger position
                            if (!(Vector3.Distance(playerPos, item.TriggerPos) < item.TriggerDistance))
                                continue;

                            // Check if can spawn lightning bolt there
                            if (!(rnd <= item.SpawnChance))
                                continue;

                            // Spawn lightning bolt
                            SummonLightningbolt(item.SpawnPosition.Around(GENERATE_RANDOM_FLOAT_IN_RANGE(item.SpawnRadiusMin, item.SpawnRadiusMax)),
                                item.CanHaveBranches,
                                item.LightningGrowsFromBottomToTop,
                                item.OverrideLightningSize,
                                item.OverrideLightningColor,
                                item.OverrideSkyColor,
                                item.OverrideSkyBrightness);

                            return;
                    }
                }
            }

            // If player is above a certain point, there is a chance the lightning bolt will spawn near (or even at) their position
            if (IsPlayerAboveDangerousHeight())
            {
                rnd = GENERATE_RANDOM_FLOAT_IN_RANGE(0.0f, 1.0f) * 100.0f;

                if (rnd < ModSettings.SpawnChanceWhenAboveDangerousHeight)
                    SummonLightningbolt(overrideThunderPos ? testThunderPos : playerPos.Around(GENERATE_RANDOM_FLOAT_IN_RANGE(0f, 200f)), setLigtningBoltAlwaysGrowFromGroundUp);
                else
                    SummonLightningbolt(overrideThunderPos ? testThunderPos : GetRandomPositionInWorld(), setLigtningBoltAlwaysGrowFromGroundUp);
            }
            else
            {
                SummonLightningbolt(overrideThunderPos ? testThunderPos : GetRandomPositionInWorld(), setLigtningBoltAlwaysGrowFromGroundUp);
            }
        }
        private void SummonLightningbolt(Vector3 spawnPosition, bool canHaveBranches = true, bool growFromGroundUp = false, int overrideLightningSize = -1, Vector3 overrideBoltColor = default(Vector3), Vector3 overrideSkyColor = default(Vector3), float overrideSkyBrightness = -1f, bool wasCalledFromAnotherScript = false)
        {
            // If there is a lightning bolt added already, reset its size to default to make it look like this lightning bolt got new power
            if (!wasCalledFromAnotherScript)
            {
                if (GENERATE_RANDOM_FLOAT_IN_RANGE(0.0f, 1.0f) > 0.5f)
                {
                    LightningBolt existingLightningBolt = lightningBolts.FirstOrDefault();
                    if (existingLightningBolt != null)
                    {
                        existingLightningBolt.StartingCoronaSize += GENERATE_RANDOM_FLOAT_IN_RANGE(300f, 500f);
                        existingLightningBolt.ResetSize();
                        return;
                    }
                }
            }

            // Get random lightning bolt size
            lastLightningSize = GENERATE_RANDOM_INT_IN_RANGE(ModSettings.MinBoltSize, ModSettings.MaxBoltSize);

            if (!(overrideLightningSize <= -1))
                lastLightningSize = overrideLightningSize;

            // Get random lightning bolt fade out speed
            float fadeOutSpeed = GENERATE_RANDOM_FLOAT_IN_RANGE(ModSettings.MinBoltFadeOutSpeed, ModSettings.MaxBoltFadeOutSpeed);

            // Create new lightning bolt instance
            LightningBolt lightningBolt = new LightningBolt(lastLightningSize, ModSettings.CoronaSize, fadeOutSpeed);

            if (canHaveBranches && !disableBranches)
                lightningBolt.Branches = new List<LightningBoltBranch>();

            if (overrideBoltColor != Vector3.Zero)
                lightningBolt.OverrideBoltColor = overrideBoltColor;
            if (overrideSkyColor != Vector3.Zero)
                lightningBolt.OverrideSkyColor = overrideSkyColor;
            if (!(overrideSkyBrightness <= -1f))
                lightningBolt.OverrideSkyBrightness = overrideSkyBrightness;

            // [DEBUG] Log last lightning pos
            lastLightningPosition = spawnPosition;

            // Find peds with umbrellas and maybe spawn the lightning bolt there
            if (!wasCalledFromAnotherScript && ModSettings.EnableUmbrellaDanger)
            {
                uint umbrellaModel1 = RAGE.AtStringHash("ec_char_brollie");
                uint umbrellaModel2 = RAGE.AtStringHash("ec_char_brollie02");
                uint umbrellaModel3 = RAGE.AtStringHash("ec_char_brollie03");

                IVPool pedPool = IVPools.GetPedPool();
                for (int i = 0; i < pedPool.Count; i++)
                {
                    UIntPtr ptr = pedPool.Get(i);

                    if (ptr == UIntPtr.Zero
                        || ptr == IVPlayerInfo.FindThePlayerPed())
                        continue;

                    if ((GENERATE_RANDOM_FLOAT_IN_RANGE(0f, 1f) * 100.0f) > ModSettings.SpawnChanceWhenHoldingUmbrella && !forceUmbrellaCheckToAlwaysPass)
                        continue;

                    int handle = (int)pedPool.GetIndex(ptr);

                    if (IS_CHAR_DEAD(handle))
                        continue;
                    if (IS_PED_A_MISSION_PED(handle))
                        continue;

                    GET_INTERIOR_FROM_CHAR(handle, out int interior);

                    if (interior != 0)
                        continue;

                    if (!IS_PED_HOLDING_AN_OBJECT(handle))
                        continue;

                    int objHandle = GET_OBJECT_PED_IS_HOLDING(handle);

                    if (objHandle == 0)
                        continue;

                    GET_OBJECT_MODEL(objHandle, out uint objModel);

                    if (objModel == umbrellaModel1
                        || objModel == umbrellaModel2
                        || objModel == umbrellaModel3)
                    {
                        GET_CHAR_COORDINATES(handle, out spawnPosition);
                        growFromGroundUp = true;
                    }
                }
            }

            // /summon lightning_bolt!
            for (int i = 0; i < lastLightningSize; i++)
            {
                if (i == 0)
                {
                    // Set the starting point of the lightning bolt
                    lightningBolt.Points[i] = spawnPosition;
                }
                else
                {
                    // Get the previous point and grow from there
                    Vector3 previousPoint = lightningBolt.Points[i - 1];

                    // Randomize bolt
                    if (growFromGroundUp)
                        previousPoint = new Vector3(previousPoint.X + i * GENERATE_RANDOM_FLOAT_IN_RANGE(-segmentSpacing, segmentSpacing), previousPoint.Y + i * GENERATE_RANDOM_FLOAT_IN_RANGE(-segmentSpacing, segmentSpacing), previousPoint.Z + i * zOffset);
                    else
                        previousPoint = new Vector3(previousPoint.X + i * GENERATE_RANDOM_FLOAT_IN_RANGE(-segmentSpacing, segmentSpacing), previousPoint.Y + i * GENERATE_RANDOM_FLOAT_IN_RANGE(-segmentSpacing, segmentSpacing), previousPoint.Z - i * zOffset);

                    // Check if point is below ground
                    float groundZ = NativeWorld.GetGroundZ(previousPoint, GroundType.Highest);
                    float distanceFromLastPointToGround = Vector3.Distance(previousPoint, new Vector3(previousPoint.X, previousPoint.Y, groundZ));

                    // If lightning bolt should grow up then always create an explosion at the spawn point of the bolt if allowed
                    if (growFromGroundUp)
                    {
                        if (ModSettings.CreateExplosions)
                            NativeWorld.AddExplosion(spawnPosition, (eExplosion)ModSettings.ExplosionType, ModSettings.ExplosionRadius, true, false, ModSettings.ExplosionCamShake);
                    }
                    else
                    {
                        // If lightning bolt is below ground, stop growing, and spawn explosion at pos if allowed to
                        if (previousPoint.Z <= groundZ)
                        {

                            // Get the ground position at the last lightning bolt point and create explosion there if allowed
                            if (ModSettings.CreateExplosions)
                            {
                                Vector3 groundPos = NativeWorld.GetGroundPosition(previousPoint, GroundType.Highest);
                                NativeWorld.AddExplosion(groundPos, (eExplosion)ModSettings.ExplosionType, ModSettings.ExplosionRadius, true, false, ModSettings.ExplosionCamShake);
                            }

                            didLastLightningHitGround = true;

                            break;
                        }
                        else
                        {
                            didLastLightningHitGround = false;
                        }
                    }

                    // Create new branch if allowed to
                    if (lightningBolt.Branches != null)
                    {
                        if (distanceFromLastPointToGround > 150f
                            && (GENERATE_RANDOM_FLOAT_IN_RANGE(0.0f, 1.0f) * 100.0f) < ModSettings.BranchSpawnChance)
                        {
                            // Create new branch
                            LightningBoltBranch branch = new LightningBoltBranch(GENERATE_RANDOM_INT_IN_RANGE(20, 40) + 1);

                            // Set branch starting position
                            branch.Points[0] = previousPoint;

                            // Add branch
                            lightningBolt.Branches.Add(branch);
                        }
                    }

                    // Set new point
                    lightningBolt.Points[i] = previousPoint;
                }
            }

            // Build Branches
            BuildBranches(lightningBolt);

            // Add new lightning bolt to list of lightning bolts
            lightningBolts.Add(lightningBolt);

            // Play thunder sound from the middle of the lightning bolt
            PlayThunderSound(lightningBolt, spawnPosition);
        }
        private void BuildBranches(LightningBolt lightningBolt)
        {
            if (lightningBolt.Branches == null)
                return;

            for (int i = 0; i < lightningBolt.Branches.Count; i++)
            {
                LightningBoltBranch branch = lightningBolt.Branches[i];

                for (int p = 1; p < branch.Points.Length; p++)
                {
                    // Get the previous point and grow from there
                    Vector3 previousPoint = branch.Points[p - 1];

                    previousPoint = new Vector3(
                        previousPoint.X + p * branch.XValue + GENERATE_RANDOM_FLOAT_IN_RANGE(branch.XValue < 0f ? branch.XValue - 0.3f : branch.XValue + 0.3f, branch.XValue + 0.5f),
                        previousPoint.Y + p * branch.YValue + GENERATE_RANDOM_FLOAT_IN_RANGE(branch.YValue < 0f ? branch.YValue - 0.3f : branch.YValue + 0.3f, branch.YValue + 0.5f),
                        previousPoint.Z + p * GENERATE_RANDOM_FLOAT_IN_RANGE(-0.5f, 0.5f));

                    branch.Points[p] = previousPoint;
                }
            }
        }
        private void PlayThunderSound(LightningBolt lightningBolt, Vector3 spawnedPosition)
        {
            // Create random thunder sound stream
            int createdSoundStream = CreateRandomThunderSoundStream(out float volumeBoostValueFromConfig);

            if (createdSoundStream != 0)
            {
                // Calculate sound delay based on distance from the player position to the creation point of the lightning bolt 
                double secondsDelay = TimeSpan.FromMilliseconds(Vector3.Distance(playerPos, spawnedPosition)).TotalSeconds * ModSettings.DelayThunderSoundMultiplier;

                // Get starting point of lightning bolt for sound
                Vector3 firstPoint = lightningBolt.Points[0];

                // Create action that plays the thunder sound
                Action actionToExecute = () =>
                {
                    // Create sound stream
                    SoundStream stream = new SoundStream(createdSoundStream, volumeBoostValueFromConfig);
                    stream.CalculateTargetVolume(IVMenuManager.GetSetting(eSettings.SETTING_SFX_LEVEL), IS_INTERIOR_SCENE());
                    stream.ApplyVolume();
                    stream.Set3DPosition(firstPoint);
                    stream.Play();
                    currentSoundStreams.Add(stream);

                    // Loop through all peds in the world and maybe make them react to the thunder sound if allowed
                    if (ModSettings.AllowPedReactions)
                    {
                        // Request shocking event animations
                        REQUEST_ANIMS("amb@shock_events");

                        IVPool pedPool = IVPools.GetPedPool();
                        for (int i = 0; i < pedPool.Count; i++)
                        {
                            UIntPtr ptr = pedPool.Get(i);

                            if (ptr == UIntPtr.Zero
                                || ptr == IVPlayerInfo.FindThePlayerPed())
                                continue;

                            if ((GENERATE_RANDOM_FLOAT_IN_RANGE(0f, 1f) * 100.0f) > ModSettings.ReactionChance && !forcePedsToReact)
                                continue;

                            IVPed ped = IVPed.FromUIntPtr(ptr);
                            int handle = (int)pedPool.GetIndex(ptr);

                            if (IS_PED_A_MISSION_PED(handle))
                                continue;

                            // React to thunder
                            if (GENERATE_RANDOM_INT_IN_RANGE(0, 100) < 40)
                                ped.SayAmbientSpeech(GENERATE_RANDOM_INT_IN_RANGE(0, 100) < 50 ? "SURPRISED" : "SHIT"); 

                            if (GENERATE_RANDOM_INT_IN_RANGE(0, 100) < 50)
                                ped.GetTaskController().LookAt(stream.Position, (uint)GENERATE_RANDOM_INT_IN_RANGE(2000, 4000));
                            else
                                ped.GetAnimationController().Play("amb@shock_events", "look_over_shoulder", 3f, AnimationFlags.PlayInUpperBodyWithWalk);
                        }
                    }
                };

                // If seconds delay is under 1 second then play sound instantly. Otherwise, delay sound
                if (secondsDelay < 1d)
                {
                    // Just add it as a delayed call too but invoke it instantly because this can crash the game when called from the rendering thread and it's trying to request the "amb@shock_events" animations.
                    AddDelayedCall(0d, actionToExecute);
                }
                else
                {
                    // This will delay the sound of the thunder depending on the distance to the player
                    AddDelayedCall(secondsDelay, actionToExecute);
                }
            }
        }
        #endregion

        #region Functions
        private static float Lerp(float a, float b, float t)
        {
            // Clamp t between 0 and 1
            t = Math.Max(0.0f, Math.Min(1.0f, t));

            return a + (b - a) * t;
        }

        private Vector3 GetRandomPositionInWorld()
        {
            float x = GENERATE_RANDOM_FLOAT_IN_RANGE(-2687.735f, 2427.867f); // Upper-Left Corner of the Map
            float y = GENERATE_RANDOM_FLOAT_IN_RANGE(3249.545f, -1381.632f); // Lower-Right Corner of the Map
            return new Vector3(x, y, ModSettings.SpawnHeight);
        }
        private bool IsPlayerAboveDangerousHeight()
        {
            return playerPos.Z > ModSettings.DangerousHeight && ModSettings.EnableDangerousHeight;
        }
        private int CreateRandomThunderSoundStream(out float volumeBoostValueFromConfig)
        {
            if (soundConfigurations.Count == 0)
            {
                volumeBoostValueFromConfig = 0f;
                return 0;
            }

            // Get random sound configuration
            SoundConfiguration soundConfiguration = soundConfigurations[GENERATE_RANDOM_INT_IN_RANGE(0, soundConfigurations.Count - 1)];

            if (string.IsNullOrWhiteSpace(soundConfiguration.FileName))
            {
                Logging.LogWarning("Could not create new thunder sound stream! Details: There was a sound configuration with an invalid file name!");
                volumeBoostValueFromConfig = 0f;
                return 0;
            }

            string fileName = string.Format("{0}\\{1}", ScriptResourceFolder, soundConfiguration.FileName);

            if (File.Exists(fileName))
            {
                volumeBoostValueFromConfig = soundConfiguration.VolumeBoost;
                return Bass.CreateStream(fileName, 0, 0, BassFlags.Bass3D);
            }
            else
            {
                Logging.LogWarning("Could not create new thunder sound stream! Details: The file {0} does not exists!", soundConfiguration.FileName);
                Logging.LogDebug("Full Path: {0}", fileName);
            }

            volumeBoostValueFromConfig = 0f;
            return 0;
        }
        #endregion

        #region Constructor
        public Main()
        {
            delayedCalls = new List<DelayedCall>();
            soundConfigurations = new List<SoundConfiguration>();
            scriptedLighting = new List<ScriptedLightning>();
            lightningBolts = new List<LightningBolt>(6); // Initialize with starting size 6
            currentSoundStreams = new List<SoundStream>(6); // Initialize with starting size 6

            WaitTickInterval = 1250;

            RAGE.OnWindowFocusChanged += RAGE_OnWindowFocusChanged;
            Uninitialize += Main_Uninitialize;
            Initialized += Main_Initialized;
            ScriptCommandReceived += Main_ScriptCommandReceived;
            OnImGuiRendering += Main_OnImGuiRendering;
            Tick += Main_Tick;
            WaitTick += Main_WaitTick;
        }
        #endregion

        private void RAGE_OnWindowFocusChanged(bool focused)
        {
            if (focused)
            {
                // Resume sounds if not in pause menu
                if (IS_PAUSE_MENU_ACTIVE())
                    return;

                for (int i = 0; i < currentSoundStreams.Count; i++)
                {
                    SoundStream stream = currentSoundStreams[i];

                    if (stream.GetState() == PlaybackState.Paused)
                        stream.Play();
                }
            }
            else
            {
                // Pause sounds
                for (int i = 0; i < currentSoundStreams.Count; i++)
                {
                    SoundStream stream = currentSoundStreams[i];

                    if (stream.GetState() == PlaybackState.Playing)
                        stream.Pause();
                }
            }
        }

        private void Main_Uninitialize(object sender, EventArgs e)
        {
            RAGE.OnWindowFocusChanged -= RAGE_OnWindowFocusChanged;

            Bass.Free();
            currentSoundStreams.Clear();
            currentSoundStreams = null;
            soundConfigurations.Clear();
            soundConfigurations = null;
            lightningBolts.Clear();
            lightningBolts = null;

            // Reload timecycle if allowed
            if (ModSettings.ReloadTimeCycleWhenModUnloads)
                IVTimeCycle.Initialise();
        }
        private void Main_Initialized(object sender, EventArgs e)
        {
            // Add console command
            RegisterConsoleCommand("pt_reloadsettings", ReloadSettings);
            RegisterConsoleCommand("pt_reloadsoundpack", ReloadSoundPackCommand);
            RegisterConsoleCommand("pt_reloadscriptedlightning", ReloadScriptedLightningCommand);
            RegisterConsoleCommand("pt_summon_lightning_bolt", SummonLightningboltCommand);

            // Initialize Bass Library
            if (!Bass.Init(-1, 44100, DeviceInitFlags.Mono | DeviceInitFlags.Device3D))
            {
                if (Bass.LastError == Errors.Already)
                    Logging.LogWarning("Failed to initialize Bass library! The Bass library was already initialized by another mod and might be missconfigured.");
                else
                    Logging.LogWarning("Failed to initialize Bass library! There might be no sound with thunder. Details: {0} (Code: {1})", Bass.LastError, (int)Bass.LastError);
            }

            // Load settings
            ModSettings.Load(Settings);

            // Load sound configurations
            LoadSoundConfigurations();

            // Load Scripted Thunder
            LoadScriptedLightning();
        }

        private object Main_ScriptCommandReceived(Script fromScript, object[] args, string command)
        {
            switch (command.ToLower())
            {
                case "summon_lightning_bolt":
                    {
                        if (args == null)
                            return false;
                        if (args.Length == 0)
                            return false;

                        try
                        {
                            if (args[0].GetType() == typeof(string))
                            {
                                // Try creating config object from given args for lightning bolt
                                LightningBoltSummonConfig config = JsonConvert.DeserializeObject<LightningBoltSummonConfig>(args[0].ToString());

                                // Summon lightning bolt
                                SummonLightningbolt(config.SpawnPosition,
                                    config.CanHaveBranches,
                                    config.GrowFromGroundUp,
                                    config.OverrideLightningSize,
                                    config.OverrideBoltColor,
                                    config.OverrideSkyColor,
                                    config.OverrideSkyBrightness,
                                    true);

                                return true;
                            }
                            else
                            {
                                if (args.Length == 1)
                                {
                                    // Summon lightning bolt
                                    SummonLightningbolt((Vector3)args[0],
                                        true,
                                        false,
                                        -1,
                                        default(Vector3),
                                        default(Vector3),
                                        -1f,
                                        true);

                                    return true;
                                }
                                else if (args.Length == 7)
                                {
                                    // Summon lightning bolt
                                    SummonLightningbolt((Vector3)args[0],
                                        (bool)args[1],
                                        (bool)args[2],
                                        (int)args[3],
                                        (Vector3)args[4],
                                        (Vector3)args[5],
                                        (float)args[6],
                                        true);

                                    return true;
                                }
                                else
                                {
                                    Logging.LogDebug("Unknown length for command {0}!", command);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logging.LogError("Command {0} sent from script {1} failed! Details: {2}", command, fromScript == null ? "UNKNOWN" : fromScript.GetName(), ex);
                        }
                    }
                    return false;

                default:
                    return false;
            }
        }

        private void Main_OnImGuiRendering(IntPtr devicePtr, ImGuiIV_DrawingContext ctx)
        {
            if (!MenuOpen)
                return;

            ImGuiIV.Begin("Project Thunder", ref MenuOpen, eImGuiWindowFlags.None);

            if (ImGuiIV.BeginTabBar("##ProjectThunderMainTabBar"))
            {

                DebugTabItem();
                SettingsTabItem();
                ScriptedLightningTabItem();

                ImGuiIV.EndTabBar();
            }

            ImGuiIV.End();
        }
        private void DebugTabItem()
        {
            if (ModSettings.EnableDebug)
            {
                if (ImGuiIV.BeginTabItem("Debug"))
                {
                    ImGuiIV.SeparatorText("Lightning Debug");
                    ImGuiIV.Text("Is player above dangerous height: {0}", IsPlayerAboveDangerousHeight());
                    ImGuiIV.Text("Last lightning size: {0}", lastLightningSize);
                    ImGuiIV.Text("Did Last Lightning Bolt Hit Ground: {0}", didLastLightningHitGround);
                    ImGuiIV.Spacing();
                    ImGuiIV.Text("Last Lightning Bolt Position: {0}", lastLightningPosition);
                    float lastThunderDistance = Vector3.Distance(playerPos, lastLightningPosition);
                    ImGuiIV.Text("Distance from player: {0} ({1} Seconds)", lastThunderDistance, TimeSpan.FromMilliseconds(lastThunderDistance).TotalSeconds * 0.5d);

                    ImGuiIV.Spacing();

                    if (ImGuiIV.Button("Summon Lightning Bolt"))
                        SummonLightningbolt(overrideThunderPos ? testThunderPos : GetRandomPositionInWorld(), true, setLigtningBoltAlwaysGrowFromGroundUp);

                    ImGuiIV.HelpMarker("Enables some debug keys. Press 'L' to summon a lightning bolt infront of the camera.");
                    ImGuiIV.SameLine();
                    ImGuiIV.CheckBox("Debug Keys Enabled", ref debugKeysEnabled);
                    ImGuiIV.DragFloat("Debug summon distance to camera", ref distanceToCamera);

                    ImGuiIV.CheckBox("Disable Random Lightning Bolts", ref disableRandomLightningBolts);
                    ImGuiIV.CheckBox("Disable Branches", ref disableBranches);
                    ImGuiIV.CheckBox("Force Peds To React", ref forcePedsToReact);
                    ImGuiIV.CheckBox("Force Umbrella Check To Always Pass", ref forceUmbrellaCheckToAlwaysPass);
                    ImGuiIV.CheckBox("Set Lightning Bolt Always Grow Up", ref setLigtningBoltAlwaysGrowFromGroundUp);
                    ImGuiIV.CheckBox("Override Position", ref overrideThunderPos);
                    if (ImGuiIV.Button("Set to player pos"))
                        testThunderPos = playerPos;
                    ImGuiIV.SameLine();
                    ImGuiIV.DragFloat3("New Position", ref testThunderPos);
                    ImGuiIV.CheckBox("Override Fade Out Speed", ref overrideFadeOutSpeed);
                    ImGuiIV.DragFloat("New Fade Out Speed", ref newFadeOutSpeed, 0.01f);

                    ImGuiIV.Spacing();
                    ImGuiIV.SeparatorText("Weather Debug");
                    ImGuiIV.Text("ForcedWeatherType: {0}", (eWeather)IVWeather.ForcedWeatherType);
                    ImGuiIV.Text("OldWeatherType: {0}", (eWeather)IVWeather.OldWeatherType);
                    ImGuiIV.Text("NewWeatherType: {0}", (eWeather)IVWeather.NewWeatherType);
                    ImGuiIV.Text("InterpolationValue: {0}", IVWeather.InterpolationValue);
                    ImGuiIV.Text("Rain: {0}", IVWeather.Rain);

                    ImGuiIV.Spacing();
                    if (ImGuiIV.Button("Force Lightning Weather"))
                        FORCE_WEATHER((uint)eWeather.WEATHER_LIGHTNING);
                    ImGuiIV.SameLine();
                    if (ImGuiIV.Button("Force Lightning Weather Now"))
                        FORCE_WEATHER_NOW((uint)eWeather.WEATHER_LIGHTNING);
                    ImGuiIV.SameLine();
                    if (ImGuiIV.Button("Reset forced weather"))
                        IVWeather.ForcedWeatherType = -1;

                    if (ImGuiIV.Button("Set time to night"))
                        SET_TIME_OF_DAY(0, 0);

                    ImGuiIV.Spacing();
                    ImGuiIV.SeparatorText("Timecycle Debug");

                    if (lightningTimecycParams != null)
                    {
                        ImGuiIV.BeginDisabled();
                        float mCloudsBrightness = lightningTimecycParams.CloudsBrightness; ImGuiIV.DragFloat("Current Cloud Brightness", ref mCloudsBrightness);
                        Vector3 mCloud1Color = lightningTimecycParams.Cloud1Color; ImGuiIV.DragFloat3("Current Cloud 1 Color", ref mCloud1Color);
                        Vector3 mCloud2Color = lightningTimecycParams.Cloud2Color; ImGuiIV.DragFloat3("Current Cloud 2 Color", ref mCloud2Color);
                        Vector3 mCloud3Color = lightningTimecycParams.Cloud3Color; ImGuiIV.DragFloat3("Current Cloud 3 Color", ref mCloud3Color);
                        ImGuiIV.DragFloat("Previous Cloud Brightness", ref previousCloudsBrightness);
                        ImGuiIV.DragFloat("Average Fade Out Speed", ref averageFadeOutSpeed);
                        ImGuiIV.EndDisabled();
                    }

                    ImGuiIV.Spacing();
                    ImGuiIV.SeparatorText("Sound Debug");
                    ImGuiIV.Text("Currently active sounds: {0}", currentSoundStreams.Count);

                    for (int i = 0; i < currentSoundStreams.Count; i++)
                    {
                        SoundStream stream = currentSoundStreams[i];

                        if (ImGuiIV.Button("Stop"))
                            stream.Stop();

                        ImGuiIV.SameLine();
                        ImGuiIV.Text("Handle: {0}, State: {1}, Volume: {2}", stream.Handle, stream.GetState(), stream.TargetVolume);
                    }

                    ImGuiIV.EndTabItem();
                }
            }
        }
        private void SettingsTabItem()
        {
            if (ImGuiIV.BeginTabItem("Settings"))
            {

                if (ImGuiIV.Button("Reload Settings"))
                    ReloadSettings(null);
                ImGuiIV.SameLine();
                if (ImGuiIV.Button("Reload Soundpack"))
                    ReloadSoundPackCommand(null);

                ImGuiIV.Spacing();
                ImGuiIV.SeparatorText("The Settings");

                ImGuiIV.Text("General");
                ImGuiIV.CheckBox("EnableDebug", ref ModSettings.EnableDebug);
                ImGuiIV.CheckBox("ReloadTimeCycleWhenModUnloads", ref ModSettings.ReloadTimeCycleWhenModUnloads);
                ImGuiIV.CheckBox("AllowLightningBoltsInCutscene", ref ModSettings.AllowLightningBoltsInCutscene);

                ImGuiIV.Spacing();
                ImGuiIV.Text("Sound");
                ImGuiIV.DragFloat("GlobalSoundMultiplier", ref ModSettings.GlobalSoundMultiplier, 0.01f);
                ImGuiIV.CheckBox("VolumeIsAllowedToGoAboveOne", ref ModSettings.VolumeIsAllowedToGoAboveOne);
                ImGuiIV.DragFloat("LowerVolumeByCertainAmountWhenInInterior", ref ModSettings.LowerVolumeByCertainAmountWhenInInterior, 0.01f);
                ImGuiIV.InputText("SoundPackToUse", ref ModSettings.SoundPackToUse);

                ImGuiIV.Spacing();
                ImGuiIV.Text("Lightning Bolt");
                ImGuiIV.DragFloat("SpawnChancePercentageBeginning", ref ModSettings.SpawnChancePercentageBeginning, 0.01f);
                ImGuiIV.DragFloat("SpawnChancePercentageOngoing", ref ModSettings.SpawnChancePercentageOngoing, 0.01f);
                ImGuiIV.DragFloat("SpawnChancePercentageEnding", ref ModSettings.SpawnChancePercentageEnding, 0.01f);
                ImGuiIV.DragFloat("ScriptedSpawnChance", ref ModSettings.ScriptedSpawnChance, 0.01f);
                ImGuiIV.DragFloat("BranchSpawnChance", ref ModSettings.BranchSpawnChance, 0.01f);
                ImGuiIV.DragFloat("DelayThunderSoundMulitplier", ref ModSettings.DelayThunderSoundMultiplier, 0.01f);
                ImGuiIV.DragInt("MinBoltSize", ref ModSettings.MinBoltSize);
                ImGuiIV.DragInt("MaxBoltSize", ref ModSettings.MaxBoltSize);
                ImGuiIV.DragFloat("MinBoltFadeOutSpeed", ref ModSettings.MinBoltFadeOutSpeed, 0.01f);
                ImGuiIV.DragFloat("MaxBoltFadeOutSpeed", ref ModSettings.MaxBoltFadeOutSpeed, 0.01f);
                ImGuiIV.ColorEdit3("BoltColor", ref ModSettings.BoltColor, eImGuiColorEditFlags.Float);
                ImGuiIV.DragFloat("CoronaSize", ref ModSettings.CoronaSize, 0.01f);
                ImGuiIV.DragFloat("SpawnHeight", ref ModSettings.SpawnHeight, 0.01f);

                ImGuiIV.Spacing();
                ImGuiIV.Text("Light");
                ImGuiIV.DragFloat("LightIntensity", ref ModSettings.LightIntensity, 0.001f);
                ImGuiIV.DragFloat("LightRange", ref ModSettings.LightRange, 0.1f);

                ImGuiIV.Spacing();
                ImGuiIV.Text("Sky");
                ImGuiIV.DragFloat("AdditionalCloudBrightness", ref ModSettings.AdditionalCloudBrightness, 0.01f);

                ImGuiIV.Spacing();
                ImGuiIV.Text("Danger");
                ImGuiIV.CheckBox("EnableDangerousHeight", ref ModSettings.EnableDangerousHeight);
                ImGuiIV.DragFloat("DangerousHeight", ref ModSettings.DangerousHeight, 0.01f);
                ImGuiIV.DragFloat("SpawnChanceWhenAboveDangerousHeight", ref ModSettings.SpawnChanceWhenAboveDangerousHeight, 0.01f);

                ImGuiIV.CheckBox("EnableUmbrellaDanger", ref ModSettings.EnableUmbrellaDanger);
                ImGuiIV.DragFloat("SpawnChanceWhenHoldingUmbrella", ref ModSettings.SpawnChanceWhenHoldingUmbrella, 0.01f);

                ImGuiIV.Spacing();
                ImGuiIV.Text("Reactions");
                ImGuiIV.CheckBox("AllowPedReactions", ref ModSettings.AllowPedReactions);
                ImGuiIV.DragFloat("ReactionChance", ref ModSettings.ReactionChance);

                ImGuiIV.Spacing();
                ImGuiIV.Text("Explosion");
                ImGuiIV.CheckBox("CreateExplosions", ref ModSettings.CreateExplosions);
                ImGuiIV.Combo("ExplosionType", ref ModSettings.ExplosionType, Consts.ExplosionTypes);
                ImGuiIV.DragFloat("ExplosionRadius", ref ModSettings.ExplosionRadius, 0.01f);
                ImGuiIV.DragFloat("ExplosionCamShake", ref ModSettings.ExplosionCamShake, 0.01f);

                ImGuiIV.EndTabItem();
            }
        }
        private void ScriptedLightningTabItem()
        {
            if (ImGuiIV.BeginTabItem("Scripted Lightning"))
            {
                if (ImGuiIV.Button("Reload Scripted Lightnings"))
                    ReloadScriptedLightningCommand(null);
                ImGuiIV.SameLine();
                if (ImGuiIV.Button("Save Scripted Lightnings"))
                    SaveScriptedLightning();

                if (ImGuiIV.Button("Create new scripted lightning"))
                    scriptedLighting.Add(new ScriptedLightning());

                ImGuiIV.Spacing();
                ImGuiIV.SeparatorText("Loaded Scripted Lightnings");

                if (scriptedLighting.Count == 0)
                {
                    ImGuiIV.Text("There are no scripted lightnings loaded.");
                }
                else
                {
                    ImGuiIV.Text("There are {0} loaded scripted lightnings.", scriptedLighting.Count);
                    ImGuiIV.Spacing();

                    for (int i = 0; i < scriptedLighting.Count; i++)
                    {
                        ScriptedLightning item = scriptedLighting[i];

                        if (ImGuiIV.TreeNode(string.Format("{0}##PTSLNODE_{1}", i + 1, i)))
                        {

                            if (ImGuiIV.Button("Delete this scripted lightning"))
                            {
                                scriptedLighting.RemoveAt(i);
                                ImGuiIV.TreePop();
                                continue;
                            }
                            ImGuiIV.Spacing();

                            ImGuiIV.CheckBox(string.Format("ForceSpawn##PTSL_{0}", i), ref item.ForceSpawn);

                            // Overrides and Lightning
                            ImGuiIV.CheckBox(string.Format("LightningGrowsFromBottomToTop##PTSL_{0}", i), ref item.LightningGrowsFromBottomToTop);
                            ImGuiIV.CheckBox(string.Format("CanHaveBranches##PTSL_{0}", i), ref item.CanHaveBranches);
                            ImGuiIV.DragInt(string.Format("OverrideLightningSize##PTSL_{0}", i), ref item.OverrideLightningSize);
                            ImGuiIV.ColorEdit3(string.Format("OverrideLightningColor##PTSL_{0}", i), ref item.OverrideLightningColor, eImGuiColorEditFlags.Float);
                            ImGuiIV.ColorEdit3(string.Format("OverrideSkyColor##PTSL_{0}", i), ref item.OverrideSkyColor, eImGuiColorEditFlags.Float);
                            ImGuiIV.DragFloat(string.Format("OverrideSkyBrightness##PTSL_{0}", i), ref item.OverrideSkyBrightness);

                            // Spawn
                            if (ImGuiIV.Button(string.Format("Set##PTSL_TPSP_{0}", i)))
                                item.SpawnPosition = playerPos;
                            ImGuiIV.SameLine();
                            if (ImGuiIV.Button(string.Format("Teleport to##PTSL_TPSP_{0}", i)))
                                IVPed.FromUIntPtr(IVPlayerInfo.FindThePlayerPed()).Teleport(item.SpawnPosition, false, true);
                            ImGuiIV.SameLine();
                            ImGuiIV.DragFloat3(string.Format("SpawnPosition##PTSL_{0}", i), ref item.SpawnPosition);
                            ImGuiIV.DragFloat(string.Format("SpawnRadiusMin##PTSL_{0}", i), ref item.SpawnRadiusMin);
                            ImGuiIV.DragFloat(string.Format("SpawnRadiusMax##PTSL_{0}", i), ref item.SpawnRadiusMax);

                            // Trigger
                            if ((TriggerType)item.TheTrigger == TriggerType.BeAt)
                            {
                                if (ImGuiIV.Button(string.Format("Set##PTSL_TPTP_{0}", i)))
                                    item.TriggerPos = playerPos;
                                ImGuiIV.SameLine();
                                if (ImGuiIV.Button(string.Format("Teleport to##PTSL_TPTP_{0}", i)))
                                    IVPed.FromUIntPtr(IVPlayerInfo.FindThePlayerPed()).Teleport(item.TriggerPos, false, true);
                                ImGuiIV.SameLine();
                                ImGuiIV.DragFloat3(string.Format("TriggerPos##PTSL_{0}", i), ref item.TriggerPos);
                                ImGuiIV.DragFloat(string.Format("TriggerDistance##PTSL_{0}", i), ref item.TriggerDistance);
                                ImGuiIV.Text("Is player within trigger distance: {0}", Vector3.Distance(playerPos, item.TriggerPos) < item.TriggerDistance);
                            }

                            ImGuiIV.Combo(string.Format("TheTrigger##PTSL_{0}", i), ref item.TheTrigger, Consts.TriggerTypes);
                            ImGuiIV.DragFloat(string.Format("SpawnChance##PTSL_{0}", i), ref item.SpawnChance);

                            ImGuiIV.TreePop();
                        }
                    }
                }

                ImGuiIV.EndTabItem();
            }
        }

        private void Main_WaitTick(object sender, EventArgs e)
        {
            if (disableRandomLightningBolts)
                return;
            if (!HAS_CUTSCENE_FINISHED() && !ModSettings.AllowLightningBoltsInCutscene)
                return;

            eWeather previousWeather = (eWeather)IVWeather.OldWeatherType;
            eWeather currentWeather = (eWeather)IVWeather.NewWeatherType;

            // Don't let any thunder show up when current minute is above or equal 57
            // This is supposed to prevent any timecycle fuck-ups
            GET_TIME_OF_DAY(out int hour, out int minute);

            if (minute >= 57)
                return;

            // Thunder is fully in progress!
            if (previousWeather == eWeather.WEATHER_LIGHTNING
                && currentWeather == eWeather.WEATHER_LIGHTNING)
            {

                PrepareForLightnigboltSummoning(ThunderstormProgress.Ongoing);

                return;
            }

            // If the thunder is starting
            if (previousWeather != eWeather.WEATHER_LIGHTNING
                && currentWeather == eWeather.WEATHER_LIGHTNING)
            {

                PrepareForLightnigboltSummoning(ThunderstormProgress.Starting);

                return;
            }

            // If the thunder is going away
            if (previousWeather == eWeather.WEATHER_LIGHTNING
                && currentWeather != eWeather.WEATHER_LIGHTNING)
            {

                PrepareForLightnigboltSummoning(ThunderstormProgress.Ending);

            }
        }
        private void Main_Tick(object sender, EventArgs e)
        {
            IVCam finalCam = IVCamera.TheFinalCam;
            IVPed playerPed = IVPed.FromUIntPtr(IVPlayerInfo.FindThePlayerPed());
            playerPos = playerPed.Matrix.Pos;

            // Pause current active sound stream if pause menu is active
            if (IS_PAUSE_MENU_ACTIVE())
            {
                for (int i = 0; i < currentSoundStreams.Count; i++)
                {
                    SoundStream stream = currentSoundStreams[i];

                    if (stream.GetState() == PlaybackState.Playing)
                        stream.Pause();
                }

                return;
            }
            else
            {
                for (int i = 0; i < currentSoundStreams.Count; i++)
                {
                    SoundStream stream = currentSoundStreams[i];

                    if (stream.GetState() == PlaybackState.Paused)
                        stream.Play();
                }
            }

            // Debug stuff
            if (debugKeysEnabled)
            {
                if (ImGuiIV.IsKeyPressed(eImGuiKey.ImGuiKey_L, false))
                {
                    // try get free cam handle from CamRecorderTest mod
                    if (SendScriptCommand("CamRecorderTest", "get_free_cam_handle", null, out object res))
                    {
                        int freeCamHandle = (int)res;

                        if (freeCamHandle != -1)
                        {
                            NativeCamera nativeCamera = new NativeCamera(freeCamHandle);
                            SummonLightningbolt(new Vector3(Vector3.Add(nativeCamera.Position, Vector3.Multiply(nativeCamera.Direction, distanceToCamera)).ToVector2(), ModSettings.SpawnHeight).Around(50f));
                        }
                    }
                    else
                    {
                        SummonLightningbolt(new Vector3(finalCam.Matrix.Pos.Around(distanceToCamera).ToVector2(), ModSettings.SpawnHeight));
                    }
                }
            }

            DateTime utcNow = DateTime.UtcNow;

            // Go through delayed call list
            for (int i = 0; i < delayedCalls.Count; i++)
            {
                DelayedCall delayedCall = delayedCalls[i];

                if (utcNow > delayedCall.ExecuteAt)
                {
                    delayedCall.ActionToExecute?.Invoke();
                    delayedCalls.RemoveAt(i);
                }
            }

            // Update 3D stuff for Bass
            Bass.Set3DFactors(1.0f, 0.0045f, 0.0f);
            Bass.Set3DPosition(new Vector3D(finalCam.Matrix.Pos.X, finalCam.Matrix.Pos.Y, finalCam.Matrix.Pos.Z),
                null,
                new Vector3D(finalCam.Matrix.At.X, finalCam.Matrix.At.Y, finalCam.Matrix.At.Z),
                new Vector3D(finalCam.Matrix.Up.X, finalCam.Matrix.Up.Y, finalCam.Matrix.Up.Z));
            Bass.Apply3D();

            // Get game SFX volume
            uint gameSfxVolume = IVMenuManager.GetSetting(eSettings.SETTING_SFX_LEVEL);

            // Check if any sound streams can be removed and update their volume based on the game volume
            for (int i = 0; i < currentSoundStreams.Count; i++)
            {
                SoundStream stream = currentSoundStreams[i];

                if (Bass.ChannelIsActive(stream.Handle) == PlaybackState.Stopped)
                    currentSoundStreams.RemoveAt(i);
                else
                {
                    stream.CalculateTargetVolume(gameSfxVolume, IS_INTERIOR_SCENE());
                    stream.ApplyVolume();
                }
            }

            // Get the timecycle parameters of the lightning weather preset
            lightningTimecycParams = IVTimeCycle.TheTimeCycle.GetTimeCycleParams((int)GET_HOURS_OF_DAY(), (int)eWeather.WEATHER_LIGHTNING);

            // Light up the clouds when there is atleast one lightning bolt in the world
            bool doesAtleastOneLightingBoltExists = lightningBolts.Count != 0;

            if (doesAtleastOneLightingBoltExists)
            {
                // Reset some stuff
                hasFadingReachedTargetCloudValues = false;

                // Get the average fade out speed of all lightning bolts
                averageFadeOutSpeed = lightningBolts.Average(x => x.FadeOutSpeed);

                // Store the previous cloud brightness
                if (!storedPreviousCloudsBrightness)
                {
                    previousCloudsBrightness = lightningTimecycParams.CloudsBrightness;
                    previousCloudsColor1 = lightningTimecycParams.Cloud1Color;
                    previousCloudsColor2 = lightningTimecycParams.Cloud2Color;
                    previousCloudsColor3 = lightningTimecycParams.Cloud3Color;
                    storedPreviousCloudsBrightness = true;
                }

                // Get the last lightning bolt that occured and apply its settings
                LightningBolt lastLightningBolt = lightningBolts.LastOrDefault();

                Vector3 targetSkyColor = ModSettings.BoltColor;
                float targetSkyBrightness = ModSettings.AdditionalCloudBrightness;

                if (lastLightningBolt != null)
                {
                    if (lastLightningBolt.OverrideSkyColor != Vector3.Zero)
                        targetSkyColor = lastLightningBolt.OverrideSkyColor;
                    if (!(lastLightningBolt.OverrideSkyBrightness <= -1f))
                        targetSkyBrightness = lastLightningBolt.OverrideSkyBrightness;
                }

                // Make the clouds light up and change color
                lightningTimecycParams.CloudsBrightness = previousCloudsBrightness + targetSkyBrightness;
                lightningTimecycParams.Cloud1Color = targetSkyColor;
                lightningTimecycParams.Cloud2Color = targetSkyColor;
                lightningTimecycParams.Cloud3Color = targetSkyColor;

                wasAnythingCloudRelatedChanged = true;
            }
            else
            {
                // If the current cloud brightness and color values have not reached the previous value yet we continue fading
                if (!hasFadingReachedTargetCloudValues && wasAnythingCloudRelatedChanged)
                {
                    // Reset some stuff
                    storedPreviousCloudsBrightness = false;

                    // Fade cloud brightness and color to previous value
                    lightningTimecycParams.CloudsBrightness = Lerp(lightningTimecycParams.CloudsBrightness, previousCloudsBrightness, averageFadeOutSpeed);
                    lightningTimecycParams.Cloud1Color = Vector3.Lerp(lightningTimecycParams.Cloud1Color, previousCloudsColor1, averageFadeOutSpeed);
                    lightningTimecycParams.Cloud2Color = Vector3.Lerp(lightningTimecycParams.Cloud2Color, previousCloudsColor2, averageFadeOutSpeed);
                    lightningTimecycParams.Cloud3Color = Vector3.Lerp(lightningTimecycParams.Cloud3Color, previousCloudsColor3, averageFadeOutSpeed);

                    // Check if current cloud brightness and color values have reached the previous value
                    if (lightningTimecycParams.CloudsBrightness <= previousCloudsBrightness
                        && lightningTimecycParams.Cloud1Color == previousCloudsColor1
                        && lightningTimecycParams.Cloud2Color == previousCloudsColor2
                        && lightningTimecycParams.Cloud3Color == previousCloudsColor3)
                    {
                        wasAnythingCloudRelatedChanged = false;
                        hasFadingReachedTargetCloudValues = true;
                    }
                }
            }

            // Draw all lightning bolts
            for (int i = 0; i < lightningBolts.Count; i++)
            {
                LightningBolt lightningBolt = lightningBolts[i];

                Color boltColor = lightningBolt.OverrideBoltColor != Vector3.Zero ? ImGuiIV.FloatRGBToColor(lightningBolt.OverrideBoltColor) : ImGuiIV.FloatRGBToColor(ModSettings.BoltColor);

                // Draw all lightning bolt points of this lightning bolt
                for (int p = 0; p < lightningBolt.Points.Length; p++)
                {
                    Vector3 point = lightningBolt.Points[p];

                    if (point != Vector3.Zero)
                    {
                        DRAW_CORONA(point, lightningBolt.CoronaSize, 0, 0f, boltColor);
                        IVShadows.StoreStaticShadow(true, point, boltColor, ModSettings.LightIntensity, ModSettings.LightRange);
                    }
                }

                // Draw all branches if there are any
                if (lightningBolt.Branches != null)
                {
                    for (int b = 0; b < lightningBolt.Branches.Count; b++)
                    {
                        LightningBoltBranch branch = lightningBolt.Branches[b];

                        for (int p = 0; p < branch.Points.Length; p++)
                        {
                            Vector3 point = branch.Points[p];

                            if (point != Vector3.Zero)
                            {
                                DRAW_CORONA(point, lightningBolt.CoronaSize, 0, 0f, boltColor);
                                IVShadows.StoreStaticShadow(true, point, boltColor, ModSettings.LightIntensity, ModSettings.LightRange);
                            }
                        }
                    }
                }

                // Fade out lightning bolt
                if (!(lightningBolt.CoronaSize <= 25f))
                    // Do fading
                {
                    lightningBolt.CoronaSize = Lerp(lightningBolt.CoronaSize, 0f, overrideFadeOutSpeed ? newFadeOutSpeed : lightningBolt.FadeOutSpeed);
                }
                else
                    // Remove lightning bolt if faded out
                {
                    lightningBolts.RemoveAt(i);
                }
            }
        }

    }
}
