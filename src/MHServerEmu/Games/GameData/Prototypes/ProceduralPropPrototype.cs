using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.GameData.Resources;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PropPackagePrototype : Prototype, IBinaryResource
    {
        private readonly Dictionary<uint, ProceduralPropGroupPrototype> _propGroupMap = new();
        public ProceduralPropGroupPrototype[] PropGroups { get; private set; }

        public void Deserialize(BinaryReader reader)
        {
            PropGroups = new ProceduralPropGroupPrototype[reader.ReadUInt32()];
            for (int i = 0; i < PropGroups.Length; i++)
                PropGroups[i] = new(reader);
        }

        public override void PostProcess()
        {
            base.PostProcess();
            //if (GameDatabase.DataDirectory.PrototypeIsAbstract(GetDataRef())){ return;}

            if (PropGroups.HasValue())
            {
                foreach (ProceduralPropGroupPrototype propGroup in PropGroups)
                {
                    if (propGroup != null && propGroup.NameId != null)
                    {
                        string str = propGroup.NameId.ToLower();
                        _propGroupMap.Add(HashHelper.Djb2(str), propGroup);   // str.Hash()
                    }
                }
            }
        }

        public ProceduralPropGroupPrototype GetPropGroupFromName(string nameId)
        {
            string name = nameId.ToLower();
            if (_propGroupMap.TryGetValue(HashHelper.Djb2(name), out var value))  // name.Hash()
            {
                if (value is ProceduralPropGroupPrototype proto) return proto;
            }
            return null;
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
        public ushort RandomRotationDegrees { get; } // short
        public ushort RandomPosition { get; } // short

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
