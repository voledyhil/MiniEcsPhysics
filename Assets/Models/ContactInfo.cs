using Unity.Mathematics;

namespace Models
{
    public struct ContactInfo
    {
        public float Penetration;
        public float2 Normal;
        public float2x2 Contacts;
        public int ContactCount;
    }
}