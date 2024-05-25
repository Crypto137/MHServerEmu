using MHServerEmu.Games.GameData.Calligraphy.Attributes;

namespace MHServerEmu.Games.Entities.Inventories
{
    public enum InventoryResult
    {
        Invalid = -1,
        Success = 0,
        NotAttempted = 1,
        UnknownFailure = 2,
        InventoryFull = 3,
        NotRootOwner = 4,
        IsRootOwner = 5,
        InvalidExistingEntityAtDest = 6,
        InvalidSourceEntity = 7,
        SourceEntityAlreadyInAnInventory = 8,
        InvalidStackEntity = 9,
        StackTypeMismatch = 10,
        NotStackable = 11,
        StacksNotSplittable = 12,
        StackCombinePartial = 13,
        InvalidSlotParam = 14,
        SlotExceedsCapacity = 15,
        SlotAlreadyOccupied = 16,
        NotInInventory = 17,
        NotFoundInThisInventory = 18,
        InventoryHasNoOwner = 19,
        SplitParamExceedsStackSize = 20,
        NoAvailableInventory = 21,
        PlayerOwnerNotFound = 22,
        InvalidGame = 23,
        ErrorCreatingNewSplitEntity = 24,
        InvalidReceivingInventory = 25,
        InvalidDestInvContainmentFilters = 26,
        InvalidBound = 27,
        InvalidUnboundItemNotAllowed = 28,
        InvalidPropertyRestriction = 29,
        InvalidCharacterRestriction = 30,
        InvalidItemTypeForCharacter = 31,
        InvalidCostumeForCharacter = 32,
        InvalidTwoOfSameArtifact = 33,
        InvalidNotAnItem = 35,
        InvalidBagItemPreventsPlayerAdds = 36,
        InvalidPlayerCannotMoveIntoThisInventory = 37,
        InvalidPlayerCannotMoveOutOfThisInventory = 38,
        InvalidNotInteractingWithCrafter = 39,
        InvalidNotInteractingWithStash = 40,
        InvalidEquipmentInventoryNotUnlocked = 41,
        InvalidNotTrading = 42,
        InvalidRestrictedByOtherItem = 34,
    };

    [AssetEnum((int)None)]
    public enum InventoryCategory   // Entity/Inventory/Category.type
    {
        None = 0,
        AvatarEquipment = 1,
        BagItem = 2,
        PlayerAdmin = 3,
        PlayerAvatars = 4,
        PlayerCraftingRecipes = 12,
        PlayerGeneral = 5,
        PlayerGeneralExtra = 6,
        PlayerStashAvatarSpecific = 7,
        PlayerStashGeneral = 8,
        PlayerTrade = 10,
        PlayerVendor = 11,
        TeamUpEquipment = 13,
        PlayerStashTeamUpGear = 9,
    }

    [AssetEnum((int)Invalid)]
    public enum InventoryEvent
    {
        Invalid,
        RegionChange,
    }

    [AssetEnum((int)None)]
    public enum InventoryConvenienceLabel
    {
        None = 0,
        AvatarArtifact1 = 1,
        AvatarArtifact2 = 2,
        AvatarArtifact3 = 3,
        AvatarArtifact4 = 4,
        AvatarLegendary = 5,
        AvatarInPlay = 6,
        AvatarLibrary = 7,
        AvatarLibraryHardcore = 8,
        AvatarLibraryLadder = 9,
        TeamUpLibrary = 10,
        TeamUpGeneral = 11,
        Costume = 12,
        CraftingRecipesLearned = 13,
        DEPRECATEDCraftingInProgress = 14,
        CraftingResults = 15,
        DangerRoomScenario = 16,
        General = 17,
        DEPRECATEDPlayerStash = 19,
        Summoned = 20,
        Trade = 21,
        UIItems = 22,
        DeliveryBox = 23,
        ErrorRecovery = 24,
        Controlled = 25,
        VendorBuyback = 26,
        PvP = 27,
        PetItem = 18,
        ItemLink = 28,
        CouponAwards = 29,
        UnifiedStash = 30,
        // Missing in the client
        //AvatarRing = 0
    }

    public enum InventoryMetaDataType : byte
    {
        Invalid,
        Parent,
        Item
    }
}
