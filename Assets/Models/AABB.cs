using Unity.Mathematics;

namespace Models
{
    public struct AABB
    {
        public float2 Min;
        public float2 Max;

        public AABB(float2 min, float2 max)
        {
            Min = min;
            Max = max;
        }
        
        public AABB(float2 size, float2 position, float rotation)
        {
            Min = float2.zero;
            Max = float2.zero;

            math.sincos(rotation, out float sin, out float cos);

            float ex = math.max(math.abs(size.x * cos + size.y * sin), math.abs(size.x * cos - size.y * sin));
            float ey = math.max(math.abs(size.x * sin - size.y * cos), math.abs(size.x * sin + size.y * cos));

            Min = new float2(position.x - ex, position.y - ey);
            Max = new float2(position.x + ex, position.y + ey);
        }

        public bool Overlap(AABB aabb)
        {
            return !(Max.x < aabb.Min.x) && !(Min.x > aabb.Max.x) && !(Max.y < aabb.Min.y) && !(Min.y > aabb.Max.y);
        }

        public override string ToString()
        {
            return $"({Min}, {Max})";
        }
    }
}