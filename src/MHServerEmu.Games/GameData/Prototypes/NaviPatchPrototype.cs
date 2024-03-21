using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Navi;

namespace MHServerEmu.Games.GameData.Prototypes
{

    public class NaviPatchSourcePrototype : Prototype
    {
        // PatchFragments "Skipping writing field %s in class %s because it has eFlagDontCook set"
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

    public class NaviPatchPrototype : Prototype
    {
        public Vector3[] Points { get; }
        public NaviPatchEdgePrototype[] Edges { get; }

        public NaviPatchPrototype(BinaryReader reader)
        {
            Points = new Vector3[reader.ReadUInt32()];
            for (int i = 0; i < Points.Length; i++)
                Points[i] = reader.ReadVector3();

            Edges = new NaviPatchEdgePrototype[reader.ReadUInt32()];
            for (int i = 0; i < Edges.Length; i++)
                Edges[i] = new(reader);
        }
    }

    public class NaviPatchEdgePrototype : Prototype
    {
        public uint ProtoNameHash { get; }
        public uint Index0 { get; }
        public uint Index1 { get; }
        public NaviContentFlags[] Flags0 { get; }
        public NaviContentFlags[] Flags1 { get; }

        public NaviPatchEdgePrototype(BinaryReader reader)
        {
            ProtoNameHash = reader.ReadUInt32();
            Index0 = reader.ReadUInt32();
            Index1 = reader.ReadUInt32();

            Flags0 = new NaviContentFlags[reader.ReadUInt32()];
            for (int i = 0; i < Flags0.Length; i++)
                Flags0[i] = (NaviContentFlags)reader.ReadByte();

            Flags1 = new NaviContentFlags[reader.ReadUInt32()];
            for (int i = 0; i < Flags1.Length; i++)
                Flags1[i] = (NaviContentFlags)reader.ReadByte();
        }
    }

    public class NaviPatchFragmentPrototype : Prototype
    {
        public Vector3 Position { get; }
        public Vector3 Rotation { get; }
        public Vector3 Scale { get; }
        public Vector3 PrePivot { get; }
        public ulong FragmentResource { get; }
    }
}
