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
        R = 0x52,   // ??? (recursion?)
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
        Costume = 10774581141289766864
    }

    public enum FieldId : ulong
    {
        UnrealClass = 9963296804083405606,
        CostumeUnrealClass = 3331018908052953682,
    }
}
