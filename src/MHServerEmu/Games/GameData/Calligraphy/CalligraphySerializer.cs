using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    /// <summary>
    /// An implementation of <see cref="GameDataSerializer"/> for Calligraphy prototypes.
    /// </summary>
    public class CalligraphySerializer : GameDataSerializer
    {
        public override void Deserialize(Prototype prototype, PrototypeId dataRef, Stream stream)
        {
            // Set this prototype's id data ref
            prototype.DataRef = dataRef;

            // Deserialize
            using (BinaryReader reader = new(stream))
            {
                // Read Calligraphy prototype file header
                CalligraphyHeader header = new(reader);

                // Temp deserialization
                prototype.DeserializeCalligraphy(reader);

                // Temp hack for property info
                if (prototype is PropertyInfoPrototype propertyInfo)
                    propertyInfo.FillPropertyInfoFields();
            }
        }
    }
}
