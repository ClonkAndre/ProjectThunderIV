using System.Numerics;
using System.Runtime.InteropServices;

namespace ProjectThunderIV.Classes.Network
{
    [StructLayout(LayoutKind.Sequential)]
    internal class NetworkSyncStruct
    {

        // Lightning
        public bool SpawnNow;
        public Vector3 SpawnPosition;
        public bool CanHaveBranches;
        public bool GrowFromGroundUp;
        public int OverrideLightningSize;
        public Vector3 OverrideBoltColor;
        public Vector3 OverrideSkyColor;
        public float OverrideSkyBrightness;

        // Blackout
        public bool ActivateBlackout;
        public int BlackoutActiveTime;

        //[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        //public string LightningSyncStr;

        //[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        //public string BlackoutSyncStr;

    }
}
