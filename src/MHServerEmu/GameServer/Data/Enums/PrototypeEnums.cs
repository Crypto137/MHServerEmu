using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.GameServer.Data.Enums
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

    public enum RegionPrototype : ulong
    {
        // Hubs
        NPEAvengersTowerHUBRegion = 9142075282174842340,    // Avengers Tower
        TrainingRoomSHIELDRegion = 12181996598405306634,    // S.H.I.E.L.D. Training Room
        XaviersMansionRegion = 7293929583592937434,         // Xavier's School
        HelicarrierRegion = 13623659297421268224,           // S.H.I.E.L.D. Helicarrier
        AsgardiaRegion = 6777883860663800739,               // Odin's Palace
        GenoshaHUBRegion = 2398160822904886324,             // Hammer Bay
        DangerRoomHubRegion = 13296910602616641976,         // Danger Room
        InvasionSafeAbodeRegion = 17072997612720691719,     // Fury's Toolshed

        // Prologue
        NPERaftRegion = 11602117409128454445,               // Raft Landing Pad

        // Chapter 1
        CH0101HellsKitchenRegion = 10115017851235015611,    // Hell's Kitchen South, Hell's Kitchen North
        CH0104SubwayRegion = 9191439093559269723,           // AbandonedSubway
        CH0105NightclubRegion = 1835346266496899713,        // Nightclub District

        // Chapter 2
        CH0201ShippingYardRegion = 6121022758926621561,     // Shipping Yard
        CH0204Q36AIMLabRegion = 2777924139474164138,        // Hidden A.I.M. Laboratory
        CH0205ConstructionRegion = 5976827590009297814,     // Crab Trap Restaurant
        CH0207TaskmasterRegion = 8702251210467252908,       // Taskmaster Institute
        CH0208CanneryRegion = 14132435072208214366,         // Cannery Row
        CH0209HoodsHideoutRegion = 3240864148892687230,     // Cargo Barge

        // Chapter 3
        CH0301MadripoorRegion = 15546930156792977757,       // Buccaneer Beach, Bamboo Forest, Lowtown
        CH0305ReconPostRegion = 13777669704527323332,       // S.H.I.E.L.D. Recon Post
        CH0307HandTowerRegion = 3712504169451561124,        // Hand Tower

        // Chapter 4
        CH0401LowerEastRegion = 11922318117493283053,       // Lower East Side
        CH0402UpperEastRegion = 7814783688219433201,        // Upper East Side
        CH0410FiskTowerRegion = 4829776495467370741,        // Fisk Tower

        // Chapter 5
        CH0501MutantTownRegion = 13322003842876840585,      // Wretched Slum, Ruined Projects, Morlock Underground
        CH0502MutantWarehouseRegion = 4439163727824297109,  // Invaded Warehouse
        CH0504PurifierChurchRegion = 14943602849314446350,  // Old Trainyard

        // Chapter 6
        CH0601FortStrykerRegion = 16979229927281662823,     // Hunting Grounds, Training Camp, Outer Compound
        CH0605StrykerBunkerRegion = 14933304278036388919,   // Command Bunker

        // Chapter 7
        CH0701SavagelandRegion = 3816293063869929975,       // Dinosaur Jungle, S.H.I.E.L.D. Science Outpost, Mutate Marsh
        CH0707SinisterLabRegion = 15860259876442020487,     // Sinister Lab

        // Chapter 8
        CH0801AIMWeaponFacilityRegion = 7735172603194383419,// A.I.M. Weapon Facility
        CH0802HYDRAIslandRegion = 10124302533162047929,     // Hydra Island
        CH0804LatveriaPCZRegion = 5943200060062505421,      // Doomstadt Outer Village, Doomstadt Inner Village
        CH0808DoomCastleRegion = 11854040468777277783,      // Castle Doom

        // Chapter 9
        CH0901NorwayPCZRegion = 5941813295004653614,        // Fjords of Norway, Ancient Ruin Site
        CH0904SiegePCZRegion = 9690692412445890462,         // Lower Asgard

        // Chapter 10
        MadripoorInvasionRegion = 3163018830455251180,      // Lowtown Warzone
        ATowerInvRegion = 3941087035061182612,              // Avengers Tower (Invaded)
        NYCRooftopInvRegion = 17036392409364308252,         // Hell's Kitchen Rooftops
        XMansionInvRegion = 10782376165327314768,           // Xavier's School (Invaded)
        UpperMadripoorRegionL60 = 9854118440624922758,      // S.W.O.R.D. Landing Zone, S.W.O.R.D. Command Post, Hightown Patrol

        // Terminals
        OpDailyBugleRegionL11To60 = 6538624689610759291,        // Daily Bugle
        DailyGTimesSquareRegionL60 = 15168170443837678687,      // Times Square
        DailyGShockerSubwayRegionL60 = 4751118824917181965,     // Abandoned Subway
        DailyGKPWarehouseRegionL60 = 10639961636982236893,      // Kingpin's Warehouse
        DailyGTaskmasterRegionL60 = 557222731317715831,         // Taskmaster Institute
        DailyGHoodsShipRegionL60 = 16440760549536244379,        // The Hood's Hideout
        DailyGFiskTowerRegionL60 = 9270366941605405365,         // Fisk Tower
        DailyGPurifierChurchRegionL60 = 558746960142673599,     // Church of Purification
        DailyGStrykerBunkerRegionL60 = 4777309101647144454,     // Stryker Command Bunker
        DailyGSinisterLabRegionL60 = 16531055526971909292,      // Sinister Lab
        DailyGAIMFacilityRegionL60 = 3134677553534149649,       // A.I.M. Weapon Facility
        DailyGHYDRAIslandRegionL60 = 5316629336354333727,       // Hydra Island
        DailyGDoomCastleRegionL60 = 13699997676891218767,       // Castle Doom
        DailyGAsgardINSTRegionL60 = 1435000301050209520,        // Odin's Palace
        DailyGHighTownInvasionRegionL60 = 4418386810606266519,  // Skrull Invasion
        DrStrangeTimesSquareRegionL60 = 7200960875039369001,    // Dimensions Collide

        // One-Shot Stories
        BronxZooRegionL60 = 5807424830177093216,                // Bronx Zoo
        HYDRAIslandPartDeuxRegionL60 = 16604626599937322846,    // March to Axis
        WakandaP1RegionL60 = 4913805506445059989,               // Vibranium Mines

        // Challenges
        XManhattanRegion1to60 = 16748618685203816205,           // Midtown Patrol
        CosmicGateRegion = 11861535639759165347,                // Cosmic Trial
        XManhattanRegion60Cosmic = 15044543158919766135,        // Midtown Patrol (Cosmic)
        BrooklynPatrolRegionL60 = 12232311720232100001,         // Industry City Patrol
        BrooklynPatrolRegionL60Cosmic = 9428362085609710367,    // Industry City Patrol (Cosmic)
        UpperMadripoorRegionL60Cosmic = 3770267243209630478,    // Hightown Patrol (Cosmic)
        UltronRaidRegionGreen = 11707449573185231773,           // The Age of Ultron
        HoloSimARegion1to60 = 1851384890999315356,              // S.H.I.E.L.D. Holo-Sim
        XmansionNWSRegionUnbanded = 17669583277812293411,       // X-Defense
        SurturRaidRegionGreen = 943404224811899020,             // Muspelheim Raid
        AxisRaidRegionGreen = 10186536050085467400              // Axis Raid
    }
}
