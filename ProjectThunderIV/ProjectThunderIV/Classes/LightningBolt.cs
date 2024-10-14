using System.Collections.Generic;
using System.Numerics;

namespace ProjectThunderIV.Classes
{
    internal class LightningBolt
    {
        #region Variables
        public Vector3[] Points;
        public List<LightningBoltBranch> Branches;
        public Vector3 GroundPosition;

        public float StartingCoronaSize;
        public float CoronaSize;
        public float FadeOutSpeed;

        public Vector3 OverrideBoltColor;
        public Vector3 OverrideSkyColor;
        public float OverrideSkyBrightness;
        #endregion

        #region Constructor
        public LightningBolt(int size, float coronaSize, float fadeOutSpeed)
        {
            Points = new Vector3[size];
            StartingCoronaSize = coronaSize;
            CoronaSize = coronaSize;
            FadeOutSpeed = fadeOutSpeed;
        }
        #endregion

        public void ResetSize()
        {
            CoronaSize = StartingCoronaSize;
        }
    }
}
