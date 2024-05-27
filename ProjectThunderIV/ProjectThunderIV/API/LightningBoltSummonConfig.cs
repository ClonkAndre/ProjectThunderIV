using System.Numerics;

namespace ProjectThunderIV.API
{
    internal class LightningBoltSummonConfig
    {
        #region Variables
        public Vector3 SpawnPosition;
        public bool CanHaveBranches;
        public bool GrowFromGroundUp;
        public int OverrideLightningSize;
        public Vector3 OverrideBoltColor;
        public Vector3 OverrideSkyColor;
        public float OverrideSkyBrightness;
        #endregion

        #region Constructor
        public LightningBoltSummonConfig(Vector3 spawnPosition,
            bool canHaveBranches,
            bool growFromGroundUp,
            int overrideLightningSize,
            Vector3 overrideBoltColor,
            Vector3 overrideSkyColor,
            float overrideSkyBrightness)
        {
            SpawnPosition = spawnPosition;
            CanHaveBranches = canHaveBranches;
            GrowFromGroundUp = growFromGroundUp;
            OverrideLightningSize = overrideLightningSize;
            OverrideBoltColor = overrideBoltColor;
            OverrideSkyColor = overrideSkyColor;
            OverrideSkyBrightness = overrideSkyBrightness;
        }
        public LightningBoltSummonConfig(Vector3 spawnPosition)
        {
            SpawnPosition = spawnPosition;
            CanHaveBranches = true;
            GrowFromGroundUp = false;
            OverrideLightningSize = -1;
            OverrideBoltColor = default(Vector3);
            OverrideSkyColor = default(Vector3);
            OverrideSkyBrightness = -1f;
        }
        public LightningBoltSummonConfig()
        {
            
        }
        #endregion
    }
}
