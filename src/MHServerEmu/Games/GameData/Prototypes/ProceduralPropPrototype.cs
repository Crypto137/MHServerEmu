using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.GameData.Resources;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PropPackagePrototype : Prototype, IBinaryResource
    {
        public ProceduralPropGroupPrototype[] PropGroups { get; private set; }

        public void Deserialize(BinaryReader reader)
        {
            PropGroups = new ProceduralPropGroupPrototype[reader.ReadUInt32()];
            for (int i = 0; i < PropGroups.Length; i++)
                PropGroups[i] = new(reader);
        }
    }

    public class ProceduralPropGroupPrototype : Prototype
    {
        public string NameId { get; }
        public string PrefabPath { get; }
        public Vector3 MarkerPosition { get; }
        public Vector3 MarkerRotation { get; }
        public MarkerSetPrototype Objects { get; }
        public NaviPatchSourcePrototype NaviPatchSource { get; }
        public ushort RandomRotationDegrees { get; }
        public ushort RandomPosition { get; }

        public ProceduralPropGroupPrototype(BinaryReader reader)
        {
            var protoNameHash = (ResourcePrototypeHash)reader.ReadUInt32();

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
