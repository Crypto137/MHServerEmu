namespace MHServerEmu.GameServer.GameData.Gpak
{
    // Type enums

    public enum CalligraphyValueType : byte
    {
        A = 0x41,   // asset
        B = 0x42,   // bool
        C = 0x43,   // curve
        D = 0x44,   // double
        L = 0x4c,   // long
        P = 0x50,   // prototype
        R = 0x52,   // ??? (embedded / anonymous prototype)
        S = 0x53,   // string
        T = 0x54    // type
    }

    public enum CalligraphyContainerType : byte
    {
        L = 0x4c,   // list (A P R T only)
        S = 0x53    // single
    }


    // Enums for specific data for easy access

    public enum BlueprintId : ulong
    {
        WorldEntity = 7901305308382563236,
        ThrowablePowerProp = 8706319841384272336,
        ThrowableRestorePowerProp = 1483936524176856276,
        Costume = 10774581141289766864,
        RegionConnectionTarget = 3341826552978477172,
        RegionConnectionNode = 1686863052116070291,
        BoxBounds = 17017764287313678816,
        SphereBounds = 8815641071010845470,
        CapsuleBounds = 3200633985132925828,
        ObjectSmall = 17525629558829421089,
        // Keywords
        Power = 6670986634407775621,
        Physical = 12758986785542509147,
        Mental = 720980541349630335,
        DiamondFormActivatePower = 18066325974134561036,
    }

    public enum FieldId : ulong
    {
        // WorldEntity
        UnrealClass = 9963296804083405606,
        Bounds = 12016533011036705044,
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
