using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Prototypes.Markers;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PropPackagePrototype : Prototype
    {
        public uint Header { get; }
        public uint Version { get; }
        public uint ClassId { get; }
        public ProceduralPropGroupPrototype[] PropGroups { get; }

        public PropPackagePrototype(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = reader.ReadUInt32();
                Version = reader.ReadUInt32();
                ClassId = reader.ReadUInt32();

                PropGroups = new ProceduralPropGroupPrototype[reader.ReadUInt32()];
                for (int i = 0; i < PropGroups.Length; i++)
                    PropGroups[i] = new(reader);
            }
        }
    }

    public class ProceduralPropGroupPrototype : Prototype
    {
        public ResourcePrototypeHash ProtoNameHash { get; }
        public string NameId { get; }
        public string PrefabPath { get; }
        public Vector3 MarkerPosition { get; }
        public Vector3 MarkerRotation { get; }
        public MarkerSetPrototype Objects { get; }
        public NaviPatchSourcePrototype NaviPatchSource { get; }
        public ushort RandomRotationDegrees { get; } // short
        public ushort RandomPosition { get; } // short

        public ProceduralPropGroupPrototype(BinaryReader reader)
        {
            ProtoNameHash = (ResourcePrototypeHash)reader.ReadUInt32();
            NameId = reader.ReadFixedString32();
            PrefabPath = reader.ReadFixedString32();
            MarkerPosition = reader.ReadVector3();
            MarkerRotation = reader.ReadVector3();

            Objects = new(reader);

            NaviPatchSource = new(reader);
            RandomRotationDegrees = reader.ReadUInt16();
            RandomPosition = reader.ReadUInt16();
        }

    }
}
