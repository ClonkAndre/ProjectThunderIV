using System;
using System.Collections.Generic;
using System.Linq;

using CCL.GTAIV;

using ProjectThunderIV.Extensions;

using IVSDKDotNet;
using IVSDKDotNet.Enums;
using static IVSDKDotNet.Native.Natives;

namespace ProjectThunderIV.Classes
{
    internal class BlackoutSystem
    {

        #region Variables and Properties
        // Variables
        private static bool wasInitialized;

        private static bool _isBlackoutActive;
        private static bool _hasBlackoutBeenTriggeredByAnotherScript;

        // Timecycle
        private static float distantCoronaSizeHour0;
        private static float distantCoronaSizeHour5;
        private static float distantCoronaSizeHour23;

        // Lists
        private static List<BlackoutModels> models;
        private static Dictionary<int, VisionPedBackup> backedUpVisionPeds;
        private static Dictionary<int, float> savedTrainSpeed;

        // Other
        private static float constrastFadeValue;
        private static float savedContrast;
        private static bool wasContrastChanged;
        private static int weatherTriggeredAt;

        // Properties
        public static bool IsActive
        {
            get
            {
                return _isBlackoutActive;
            }
            private set
            {
                _isBlackoutActive = value;
            }
        }
        public static bool WasTriggeredByAnotherScript
        {
            get
            {
                return _hasBlackoutBeenTriggeredByAnotherScript;
            }
            private set
            {
                _hasBlackoutBeenTriggeredByAnotherScript = value;
            }
        }
        #endregion

        #region Structs
        public struct BlackoutModels
        {
            #region Variables
            public int ModelIndex;
            public uint OriginalHoursOnOffFlag;
            #endregion

            #region Constructor
            public BlackoutModels(int modelIndex, uint originalHoursOnffFlag)
            {
                ModelIndex = modelIndex;
                OriginalHoursOnOffFlag = originalHoursOnffFlag;
            }
            #endregion
        }
        public struct VisionPedBackup
        {
            #region Variables
            public int Handle;
            public float OriginalSenseRange;
            public float NewSenseRange;
            #endregion

            #region Constructor
            public VisionPedBackup(int handle, float originalSenseRange, float newSenseRange)
            {
                Handle = handle;
                OriginalSenseRange = originalSenseRange;
                NewSenseRange = newSenseRange;
            }
            #endregion

            #region Functions
            public bool DoesPedExists()
            {
                return Handle != 0 && DOES_CHAR_EXIST(Handle);
            }
            #endregion

            public override string ToString()
            {
                return string.Format("OriginalSenseRange: {0}, NewSenseRange: {1}", OriginalSenseRange, NewSenseRange);
            }
        }
        #endregion

        #region Methods
        public static void Initialize()
        {
            if (wasInitialized)
                return;

            wasInitialized = true;

            // Save original corona size of timecycle
            distantCoronaSizeHour0 =    IVTimeCycle.TheTimeCycle.GetTimeCycleParams(0, (int)eWeather.WEATHER_LIGHTNING).DistantCoronaSize;
            distantCoronaSizeHour5 =    IVTimeCycle.TheTimeCycle.GetTimeCycleParams(5, (int)eWeather.WEATHER_LIGHTNING).DistantCoronaSize;
            distantCoronaSizeHour23 =   IVTimeCycle.TheTimeCycle.GetTimeCycleParams(23, (int)eWeather.WEATHER_LIGHTNING).DistantCoronaSize;

            // Get all timed models
            IVBaseModelInfo[] infos = IVModelInfo.ModelInfos.Where(x => (eModelInfoType)x.GetModelType() == eModelInfoType.MODEL_INFO_TIME).ToArray();

            // Initialize lists
            models = new List<BlackoutModels>(infos.Length);
            backedUpVisionPeds = new Dictionary<int, VisionPedBackup>(32);
            savedTrainSpeed = new Dictionary<int, float>(32);

            // Store original time flags and index of models
            for (int i = 0; i < infos.Length; i++)
            {
                IVBaseModelInfo info = infos[i];

                // Get the time info of this model
                IVTimeInfo modelTimeInfo = info.GetTimeInfo();

                // Get the actual index from within the ModelInfos array
                int index = IVModelInfo.GetIndexFromHashKey(info.Hash);

                // Store this model with its index and its original time flags
                models.Add(new BlackoutModels(index, modelTimeInfo.HoursOnOff));
            }

            infos = null;
        }
        public static void Uninitialize(bool isShuttingDown)
        {
            if (!IsValid())
                return;

            // Get rid of all active delayed actions
            Logging.LogDebug("Script is uninitializing. Removed {0} blackout delayed actions with tag 'BLACKOUT_ON'.", Main.Instance.RemoveAllDelayedActionsWithThisTag("BLACKOUT_ON"));
            Logging.LogDebug("Script is uninitializing. Removed {0} blackout delayed actions with tag 'BLACKOUT_OFF'.", Main.Instance.RemoveAllDelayedActionsWithThisTag("BLACKOUT_OFF"));

            // Do stuff if game is not shutting down
            if (!isShuttingDown)
            {
                // Turn all buildings back on
                SwitchBlackout(false, false, true);

                // Restore previous contrast
                if (!(savedContrast <= 0f))
                    IVMenuManager.FloatContrast = savedContrast;

                // Restore peds original sense range
                foreach (KeyValuePair<int, VisionPedBackup> item in backedUpVisionPeds)
                {
                    VisionPedBackup visionPedBackout = item.Value;

                    if (visionPedBackout.DoesPedExists())
                        SET_SENSE_RANGE(visionPedBackout.Handle, visionPedBackout.OriginalSenseRange);
                }
            }

            // Reset stuff
            IsActive = false;
            WasTriggeredByAnotherScript = false;

            // Get rid of lists
            if (models != null)
            {
                models.Clear();
                models = null;
            }
            if (backedUpVisionPeds != null)
            {
                backedUpVisionPeds.Clear();
                backedUpVisionPeds = null;
            }
            if (savedTrainSpeed != null)
            {
                savedTrainSpeed.Clear();
                savedTrainSpeed = null;
            }
        }

        private static void SwitchTimedModelFlag(int listIndex, bool on)
        {
            if (!IsValid())
                return;

            // Get the model from the list at the given index
            BlackoutModels blackoutModel = models[listIndex];

            // Check if model index is valid
            if (blackoutModel.ModelIndex <= 0)
                return;

            // Get the base model info from this model index
            IVBaseModelInfo modelInfo = IVModelInfo.GetModelInfoFromIndex(blackoutModel.ModelIndex);

            if (modelInfo == null)
                return;

            // Get the time info from this base model info
            IVTimeInfo modelTimeInfo = modelInfo.GetTimeInfo();

            if (modelTimeInfo == null)
                return;

            // Change flags
            if (on)
            {
                // Turn building lights off
                modelTimeInfo.HoursOnOff = 0;
            }
            else
            {
                // Turn building lights on
                modelTimeInfo.HoursOnOff = blackoutModel.OriginalHoursOnOffFlag;
            }
        }

        public static void Update()
        {
            if (!IsValid())
                return;

            // Pay'n'Spray shops do not work when there is no power
            if (IsActive && !IS_PAY_N_SPRAY_ACTIVE())
            {
                // Only disable Pay'n'Spray shops when there is currently no respray happening
                SET_NO_RESPRAYS(true);
            }

            if (!ModSettings.EnableAdditionalBlackoutDarkness)
                return;
            if (savedContrast <= 0f)
                return;

            bool isEveningOrNight = NativeWorld.GetDayState() == DayState.Evening || NativeWorld.GetDayState() == DayState.Night;

            // Cap AdditionalBlackoutDarkness setting
            if (ModSettings.AdditionalBlackoutDarkness < 0.0f)
                ModSettings.AdditionalBlackoutDarkness = 0f;
            if (ModSettings.AdditionalBlackoutDarkness > 1.0f)
                ModSettings.AdditionalBlackoutDarkness = 1.0f;

            // Handle fading
            if (IsActive)
            {
                // Fade value in
                constrastFadeValue += 0.0045f;

                if (constrastFadeValue > 1.0f)
                    constrastFadeValue = 1.0f;
            }
            else
            {
                // Fade value out
                constrastFadeValue -= 0.01f;

                if (constrastFadeValue < 0.0f)
                    constrastFadeValue = 0.0f;
            }

            // Constrast and Ped vision code
            // Code will only run in the evening or the night as it wouldn't make sense
            // for the cops to be unable to see the player anymore in day time.
            // The additional darkness also doesn't make any sense in day time.
            if (isEveningOrNight)
            {
                if (IsActive)
                {
                    // Change contrast
                    IVMenuManager.FloatContrast = Main.Lerp(savedContrast, savedContrast + ModSettings.AdditionalBlackoutDarkness, constrastFadeValue);
                    wasContrastChanged = true;

                    // Ped vision
                    if (ModSettings.DecreaseCopsVisionOnActiveBlackout)
                    {
                        IVPool pedPool = Main.Instance.GetPedPool();
                        for (int i = 0; i < pedPool.Count; i++)
                        {
                            UIntPtr ptr = pedPool.Get(i);

                            if (ptr == UIntPtr.Zero)
                                continue;
                            if (ptr == Main.Instance.GetPlayerPed().GetUIntPtr())
                                continue;

                            int handle = (int)pedPool.GetIndex(ptr);

                            if (!DOES_CHAR_EXIST(handle))
                                continue;

                            // Check if char is already added to list
                            if (backedUpVisionPeds.ContainsKey(handle))
                            {
                                // Already added to list.

                                VisionPedBackup visionPedBackout = backedUpVisionPeds[handle];

                                // Keep overriding their sense range to their target value.
                                SET_SENSE_RANGE(visionPedBackout.Handle, visionPedBackout.NewSenseRange);

                                continue;
                            }

                            // Get and check type of ped
                            GET_PED_TYPE(handle, out uint pedType);

                            if ((ePedType)pedType != ePedType.PED_TYPE_COP)
                                continue;

                            IVPed ped = IVPed.FromUIntPtr(ptr);

                            if (ped == null)
                                continue;

                            IVPedIntelligenceNY pedIntelligence = ped.PedIntelligence;

                            if (pedIntelligence != null)
                            {
                                // Get current sense range value
                                float originalMinSenseRange = pedIntelligence.SenseRange1;

                                // Calculate new sense range value
                                float darkness = ModSettings.AdditionalBlackoutDarkness * 10f;

                                float newValue = originalMinSenseRange / darkness;

                                SET_SENSE_RANGE(handle, newValue);

                                Logging.LogDebug("Sense range of ped {0} was set to {1} (Original was {2}).", handle, newValue, originalMinSenseRange);

                                // Add char to list
                                backedUpVisionPeds.Add(handle, new VisionPedBackup(handle, originalMinSenseRange, newValue));
                            }
                        }
                    }
                }
                else
                {
                    // Change contrast
                    IVMenuManager.FloatContrast = Main.Lerp(savedContrast, savedContrast + ModSettings.AdditionalBlackoutDarkness, constrastFadeValue);
                    wasContrastChanged = false;

                    // Restore previous ped vision
                    if (ModSettings.DecreaseCopsVisionOnActiveBlackout)
                    {
                        for (int i = 0; i < backedUpVisionPeds.Count; i++)
                        {
                            KeyValuePair<int, VisionPedBackup> kvp = backedUpVisionPeds.ElementAt(i);

                            // Restore previous sense range
                            if (kvp.Value.DoesPedExists())
                                SET_SENSE_RANGE(kvp.Value.Handle, kvp.Value.OriginalSenseRange);

                            // Remove list entry
                            backedUpVisionPeds.Remove(kvp.Key);
                        }
                    }
                }
            }
            else
            {
                // If contrast was changed but it is no longer evening or night then we need to reset the contrast here
                if (wasContrastChanged)
                {
                    IVMenuManager.FloatContrast = savedContrast;
                    wasContrastChanged = false;
                }
            }

            // Train stopping code
            if (IsActive)
            {
                IVPool vehPool = Main.Instance.GetVehiclePool();
                for (int i = 0; i < vehPool.Count; i++)
                {
                    UIntPtr ptr = vehPool.Get(i);

                    if (ptr == UIntPtr.Zero)
                        continue;

                    int handle = (int)vehPool.GetIndex(ptr);

                    if (!DOES_VEHICLE_EXIST(handle))
                    {
                        if (savedTrainSpeed.ContainsKey(handle))
                            savedTrainSpeed.Remove(handle);

                        continue;
                    }

                    GET_CAR_MODEL(handle, out uint model);

                    if (!IS_THIS_MODEL_A_TRAIN(model))
                        continue;

                    if (!savedTrainSpeed.ContainsKey(handle))
                    {
                        // Save train speed when we first got the train
                        GET_CAR_SPEED(handle, out float speed);
                        savedTrainSpeed.Add(handle, speed);
                    }
                    else
                    {
                        // Get the saved train speed for this handle
                        float speed = savedTrainSpeed[handle];

                        // Lerp from saved speed to 0 for smooth deceleration
                        SET_TRAIN_SPEED(handle, Main.Lerp(speed, 0f, constrastFadeValue));
                    }
                }
            }

            // Cleanup invalid items within backedUpVisionPeds dict
            if (backedUpVisionPeds.Count != 0)
            {
                for (int i = 0; i < backedUpVisionPeds.Count; i++)
                {
                    KeyValuePair<int, VisionPedBackup> kvp = backedUpVisionPeds.ElementAt(i);

                    // Remove list entry
                    if (!kvp.Value.DoesPedExists())
                        backedUpVisionPeds.Remove(kvp.Key);
                }
            }
        }
        #endregion

        #region Functions
        private static bool IsValid()
        {
            return wasInitialized && models != null;
        }
        #endregion

        public static bool SwitchBlackout(bool switchOn, bool calledFromAnotherScript = false, bool instant = false)
        {
            // Check stuff
            if (!IsValid())
                return false;
            if (IsActive && switchOn) // Already switched on
                return false;
            if (!IsActive && !switchOn) // Already switched off
                return false;

            // Do stuff on switch on/off
            if (switchOn)
            {
                // Get rid of delayed actions that turn the buildings off depending on the state so they dont interfere with each other
                Logging.LogDebug("Blackout switched on! Removed {0} blackout delayed actions with tag 'BLACKOUT_OFF'.", Main.Instance.RemoveAllDelayedActionsWithThisTag("BLACKOUT_OFF"));

                // Play a "power down" sound
                Main.Instance.PlayBlackoutSound();

                // Save original constrast value
                savedContrast = IVMenuManager.FloatContrast;
            }
            else
            {
                // Get rid of delayed actions that turn the buildings on depending on the state so they dont interfere with each other
                Logging.LogDebug("Blackout switched off! Removed {0} blackout delayed actions with tag 'BLACKOUT_ON'.", Main.Instance.RemoveAllDelayedActionsWithThisTag("BLACKOUT_ON"));

                // Cleanup some stuff
                savedTrainSpeed.Clear();
                
                // When power is back on, Pay'n'Spray shops can operate normally again.
                SET_NO_RESPRAYS(false);
            }

            // Set states
            IsActive = switchOn;
            WasTriggeredByAnotherScript = calledFromAnotherScript;

            // Randomize list
            models = models.Randomize();

            bool shouldSwitchOn = switchOn;

            // Go through list and add delayed call which gets executed a bit later for each model so they dont go out at the same time
            for (int i = 0; i < models.Count; i++)
            {
                int listIndex = i;

                // Create action that is responsible for the building blackout
                Action a = () =>
                {
                    SwitchTimedModelFlag(listIndex, shouldSwitchOn);
                };

                // Switch buildings on/off instantly or one-by-one
                if (instant)
                    a.Invoke();
                else
                    Main.Instance.AddDelayedCall(TimeSpan.FromMilliseconds(i * GENERATE_RANDOM_FLOAT_IN_RANGE(0.7f, 1.5f)), a, false, switchOn ? "BLACKOUT_ON" : "BLACKOUT_OFF");
            }

            // Change size of distant coronas
            if (switchOn)
            {
                GET_CURRENT_WEATHER(out weatherTriggeredAt);

                // Make them invisible when the blackout was switched on to make it look like the city really lost its power
                IVTimeCycle.TheTimeCycle.GetTimeCycleParams(0, weatherTriggeredAt).DistantCoronaSize = 0f;
                IVTimeCycle.TheTimeCycle.GetTimeCycleParams(5, weatherTriggeredAt).DistantCoronaSize = 0f;
                IVTimeCycle.TheTimeCycle.GetTimeCycleParams(23, weatherTriggeredAt).DistantCoronaSize = 0f;
            }
            else
            {
                // Restore their previous size when the blackout was switched off
                IVTimeCycle.TheTimeCycle.GetTimeCycleParams(0, weatherTriggeredAt).DistantCoronaSize = distantCoronaSizeHour0;
                IVTimeCycle.TheTimeCycle.GetTimeCycleParams(5, weatherTriggeredAt).DistantCoronaSize = distantCoronaSizeHour5;
                IVTimeCycle.TheTimeCycle.GetTimeCycleParams(23, weatherTriggeredAt).DistantCoronaSize = distantCoronaSizeHour23;
            }

            // Make light unable to render but keep some other lights
            Main.Instance.SetInterceptAddSceneLightsCall(switchOn);

            // Make other coronas unable to render but keep project thunder coronas
            Main.Instance.SetInterceptOnRenderCoronaCall(switchOn);

            // Force traffic lights to be off
            Main.Instance.SetInterceptGetTrafficLightStateCalls(switchOn);

            return true;
        }

    }
}
