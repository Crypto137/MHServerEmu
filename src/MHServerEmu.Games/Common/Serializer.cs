using Google.ProtocolBuffers;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Common
{
    public static class Serializer
    {
        /// <summary>
        /// Reads a prototype enum value for the specified class from the stream and converts it to a data ref.
        /// </summary>
        public static PrototypeId ReadPrototypeRef<T>(this CodedInputStream stream) where T : Prototype
        {
            return GameDatabase.DataDirectory.GetPrototypeFromEnumValue<T>((int)stream.ReadRawVarint64());
        }

        /// <summary>
        /// Converts a prototype data ref to an enum value for the specified class and writes it to the stream.
        /// </summary>
        public static void WritePrototypeRef<T>(this CodedOutputStream stream, PrototypeId prototypeId) where T : Prototype
        {
            stream.WriteRawVarint64((ulong)GameDatabase.DataDirectory.GetPrototypeEnumValue<T>(prototypeId));
        }
    }
}
