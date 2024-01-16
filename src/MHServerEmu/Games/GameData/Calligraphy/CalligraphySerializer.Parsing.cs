using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    public partial class CalligraphySerializer
    {
        // Rough early experiments implementing field parsers

        private static Func<BinaryReader, object> GetParser(CalligraphyBaseType baseType, CalligraphyStructureType structureType)
        {
            // We're currently using Calligraphy types here as a temporary solution
            // Probably need some kind of lookup dictionary to avoid unnecessary branching here
            switch (baseType)
            {
                case CalligraphyBaseType.Boolean: return structureType == CalligraphyStructureType.Simple ? ParseBool : ParseListBool;
                case CalligraphyBaseType.Double: return structureType == CalligraphyStructureType.Simple ? ParseDouble : ParseListDouble;
                case CalligraphyBaseType.Long: return structureType == CalligraphyStructureType.Simple ? ParseInt64 : ParseListInt64;
                case CalligraphyBaseType.RHStruct: return structureType == CalligraphyStructureType.Simple ? ParseRHStruct : ParseListRHStruct;
                default: return structureType == CalligraphyStructureType.Simple ? ParseUInt64 : ParseListUInt64;
            }
        }

        // TODO: FieldParserParams
        // TODO: parseValue()
        // TODO: Maybe move this to a helper static class?

        private static object ParseBool(BinaryReader reader) => Convert.ToBoolean(reader.ReadUInt64());
        private static object ParseInt64(BinaryReader reader) => reader.ReadInt64();
        private static object ParseUInt64(BinaryReader reader) => reader.ReadUInt64();
        private static object ParseDouble(BinaryReader reader) => reader.ReadDouble();
        private static object ParseRHStruct(BinaryReader reader) => new Prototype(reader);

        private static object ParseListBool(BinaryReader reader) => ParseCollection(reader, ParseBool);
        private static object ParseListInt64(BinaryReader reader) => ParseCollection(reader, ParseInt64);
        private static object ParseListUInt64(BinaryReader reader) => ParseCollection(reader, ParseUInt64);
        private static object ParseListDouble(BinaryReader reader) => ParseCollection(reader, ParseDouble);
        private static object ParseListRHStruct(BinaryReader reader) => ParseCollection(reader, ParseRHStruct);

        private static object ParseCollection(BinaryReader reader, Func<BinaryReader, object> itemParser)
        {
            var values = new object[reader.ReadInt16()];
            for (int i = 0; i < values.Length; i++)
                values[i] = itemParser(reader);
            return values;
        }
    }
}
