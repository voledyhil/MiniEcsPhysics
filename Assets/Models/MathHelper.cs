using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Models
{
    public class MathHelper
    {
        public const float EPSILON = 1.1920928955078125e-7f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equal(float a, float b)
        {
            return math.abs(a - b) <= EPSILON;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 Mul(float2x2 lhs, float2 rhs)
        {
            return new float2(lhs.c0.x * rhs.x + lhs.c1.x * rhs.y, lhs.c0.y * rhs.x + lhs.c1.y * rhs.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Cross(float2 x, float2 y)
        {
            return x.x * y.y - x.y * y.x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 Cross(float x, float2 y)
        {
            return new float2(y.y * -x, y.x * x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2x2 Transpose(float2x2 v)
        {
            return new float2x2(v.c0.x, v.c0.y, v.c1.x, v.c1.y);
        }

    }
}