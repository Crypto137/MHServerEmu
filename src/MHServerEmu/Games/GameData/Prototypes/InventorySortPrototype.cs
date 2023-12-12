namespace MHServerEmu.Games.GameData.Prototypes
{

    public class InventorySortPrototype : Prototype
    {
        public ulong DisplayName;
        public bool Ascending;
        public bool DisplayInUI;
        public InventorySortPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(InventorySortPrototype), proto); }
    }

    public class InventorySortAlphaPrototype : InventorySortPrototype
    {
        public InventorySortAlphaPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(InventorySortAlphaPrototype), proto); }
    }

    public class InventorySortRarityPrototype : InventorySortPrototype
    {
        public InventorySortRarityPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(InventorySortRarityPrototype), proto); }
    }

    public class InventorySortItemLevelPrototype : InventorySortPrototype
    {
        public InventorySortItemLevelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(InventorySortItemLevelPrototype), proto); }
    }

    public class InventorySortUsableByPrototype : InventorySortPrototype
    {
        public InventorySortUsableByPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(InventorySortUsableByPrototype), proto); }
    }

    public class ItemSortCategoryPrototype : Prototype
    {
        public ItemSortCategoryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemSortCategoryPrototype), proto); }
    }

    public class InventorySortCategoryPrototype : InventorySortPrototype
    {
        public InventorySortCategoryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(InventorySortCategoryPrototype), proto); }
    }

    public class InventorySortSubCategoryPrototype : InventorySortCategoryPrototype
    {
        public InventorySortSubCategoryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(InventorySortSubCategoryPrototype), proto); }
    }

}
