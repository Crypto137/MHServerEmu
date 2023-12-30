namespace MHServerEmu.Games.GameData.Prototypes
{
    public class InventorySortPrototype : Prototype
    {
        public ulong DisplayName { get; protected set; }
        public bool Ascending { get; protected set; }
        public bool DisplayInUI { get; protected set; }
    }

    public class InventorySortAlphaPrototype : InventorySortPrototype
    {
    }

    public class InventorySortRarityPrototype : InventorySortPrototype
    {
    }

    public class InventorySortItemLevelPrototype : InventorySortPrototype
    {
    }

    public class InventorySortUsableByPrototype : InventorySortPrototype
    {
    }

    public class ItemSortCategoryPrototype : Prototype
    {
    }

    public class InventorySortCategoryPrototype : InventorySortPrototype
    {
    }

    public class InventorySortSubCategoryPrototype : InventorySortCategoryPrototype
    {
    }
}
