namespace MHServerEmu.Games.GameData
{
    // Based on CalligraphySerializer::getParser

    public enum PrototypeFieldType
    {
        Int8 = 0,
        Int16 = 1,
        Int32 = 2,
        Int64 = 3,                      // Also 27, 28
        Bool = 4,
        Float32 = 8,
        Float64 = 9,
        Enum = 11,
        UnkType12 = 12,                 // Same as 33 and 52
        FunctionPtr = 13,
        AssetRef = 15,
        AssetTypeRef = 16,
        CurveRef = 17,
        UnkType27 = 27,                 // Same as Int64
        UnkType28 = 28,                 // Same as Int64
        Prototype = 31,
        PrototypePtr = 32,
        UnkType33 = 33,                 // Same as 12 and 52
        VectorPrototypeDataRef = 34,
        ListPrototypeDataRef = 35,
        VectorAssetDataRef = 36,
        ListAssetRef = 37,
        ListAssetTypeRef = 38,
        ListBool = 39,
        ListEnum = 40,
        ListInt8 = 41,
        ListInt16 = 42,
        ListInt32 = 43,
        ListInt64 = 44,
        ListFloat32 = 45,
        ListFloat64 = 46,
        ListString = 47,
        ListPrototypePtr = 48,          // "Lists of PrototypePtrs are not parsed as a standard prototype field"
        ListMixin = 49,                 // "Mixin lists are not parsed as a standard prototype field"
        VectorPrototypePtr = 50,        // "Vectors of PrototypePtrs are not parsed as a standard prototype field"
        UnkType51 = 51,
        UnkType52 = 52,                 // Same as 12 and 33
        Vector = 53,
        PropertyId = 54,
        PropertyCollection = 55,        // "Property collections are not parsed as a standard prototype field"
        PropertyList = 56
    }
}
