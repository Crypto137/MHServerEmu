namespace MHServerEmu.Games.GameData.Calligraphy
{
    #region Enums

    public enum CalligraphyBaseType : byte
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

    public enum CalligraphyStructureType : byte
    {
        Simple = 0x53,      // Simple
        List = 0x4c         // List (only for assets, prototypes, rhstructs, and types)
    }

    // Bit field enums

    [Flags]
    public enum CurveRecordFlags : byte
    {
        None        = 0
        // Although curve records do have a field for flags, it's 0 for all records
    }

    [Flags]
    public enum AssetTypeRecordFlags : byte
    {
        None        = 0,
        Protected   = 1 << 0    // AssetDirectory::AssetTypeIsProtected()
    }

    [Flags]
    public enum AssetValueFlags : byte
    {
        None        = 0,
        Protected   = 1 << 0    // AssetType::AssetIsProtected()
    }

    [Flags]
    public enum BlueprintRecordFlags : byte
    {
        None        = 0,
        Protected   = 1 << 0    // DataDirectory::BlueprintIsProtected()
    }

    [Flags]
    public enum PrototypeRecordFlags : byte
    {
        None        = 0,
        Abstract    = 1 << 0,   // DataDirectory::PrototypeIsAbstract()
        Protected   = 1 << 1,   // DataDirectory::PrototypeIsProtected()
        EditorOnly  = 1 << 2,   // DataDirectory::isEditorOnlyByClassId(), seems to be set for NaviFragmentPrototype only
    }

    #endregion

    #region Constants

    /// <summary>
    /// Contains constants for quick access to blueprints.
    /// </summary>
    public static class HardcodedBlueprints
    {
        public const BlueprintId Power                      = (BlueprintId)6670986634407775621;     // Powers/Blueprints/Power.blueprint
        public const BlueprintId Item                       = (BlueprintId)10622579046198020830;    // Entity/Items/Item.blueprint
        public const BlueprintId Costume                    = (BlueprintId)10774581141289766864;    // Entity/Items/Costumes/Costume.blueprint 
        public const BlueprintId LootTable                  = (BlueprintId)5508985158461163528;     // Loot/Table.blueprint
        public const BlueprintId Region                     = (BlueprintId)1677652504589371837;     // Regions/Region.blueprint
        public const BlueprintId Agent                      = (BlueprintId)13439488624722906428;    // Entity/Characters/Agent.blueprint
        public const BlueprintId Selector                   = (BlueprintId)8623625140757141920;     // Populations/Blueprints/EntitySelector/EntitySelector.blueprint 
    }

    #endregion
}
