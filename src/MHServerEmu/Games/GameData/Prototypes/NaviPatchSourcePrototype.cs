using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class NaviPatchSourcePrototype : Prototype
    {
        // public NaviPatchFragmentPrototype[] PatchFragments; "Skipping writing field %s in class %s because it has eFlagDontCook set"
        public uint NaviPatchCrc { get; }
        public NaviPatchPrototype NaviPatch { get; }
        public NaviPatchPrototype PropPatch { get; }
        public float PlayableArea { get; }
        public float SpawnableArea { get; }

        public NaviPatchSourcePrototype(BinaryReader reader)
        {
            NaviPatchCrc = reader.ReadUInt32();
            NaviPatch = new(reader);
            PropPatch = new(reader);
            PlayableArea = reader.ReadSingle();
            SpawnableArea = reader.ReadSingle();
        }
    }
    
    public class NaviPatchFragmentPrototype : Prototype
    {
        public Vector3 Position; 
        public Vector3 Rotation;
        public Vector3 Scale;
        public Vector3 PrePivot;
        public ulong FragmentResource;
        public NaviPatchFragmentPrototype() {}
    }

    public enum ContentTags
    {
        None = 0,
        OpaqueWall = 1,
        TransparentWall = 2,
        Blocking = 3,
        NoFly = 4,
        Walkable = 5,
        Obstacle = 6,
    }

    public class NaviFragmentPolyPrototype : Prototype
    {
        public ContentTags ContentTag;
        public ulong Points;
        public NaviFragmentPolyPrototype() {}
    }

    public class NaviFragmentPrototype : Prototype
    {
        public NaviFragmentPolyPrototype[] FragmentPolys;
        public NaviFragmentPolyPrototype[] PropFragmentPolys;
        public NaviFragmentPrototype() {}
    }


}
