using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData.Resources;

namespace MHServerEmu.Games.GameData.Prototypes.Markers
{
    public class UnrealPropMarkerPrototype : MarkerPrototype
    {
        public string UnrealClassName { get; }
        public string UnrealQualifiedName { get; }
        public string UnrealArchetypeName { get; }

        public UnrealPropMarkerPrototype(BinaryReader reader)
        {
            ProtoNameHash = ResourcePrototypeHash.UnrealPropMarkerPrototype;

            UnrealClassName = reader.ReadFixedString32();
            UnrealQualifiedName = reader.ReadFixedString32();
            UnrealArchetypeName = reader.ReadFixedString32();

            Position = reader.ReadVector3();
            Rotation = reader.ReadVector3();
        }
    }
}
