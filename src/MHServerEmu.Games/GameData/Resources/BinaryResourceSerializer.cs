using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.Resources
{
    /// <summary>
    /// An implementation of <see cref="GameDataSerializer"/> for resource prototypes.
    /// </summary>
    public class BinaryResourceSerializer : GameDataSerializer
    {
        public override void Deserialize(Prototype prototype, PrototypeId dataRef, Stream stream)
        {
            // Set this prototype's id data ref
            prototype.DataRef = dataRef;

            // Deserialize
            using (BinaryReader reader = new(stream))
            {
                // Read resource header
                BinaryResourceHeader header = new(reader);

                // Deserialize using the IBinaryResource interface
                IBinaryResource binaryResource = (IBinaryResource)prototype;
                binaryResource.Deserialize(reader);
            }
        }
    }
}
