using System;

using System.Numerics;

namespace ProjectThunderIV.Extensions
{
    internal static class VectorExtensions
    {

        public static Vector3 Clamp(this Vector3 vec, float min, float max)
        {
            float x = Math.Max(min, Math.Min(max, vec.X));
            float y = Math.Max(min, Math.Min(max, vec.Y));
            float z = Math.Max(min, Math.Min(max, vec.Z));
            return new Vector3(x, y, z);
        }

    }
}
