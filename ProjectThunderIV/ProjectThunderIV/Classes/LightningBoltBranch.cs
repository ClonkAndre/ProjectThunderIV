using System.Numerics;

using static IVSDKDotNet.Native.Natives;

namespace ProjectThunderIV.Classes
{
    internal class LightningBoltBranch
    {

        #region Variables
        public Vector3[] Points;

        public float XValue;
        public float YValue;
        #endregion

        #region Constructor
        public LightningBoltBranch(int size)
        {
            Points = new Vector3[size];

            XValue = GENERATE_RANDOM_FLOAT_IN_RANGE(-0.5f, 0.5f);
            YValue = GENERATE_RANDOM_FLOAT_IN_RANGE(-0.5f, 0.5f);
        }
        #endregion

    }
}
