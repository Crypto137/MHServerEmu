namespace MHServerEmu.Games.GameData.Calligraphy
{
    // Type enums

    public enum CalligraphyValueType : byte
    {
        Asset = 0x41,       // A (Id reference to an asset)
        Boolean = 0x42,     // B (Stored as a UInt64)
        Curve = 0x43,       // C (Id reference to a curve)
        Double = 0x44,      // D (For all floating point values)
        Long = 0x4c,        // L (For all integer values)
        Prototype = 0x50,   // P (Id reference to another prototype)
        RHStruct = 0x52,    // R (Embedded prototype without an id, the name is mentioned in EntitySelectorActionPrototype::Validate)
        String = 0x53,      // S (Id reference to a localized string)
        Type = 0x54         // T (Id reference to an AssetType)
    }

    public enum CalligraphyContainerType : byte
    {
        Simple = 0x53,      // Simple
        List = 0x4c         // List (only for assets, prototypes, rhstructs, and types)
    }

    // Enums for specific data for easy access

    /// <summary>
    /// Represents a hardcoded default prototype id.
    /// </summary>
    public enum DefaultPrototypeId : ulong
    {
        WorldEntity = 7901305308382563236,
        ThrowableProp = 14997899060839977779,
        ThrowableSmartProp = 13025272806030579034,
        DestructibleProp = 18375929633378932151,
        ThrowablePowerProp = 8706319841384272336,
        ThrowableRestorePowerProp = 1483936524176856276,
        Costume = 10774581141289766864,
        RegionConnectionTarget = 3341826552978477172,
        RegionConnectionNode = 1686863052116070291,
        BoxBounds = 17017764287313678816,
        SphereBounds = 8815641071010845470,
        CapsuleBounds = 3200633985132925828,
        ObjectSmall = 17525629558829421089,
        NPCTemplateHub = 1884494645036913959,
        // Keywords
        Power = 6670986634407775621,
        Physical = 12758986785542509147,
        Mental = 720980541349630335,
        DiamondFormActivatePower = 18066325974134561036,
    }

    /// <summary>
    /// Represents a hardcoded prototype field id.
    /// </summary>
    public enum FieldId : ulong
    {
        // WorldEntity
        UnrealClass = 9963296804083405606,
        Bounds = 12016533011036705044,
        SnapToFloorOnSpawn = 5130007170241074758,
        PreInteractPower = 9600084259269121367,
        // Costume
        CostumeUnrealClass = 3331018908052953682,
        // RegionConnectionTargetPrototype
        Cell = 16197009792217650425,
        Region = 16646150204029081053,
        Name = 3066023917589763322,
        Entity = 3466157296571782646,
        Area = 5368244907863250162,
        // RegionConnectionNode
        Target = 11933919116406428927,
        Origin = 814438190827050240,
        // BoxBounds
        Height = 3574539659527919422,
        // SphereBounds
        Radius = 7918915620597274763,
        // CapsuleBounds
        HeightFromCenter = 12857512594432138519,
        // Power
        AnimationTimeMS = 185983721281754809,
        Keywords = 17189444542781133794,
    }
}
