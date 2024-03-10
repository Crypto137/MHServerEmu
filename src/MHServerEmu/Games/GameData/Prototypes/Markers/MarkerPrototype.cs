using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Resources;

namespace MHServerEmu.Games.GameData.Prototypes.Markers
{
    /// <summary>
    /// Base class for all MarkerPrototypes.
    /// </summary>
    public class MarkerPrototype : Prototype
    {
        public Vector3 Position { get; protected set; }
        public Orientation Rotation { get; protected set; }

        public void ReadMarker(BinaryReader reader)
        {
            Position = reader.ReadVector3();
            Rotation = reader.ReadOrientation();
        }
    }

    public class MarkerFilterPrototype : Prototype
    {
    }

    public class MarkerSetPrototype : Prototype
    {
        public MarkerPrototype[] Markers { get; }

        public MarkerSetPrototype(BinaryReader reader)
        {
            Markers = new MarkerPrototype[reader.ReadInt32()];
            for (int i = 0; i < Markers.Length; i++)
                Markers[i] = ReadMarkerPrototype(reader);
        }

        private MarkerPrototype ReadMarkerPrototype(BinaryReader reader)
        {
            var hash = (ResourcePrototypeHash)reader.ReadUInt32();

            switch (hash)
            {
                case ResourcePrototypeHash.CellConnectorMarkerPrototype:
                    return new CellConnectorMarkerPrototype(reader);
                case ResourcePrototypeHash.DotCornerMarkerPrototype:
                    return new DotCornerMarkerPrototype(reader);
                case ResourcePrototypeHash.EntityMarkerPrototype:
                    return new EntityMarkerPrototype(reader);
                case ResourcePrototypeHash.RoadConnectionMarkerPrototype:
                    return new RoadConnectionMarkerPrototype(reader);
                case ResourcePrototypeHash.ResourceMarkerPrototype:
                    return new ResourceMarkerPrototype(reader);
                case ResourcePrototypeHash.UnrealPropMarkerPrototype:
                    return new UnrealPropMarkerPrototype(reader);

                default:    // Throw an exception if there's a hash for a type we didn't expect
                    throw new NotImplementedException($"Unknown ResourcePrototypeHash {(uint)hash}.");
            }
        }

        public void GetContainedEntities(HashSet<PrototypeId> refs)
        {
            if (Markers.HasValue())
            {
                foreach (var marker in Markers)
                {
                    if ((marker is EntityMarkerPrototype entityMarkerProto) == false) continue;
                    var guid = entityMarkerProto.EntityGuid;
                    if (guid == PrototypeGuid.Invalid) continue;
                    var entityRef = GameDatabase.GetDataRefByPrototypeGuid(guid);
                    if (entityRef == PrototypeId.Invalid) continue;

                    refs.Add(entityRef);
                }
            }
        }

    }
}
