using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    public partial class CalligraphySerializer
    {
        // Rough early experiments implementing field parsers

        private static Func<FieldParserParams, object> GetParser(CalligraphyBaseType baseType, CalligraphyStructureType structureType)
        {
            // We're currently using Calligraphy types here as a temporary solution
            // Probably need some kind of lookup dictionary to avoid unnecessary branching here
            switch (baseType)
            {
                case CalligraphyBaseType.Boolean:   return structureType == CalligraphyStructureType.Simple ? ParseBool : ParseListBool;
                case CalligraphyBaseType.Double:    return structureType == CalligraphyStructureType.Simple ? ParseDouble : ParseListDouble;
                case CalligraphyBaseType.Long:      return structureType == CalligraphyStructureType.Simple ? ParseInt64 : ParseListInt64;
                case CalligraphyBaseType.RHStruct:  return structureType == CalligraphyStructureType.Simple ? ParseRHStruct : ParseListRHStruct;
                default: return structureType == CalligraphyStructureType.Simple ? ParseUInt64 : ParseListUInt64;
            }
        }

        // TODO: parseValue()
        // TODO: Maybe move this to a helper static class?

        private static object ParseBool(FieldParserParams @params) => Convert.ToBoolean(@params.Reader.ReadUInt64());
        private static object ParseInt64(FieldParserParams @params) => @params.Reader.ReadInt64();
        private static object ParseUInt64(FieldParserParams @params) => @params.Reader.ReadUInt64();
        private static object ParseDouble(FieldParserParams @params) => @params.Reader.ReadDouble();
        private static object ParseRHStruct(FieldParserParams @params) => new Prototype(@params.Reader);

        private static object ParseListBool(FieldParserParams @params) => ParseCollection(@params, ParseBool);
        private static object ParseListInt64(FieldParserParams @params) => ParseCollection(@params, ParseInt64);
        private static object ParseListUInt64(FieldParserParams @params) => ParseCollection(@params, ParseUInt64);
        private static object ParseListDouble(FieldParserParams @params) => ParseCollection(@params, ParseDouble);
        private static object ParseListRHStruct(FieldParserParams @params) => ParseCollection(@params, ParseRHStruct);

        private static object ParseCollection(FieldParserParams @params, Func<FieldParserParams, object> itemParser)
        {
            var reader = @params.Reader;

            var values = new object[reader.ReadInt16()];
            for (int i = 0; i < values.Length; i++)
                values[i] = itemParser(@params);
            return values;
        }

        /// <summary>
        /// Contains parameters for field parsing methods.
        /// </summary>
        private readonly struct FieldParserParams
        {
            public BinaryReader Reader { get; }
            public System.Reflection.PropertyInfo FieldInfo { get; }
            public Prototype OwnerPrototype { get; }
            public Blueprint OwnerBlueprint { get; }
            public BlueprintMemberInfo BlueprintMemberInfo { get; }

            public FieldParserParams(BinaryReader reader, System.Reflection.PropertyInfo fieldInfo, Prototype ownerPrototype,
                Blueprint ownerBlueprint, BlueprintMemberInfo blueprintMemberInfo)
            {
                Reader = reader;
                FieldInfo = fieldInfo;
                OwnerPrototype = ownerPrototype;
                OwnerBlueprint = ownerBlueprint;
                BlueprintMemberInfo = blueprintMemberInfo;
            }
        }
    }
}
