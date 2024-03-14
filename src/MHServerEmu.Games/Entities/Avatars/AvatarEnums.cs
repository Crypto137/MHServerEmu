using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Entities.Avatars
{
    public static class AvatarPrototypeEnumExtensions
    {
        public static HardcodedAvatarEntityId ToEntityId(this AvatarPrototypeId prototype)
        {
            return Enum.Parse<HardcodedAvatarEntityId>(Enum.GetName(prototype));
        }

        public static PrototypeId ToAvatarPrototypeId(this HardcodedAvatarEntityId avatarEntityId)
        {
            return (PrototypeId)Enum.Parse<AvatarPrototypeId>(Enum.GetName(avatarEntityId));
        }

        public static HardcodedAvatarPropertyCollectionReplicationId ToPropertyCollectionReplicationId(this HardcodedAvatarEntityId avatarEntityId)
        {
            return Enum.Parse<HardcodedAvatarPropertyCollectionReplicationId>(Enum.GetName(avatarEntityId));
        }

        public static HardcodedAvatarPropertyCollectionReplicationId ToPropertyCollectionReplicationId(this AvatarPrototypeId prototype)
        {
            return Enum.Parse<HardcodedAvatarPropertyCollectionReplicationId>(Enum.GetName(prototype));
        }
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

    public enum HardcodedAvatarEntityId : ulong
    {
        Nova = 14646213,
        EmmaFrost = 14646214,
        IronMan = 14646215,
        BlackPanther = 14646216,
        Psylocke = 14646217,
        KittyPryde = 14646218,
        Carnage = 14646219,
        Deadpool = 14646220,
        Ultron = 14646221,
        NickFury = 14646222,
        Hawkeye = 14646223,
        Vision = 14646224,
        MoonKnight = 14646225,
        Punisher = 14646226,
        Starlord = 14646227,
        Blade = 14646228,
        BlackBolt = 14646229,
        Gambit = 14646230,
        Rogue = 14646231,
        Cyclops = 14646232,
        Storm = 14646233,
        HumanTorch = 14646234,
        GreenGoblin = 14646235,
        X23 = 14646236,
        Elektra = 14646237,
        Thor = 14646238,
        Hulk = 14646239,
        WarMachine = 14646240,
        Magneto = 14646241,
        RocketRaccoon = 14646242,
        GhostRider = 14646243,
        Spiderman = 14646244,
        DoctorStrange = 14646245,
        BlackWidow = 14646246,
        Iceman = 14646247,
        CaptainAmerica = 14646248,
        SilverSurfer = 14646249,
        Colossus = 14646250,
        Daredevil = 14646251,
        InvisibleWoman = 14646252,
        SheHulk = 14646253,
        JeanGrey = 14646254,
        BlackCat = 14646255,
        Juggernaut = 14646256,
        SquirrelGirl = 14646257,
        Angela = 14646258,
        IronFist = 14646259,
        Wolverine = 14646260,
        Taskmaster = 14646261,
        ScarletWitch = 14646262,
        Thing = 14646263,
        LukeCage = 14646264,
        Loki = 14646265,
        WinterSoldier = 14646266,
        Venom = 14646267,
        Magik = 14646268,
        Cable = 14646269,
        Beast = 14646270,
        MsMarvel = 14646271,
        MrFantastic = 14646272,
        DrDoom = 14646273,
        AntMan = 14646274,
        Nightcrawler = 14646275
    }

    public enum HardcodedAvatarPropertyCollectionReplicationId : ulong
    {
        Nova = 9078336,
        EmmaFrost = 9078338,
        IronMan = 9078340,
        BlackPanther = 9078342,
        Psylocke = 9078344,
        KittyPryde = 9078346,
        Carnage = 9078348,
        Deadpool = 9078350,
        Ultron = 9078352,
        NickFury = 9078354,
        Hawkeye = 9078356,
        Vision = 9078358,
        MoonKnight = 9078360,
        Punisher = 9078362,
        Starlord = 9078364,
        Blade = 9078366,
        BlackBolt = 9078368,
        Gambit = 9078370,
        Rogue = 9078372,
        Cyclops = 9078374,
        Storm = 9078376,
        HumanTorch = 9078378,
        GreenGoblin = 9078380,
        X23 = 9078382,
        Elektra = 9078384,
        Thor = 9078386,
        Hulk = 9078388,
        WarMachine = 9078390,
        Magneto = 9078392,
        RocketRaccoon = 9078394,
        GhostRider = 9078396,
        Spiderman = 9078398,
        DoctorStrange = 9078400,
        BlackWidow = 9078402,
        Iceman = 9078404,
        CaptainAmerica = 9078406,
        SilverSurfer = 9078408,
        Colossus = 9078410,
        Daredevil = 9078412,
        InvisibleWoman = 9078414,
        SheHulk = 9078416,
        JeanGrey = 9078418,
        BlackCat = 9078420,
        Juggernaut = 9078422,
        SquirrelGirl = 9078424,
        Angela = 9078426,
        IronFist = 9078428,
        Wolverine = 9078430,
        Taskmaster = 9078432,
        ScarletWitch = 9078434,
        Thing = 9078436,
        LukeCage = 9078438,
        Loki = 9078440,
        WinterSoldier = 9078442,
        Venom = 9078444,
        Magik = 9078446,
        Cable = 9078448,
        Beast = 9078450,
        MsMarvel = 9078452,
        MrFantastic = 9078454,
        DrDoom = 9078456,
        AntMan = 9078458,
        Nightcrawler = 9078460
    }
}
