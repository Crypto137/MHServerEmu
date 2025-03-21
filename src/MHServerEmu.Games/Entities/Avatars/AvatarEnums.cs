using MHServerEmu.Games.GameData.Calligraphy.Attributes;

namespace MHServerEmu.Games.Entities.Avatars
{
    [AssetEnum((int)Invalid)]
    public enum AvatarStat
    {
        Invalid = 0,
        Durability = 1,
        Energy = 2,
        Fighting = 3,
        Intelligence = 4,
        Speed = 5,
        Strength = 6,
    }

    [AssetEnum((int)Invalid)]
    public enum AvatarMode
    {
        Invalid = -1,
        Normal = 0,
        Hardcore = 1,
        Ladder = 2,
    }

    public enum AvatarUnlockType : long
    {
        None,
        Starter,
        Type2,
        Type3,
        Type4,
        Type5,
        Type6
    }

    public enum DeathReleaseRequestType : uint
    {
        Checkpoint,
        Town,
        Corpse,
        Ally,
        NumRequestTypes
    }

    public enum AbilitySlotOpValidateResult
    {
        Valid,
        PowerNotUsableByAvatar,
        PowerNotActive,
        PowerSlotMismatch,
        PowerNotUnlocked,
        SwapSameSlot,
        ItemNotEquipped,
        AvatarIsInCombat,
        GenericError
    }

    public enum CanToggleTalentResult
    {
        Success,
        InCombat,
        RestrictiveCondition,
        LevelRequirement,
        GenericError
    }

    public enum CanSetInfinityRankResult
    {
        Success,
        ErrorGeneric,
        ErrorLevelRequirement,
        ErrorInsufficientPoints,
        Error4,
        ErrorCannotRemove,
        ErrorPrerequisiteRequirement
    }

    public enum CanSetOmegaRankResult
    {
        Success,
        ErrorGeneric,
        ErrorLevelRequirement,
        ErrorInsufficientPoints,
        Error4,
        ErrorCannotRemove,
        ErrorPrerequisiteRequirement
    }

    public enum AvatarPrototypeId : ulong
    {
        Angela = 13124420519155930214,
        AntMan = 18132742377931805821,
        Beast = 17049937000059835405,
        BlackBolt = 6223810743151629740,
        BlackCat = 12534955053251630387,
        BlackPanther = 456629384788186861,
        BlackWidow = 10144061740894656037,
        Blade = 6139368802772849654,
        Cable = 16209821644389487605,
        CaptainAmerica = 10617813376954079152,
        Carnage = 1644838416155284687,
        Colossus = 11118079530304738681,
        Cyclops = 6572304655153960187,
        Daredevil = 11615789429029934510,
        Deadpool = 1660250039076459846,
        DoctorStrange = 9703681217466931069,
        DrDoom = 17750839636937086083,
        Elektra = 7937405352416253158,
        EmmaFrost = 412966192105395660,
        Gambit = 6448072532466209906,
        GhostRider = 9255468350667101753,
        GreenGoblin = 7015297277250377354,
        Hawkeye = 3597588143900726508,
        Hulk = 8294172517850551218,
        HumanTorch = 6996312464889026103,
        Iceman = 10305433616491287659,
        InvisibleWoman = 12259390671146653669,
        IronFist = 13212972008131138924,
        IronMan = 421791326977791218,
        JeanGrey = 12460013083760072019,
        Juggernaut = 13061049321858668090,
        KittyPryde = 1172420421674735191,
        Loki = 14419064055405876141,
        LukeCage = 14354783269825877311,
        Magik = 15743789998840419335,
        Magneto = 8755692150967833833,
        MoonKnight = 4196473162086422076,
        MrFantastic = 17591756837553313434,
        MsMarvel = 17510769099164947813,
        NickFury = 2198068880456357225,
        Nightcrawler = 18152689483875489544,
        Nova = 72066007482110898,
        Psylocke = 724683970231539048,
        Punisher = 4616550151502632300,
        RocketRaccoon = 8842376528969668459,
        Rogue = 6514650100102861856,
        ScarletWitch = 13840162506148812555,
        SheHulk = 12394659164528645362,
        SilverSurfer = 11103546526429026090,
        Spiderman = 9378552423541970369,
        SquirrelGirl = 13109043516307281699,
        Starlord = 5394058310044226921,
        Storm = 6791894920589808691,
        Taskmaster = 13583994425176888893,
        Thing = 14005962908529333272,
        Thor = 7949859047165531067,
        Ultron = 1977663415972730018,
        Venom = 15662110445428479011,
        Vision = 3980807850392229014,
        WarMachine = 8651855704152086045,
        WinterSoldier = 15115104590402361225,
        Wolverine = 13237838511939982809,
        X23 = 7643048032188437211
    }
}
