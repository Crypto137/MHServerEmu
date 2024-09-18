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
        public const BlueprintId Power                      = (BlueprintId)6670986634407775621;     // Powers/Blueprints/Power.defaults
        public const BlueprintId DiamondFormActivatePower   = (BlueprintId)18066325974134561036;    // Powers/Blueprints/Keywords/DiamondFormActivatePower.blueprint
        public const BlueprintId Mental                     = (BlueprintId)720980541349630335;      // Powers/Blueprints/Keywords/Mental.blueprint
        public const BlueprintId Item                       = (BlueprintId)10622579046198020830;    // Entity/Items/Item.blueprint
        public const BlueprintId Artifact                   = (BlueprintId)1626168533479592044;     // Entity/Items/Artifacts/Artifact.blueprint
        public const BlueprintId Costume                    = (BlueprintId)10774581141289766864;    // Entity/Items/Costumes/Costume.blueprint 
        public const BlueprintId CraftingElement            = (BlueprintId)12132161488226819436;    // Entity/Items/Crafting/Ingredients/ElementProtos/CraftingElement.defaults
        public const BlueprintId PetItem                    = (BlueprintId)10446998100463455761;    // Entity/Items/Pets/PetItem.blueprint 
        public const BlueprintId Relic                      = (BlueprintId)8255039456199642606;     // Entity/Items/Relics/Relic.blueprint 
        public const BlueprintId RingBlueprint              = (BlueprintId)17978417066261680389;    // Entity/Items/Rings/RingBlueprint.blueprint 
        public const BlueprintId RunewordGlyphParent        = (BlueprintId)8882767421611578730;     // Entity/Items/Runewords/RunewordGlyphParen.blueprint 
        public const BlueprintId Region                     = (BlueprintId)1677652504589371837;     // Regions/Region.blueprint
    }

    #endregion
}
