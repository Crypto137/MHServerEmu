using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public enum NaviContentTags
    {
        None = 0,
        OpaqueWall = 1,
        TransparentWall = 2,
        Blocking = 3,
        NoFly = 4,
        Walkable = 5,
        Obstacle = 6
    }

    [Flags]
    public enum NaviContentFlags
    {
        AddWalk         = 1 << 0,
        RemoveWalk      = 1 << 1,
        AddFly          = 1 << 2,
        RemoveFly       = 1 << 3,
        AddPower        = 1 << 4,
        RemovePower     = 1 << 5,
        AddSight        = 1 << 6,
        RemoveSight     = 1 << 7
    }

    public class NaviPatchPrototype
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

    public class NaviPatchEdgePrototype
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
}
