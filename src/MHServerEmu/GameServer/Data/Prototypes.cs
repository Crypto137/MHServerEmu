using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.GameServer.Data
{
    public enum AvatarPrototype : ulong
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
        CaptainMarvel = 17510769099164947813,
        Carnage = 1644838416155284687,
        Colossus = 11118079530304738681,
        Cyclops = 6572304655153960187,
        Daredevil = 11615789429029934510,
        Deadpool = 1660250039076459846,
        DoctorDoom = 17750839636937086083,
        DoctorStrange = 9703681217466931069,
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
        MisterFantastic = 17591756837553313434,
        MoonKnight = 4196473162086422076,
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
        SpiderMan = 9378552423541970369,
        SquirrelGirl = 13109043516307281699,
        StarLord = 5394058310044226921,
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

    public enum RegionPrototype : ulong
    {
        // Hubs
        AvengersTower = 9142075282174842340,
        ShieldTrainingRoom = 12181996598405306634,
        XaviersSchool = 7293929583592937434,
        ShieldHelicarrier = 13623659297421268224,
        OdinsPalace = 6777883860663800739,
        HammerBay = 2398160822904886324,
        DangerRoom = 13296910602616641976,
        FurysToolshed = 17072997612720691719,

        // Prologue
        RaftLandingPad = 11602117409128454445,

        // Chapter 1
        HellsKitchenSouth = 10115017851235015611,
        HellsKitchenNorth = 10115017851235015611,
        AbandonedSubway = 9191439093559269723,
        NightclubDistrict = 1835346266496899713,

        // Chapter 2
        ShippingYard = 6121022758926621561,
        HiddenAimLaboratory = 2777924139474164138,
        CrabTrapRestaurant = 5976827590009297814,
        TaskmasterInstitute = 8702251210467252908,
        CanneryRow = 14132435072208214366,
        CargoBarge = 3240864148892687230,

        // Chapter 3
        BuccaneerBeach = 15546930156792977757,
        BambooForest = 15546930156792977757,
        Lowtown = 15546930156792977757,
        ShieldReconPost = 13777669704527323332,
        HandTower = 3712504169451561124,

        // Chapter 4
        LowerEastSide = 11922318117493283053,
        UpperEastSide = 7814783688219433201,
        FiskTower = 4829776495467370741,

        // Chapter 5
        WretchedSlum = 13322003842876840585,
        InvadedWarehouse = 4439163727824297109,
        RuinedProjects = 13322003842876840585,
        MorlockUnderground = 13322003842876840585,
        OldTrainyard = 14943602849314446350,

        // Chapter 6
        HuntingGrounds = 16979229927281662823,
        TrainingCamp = 16979229927281662823,
        OuterCompound = 16979229927281662823,
        CommandBunker = 14933304278036388919,

        // Chapter 7
        DinosaurJungle = 3816293063869929975,
        ShieldScienceOutpost = 3816293063869929975,
        MutateMarsh = 3816293063869929975,
        SinisterLab = 15860259876442020487,

        // Chapter 8
        AimWeaponFacility = 7735172603194383419,
        HydraIsland = 10124302533162047929,
        DoomstadtOuterVillage = 5943200060062505421,
        DoomstadtInnerVillage = 5943200060062505421,
        CastleDoom = 11854040468777277783,

        // Chapter 9
        FjordsOfNorway = 5941813295004653614,
        AncientRuinSite = 5941813295004653614,
        LowerAsgard = 9690692412445890462,

        // Chapter 10
        LowtownWarzone = 3163018830455251180,
        AvengersTowerInvaded = 3941087035061182612,
        HellsKitchenRooftops = 17036392409364308252,
        XaviersSchoolInvaded = 10782376165327314768,
        SwordLandingZone = 9854118440624922758,
        SwordCommandPost = 9854118440624922758,

        // Terminals
        DailyBugleTerminal = 6538624689610759291,
        TimesSquareTerminal = 15168170443837678687,
        AbandonedSubwayTerminal = 4751118824917181965,
        KingpinsWarehouseTerminal = 10639961636982236893,
        TaskmasterInstituteTerminal = 557222731317715831,
        TheHoodsHideoutTerminal = 16440760549536244379,
        FiskTowerTerminal = 9270366941605405365,
        ChurchOfPurificationTerminal = 558746960142673599,
        StrykerCommandBunkerTerminal = 4777309101647144454,
        SinisterLabTerminal = 16531055526971909292,
        AimWeaponFacilityTerminal = 3134677553534149649,
        HydraIslandTerminal = 5316629336354333727,
        CastleDoomTerminal = 13699997676891218767,
        OdinsPalaceTerminal = 1435000301050209520,
        SkrullInvasionTerminal = 4418386810606266519,
        DimensionsCollideTerminal = 7200960875039369001,

        // One Shot Stories
        BronxZoo = 5807424830177093216,
        MarchToAxis = 16604626599937322846,
        VibraniumMines = 4913805506445059989,

        // Challenges
        MidtownPatrol = 16748618685203816205,
        CosmicTrial = 11861535639759165347,
        MidtownPatrolCosmic = 15044543158919766135,
        IndustryCityPatrol = 12232311720232100001,
        IndustryCityPatrolCosmic = 9428362085609710367,
        HightownPatrol = 9854118440624922758,
        HightownPatrolCosmic = 3770267243209630478,
        TheAgeOfUltron = 11707449573185231773,
        ShieldHoloSim = 1851384890999315356,
        XDefense = 17669583277812293411,
        MuspelheimRaid = 943404224811899020,
        AxisRaid = 10186536050085467400
    }
}
