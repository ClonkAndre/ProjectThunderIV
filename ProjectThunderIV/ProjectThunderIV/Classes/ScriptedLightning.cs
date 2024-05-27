using System.Numerics;

using Newtonsoft.Json;

namespace ProjectThunderIV.Classes
{
    internal class ScriptedLightning
    {
        [JsonIgnore] public bool ForceSpawn;
        public bool LightningGrowsFromBottomToTop;
        public bool CanHaveBranches;
        public int OverrideLightningSize;
        public Vector3 OverrideLightningColor;
        public Vector3 OverrideSkyColor;
        public float OverrideSkyBrightness;

        public Vector3 SpawnPosition;
        public float SpawnRadiusMin;
        public float SpawnRadiusMax;

        //public bool OnlyTriggerWhenPlayerIsWithinTriggerPosRadius;
        public Vector3 TriggerPos;
        public float TriggerDistance;

        public int TheTrigger;
        public float SpawnChance;
    }
}
