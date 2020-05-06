using Unity.Mathematics;

namespace Models
{
    public struct ContactInfo
    {
        public float Penetration;
        public float2 Normal;
        public float2 HitPoint;
        public bool Hit;
    }
}