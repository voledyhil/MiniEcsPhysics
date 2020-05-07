using MiniEcs.Core;

namespace MiniEcs.Components
{
    public partial class ComponentType
    {
        public const byte Hero = 7;
        public const byte Character = 8;

        public const byte StaticRect = 9;
        public const byte StaticCircle = 10;
        public const byte BlueCircle = 11;
        public const byte YellowCircle = 12;

        public const byte TotalComponents = 13;
    }    
    
    
    public class HeroComponent : IEcsComponent
    {
        public byte Index => ComponentType.Hero;
    }

    public class CharacterComponent : IEcsComponent
    {
        public byte Index => ComponentType.Character;

        public Character Ref;
    }

    public class StaticRectComponent : IEcsComponent
    {
        public byte Index => ComponentType.StaticRect;
    }

    public class StaticCircleComponent : IEcsComponent
    {
        public byte Index => ComponentType.StaticCircle;
    }

    public class BlueCircleComponent : IEcsComponent
    {
        public byte Index => ComponentType.BlueCircle;
    }

    public class YellowCircleComponent : IEcsComponent
    {
        public byte Index => ComponentType.YellowCircle;
    }

}