using MHServerEmu.Games.GameData.Calligraphy.Attributes;

namespace MHServerEmu.Games.Regions
{
    // TODO: remove hardcoded prototype ids and combine these two enums

    [AssetEnum((int)Green)]
    public enum DifficultyTier
    {
        Green,
        Red,
        Cosmic
    }

    public enum DifficultyTierPrototypeId : ulong
    {
        Normal = 18016845980090109785,
        Heroic = 7540373722300157771,
        Superheroic = 586640101754933627,   // interpreted as Cosmic by the 1.52 client
        Cosmic = 1087474643293441873,
        Omega1 = 424700179461639950
    }

    public enum RegionPrototypeId : ulong
    {
        // Hubs
        AvengersTowerHUBRegion = 14599574127156009346,      // Avengers Tower (original)
        NPEAvengersTowerHUBRegion = 9142075282174842340,    // Avengers Tower
        TrainingRoomSHIELDRegion = 12181996598405306634,    // S.H.I.E.L.D. Training Room
        XaviersMansionRegion = 7293929583592937434,         // Xavier's School
        HelicarrierRegion = 13623659297421268224,           // S.H.I.E.L.D. Helicarrier
        AsgardiaRegion = 6777883860663800739,               // Odin's Palace
        GenoshaHUBRegion = 2398160822904886324,             // Hammer Bay
        DangerRoomHubRegion = 13296910602616641976,         // Danger Room
        InvasionSafeAbodeRegion = 17072997612720691719,     // Fury's Toolshed

        // Prologue
        TutorialBankRegion = 15109540093298418681,
        TimesSquareTutorialRegion = 13422564811632352998,
        NPERaftRegion = 11602117409128454445,               // Raft Landing Pad

        // Chapter 1
        CH0101HellsKitchenRegion = 10115017851235015611,    // Hell's Kitchen South, Hell's Kitchen North
        CH0102PowerPlantRegion = 3515564814947916554,
        CH0103NYPDRegion = 9428632312349464698,
        CH0104SubwayRegion = 9191439093559269723,           // AbandonedSubway
        CH0105NightclubRegion = 1835346266496899713,        // Nightclub District
        CH0106KPWarehouseRegion = 11347858582155828080,

        // Chapter 2
        CH0201ShippingYardRegion = 6121022758926621561,     // Shipping Yard
        CH0202HoodSightingContainerRegion = 11668761448997789490,
        CH0203RhinoBargeRegion = 10386701194519845514,
        CH0204Q36AIMLabRegion = 2777924139474164138,        // Hidden A.I.M. Laboratory
        CH0205ConstructionRegion = 5976827590009297814,     // Crab Trap Restaurant
        CH0206TaskmasterVHSTapeConstructionRegion = 3431042234570515153,
        CH0207TaskmasterRegion = 8702251210467252908,       // Taskmaster Institute
        CH0208CanneryRegion = 14132435072208214366,         // Cannery Row
        CH0209HoodsHideoutRegion = 3240864148892687230,     // Cargo Barge

        // Chapter 3
        CH0301MadripoorRegion = 15546930156792977757,       // Buccaneer Beach, Bamboo Forest, Lowtown
        CH0302HydraOutpostRegion = 1829085176078803532,
        CH0303WatermillRegion = 3500476209685602963,
        CH0304PoisonGladeRegion = 18359979659955348623,
        CH0305ReconPostRegion = 13777669704527323332,       // S.H.I.E.L.D. Recon Post
        CH0306PrincessBarRegion = 17486730436248543811,
        CH0307HandTowerRegion = 3712504169451561124,        // Hand Tower

        // Chapter 4
        CH0401LowerEastRegion = 11922318117493283053,       // Lower East Side
        CH0402UpperEastRegion = 7814783688219433201,        // Upper East Side
        CH0403MGHStorageRegion = 3315074099029612874,
        CH0404MGHFactoryRegion = 16983916443208784206,
        CH0405WaxMuseumRegion = 18416219930763860231,       // Wax Museum
        CH0406SubwayRegion = 17364482227990173623,
        CH0407NYPDRooftopRegion = 15623473767309973985,
        CH0408MaggiaRestaurantRegion = 4986534524151667661, // Maggia Restaurant
        CH0409MoloidRegion = 9207898269786445731,
        CH0410FiskTowerRegion = 4829776495467370741,        // Fisk Tower

        // Chapter 5
        CH0501MutantTownRegion = 13322003842876840585,      // Wretched Slum, Ruined Projects, Morlock Underground
        CH0502MutantWarehouseRegion = 4439163727824297109,  // Invaded Warehouse
        CH0503SupervillainRecCenterRegion = 5817790306607898371,
        CH0504PurifierChurchRegion = 14943602849314446350,  // Old Trainyard

        // Chapter 6
        CH0601FortStrykerRegion = 16979229927281662823,     // Hunting Grounds, Training Camp, Outer Compound
        CH0602DeepCavernRegion = 17481886704038320822,
        CH0603CircusSideshowRegion = 1457545689465560201,
        CH0604AIMWeaponsLabRegion = 11677399496238373886,
        CH0605StrykerBunkerRegion = 14933304278036388919,   // Command Bunker
        CH0606MagnetoBunkerRegion = 3352424872722570255,

        // Chapter 7
        CH0701SavagelandRegion = 3816293063869929975,       // Dinosaur Jungle, S.H.I.E.L.D. Science Outpost, Mutate Marsh
        CH0702SauronCavesRegion = 5328248675005700748,
        CH0703BroodCavesRegion = 16291762617282992651,
        CH0705SabretoothShowdownRegion = 2422398536026824089,
        CH0706MutateCavesRegion = 11143195439539100296,     // Evolution Facility
        CH0707SinisterLabRegion = 15860259876442020487,     // Sinister Lab

        // Chapter 8
        CH0801AIMWeaponFacilityRegion = 7735172603194383419,// A.I.M. Weapon Facility
        CH0802HYDRAIslandRegion = 10124302533162047929,     // Hydra Island
        CH0803MandarinBossRegion = 2774018200640040,
        CH0804LatveriaPCZRegion = 5943200060062505421,      // Doomstadt Outer Village, Doomstadt Inner Village
        CH0805LatveriaPCZObjectiveARegion = 17551246229059673578,
        CH0806LatveriaPCZObjectiveBRegion = 17140936127691824620,
        CH0807LatveriaPCZObjectiveCRegion = 16703534592391519726,
        CH0808DoomCastleRegion = 11854040468777277783,      // Castle Doom
        CH0809DrDoomBossRegion = 136636993954912617,

        // Chapter 9
        CH0901NorwayPCZRegion = 5941813295004653614,        // Fjords of Norway, Ancient Ruin Site
        CH0902NorwayDarkForestRegion = 13806682145451612951,
        CH0903AsgardiaInstanceRegion = 749796778830142164,
        CH0904SiegePCZRegion = 9690692412445890462,         // Lower Asgard
        CH0905CanalRegion = 12361256372427953732,
        CH0906LokiBossRegion = 9322383157057886124,

        // Chapter 10
        MadripoorInvasionRegion = 3163018830455251180,      // Lowtown Warzone
        ATowerInvRegion = 3941087035061182612,              // Avengers Tower (Invaded)
        NYCRooftopInvRegion = 17036392409364308252,         // Hell's Kitchen Rooftops
        XMansionInvRegion = 10782376165327314768,           // Xavier's School (Invaded)
        UpperMadripoorRegionL60 = 9854118440624922758,      // S.W.O.R.D. Landing Zone, S.W.O.R.D. Command Post, Hightown Patrol
        HelicarrierInvasionRegion = 6825425747905684388,

        // Original Story
        HellsKitchen01Region = 16736492562991814709,
        NightclubRegion = 4122735557480815236,
        BrooklynRegion = 7168803565113578942,
        HellsKitchen02RedlightRegion = 4797431844393001110,
        UpperEastSideRegion = 7309701253780479014,
        FiskTowerRegion = 3083646536973818086,
        MrSinisterBaseRegion = 4018019170947768992,

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
        AxisRaidRegionGreen = 10186536050085467400,              // Axis Raid

        // Treasure Rooms
        TRGameCenterRegion = 16693804270797857925,              // Game Center

        // Special
        CosmicDoopSectorSpaceRegion = 8744981792306700722,      // Cosmic Doop Sector Space

        LimboRegionL60 = 4742357611317369027,
        AsgardCowLevelRegion = 17913362697985334451,
        ClassifiedBovineSectorRegion = 12735255224807267622,
    }

    public enum AreaPrototypeId : ulong
    {
        AvengersTowerHubArea = 11587794710165067927,
        NPEAvengersTowerHubArea = 11135337283876558073,
        TrainingRoomSHIELDArea = 5570532918023034911,
        XaviersMansionArea = 10707862600903825135,
        HelicarrierArea = 13861225053224309781,
        AsgardiaArea = 227371792232157880,
        GenoshaHUBArea = 1688916684898308937,
        GenoshaHUBEntryArea = 13803213742814074235,
        DangerRoomHubArea = 12475690031293798605,
        InvasionSafeAbodeArea1 = 14355026965969443149,
        InvasionSafeAbodeArea2 = 6833415259207637326,
        DailyGSubwayFactoryGen1Area = 9662974537569346456,
        XManhattanArea1 = 307100709842327667
    }
}
