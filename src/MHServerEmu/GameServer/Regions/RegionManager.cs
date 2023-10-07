using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.Entities;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;

namespace MHServerEmu.GameServer.Regions
{
    public partial class RegionManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly RegionPrototype[] AvailableRegions = new RegionPrototype[]
        {
            RegionPrototype.AvengersTowerHUBRegion,
            RegionPrototype.NPEAvengersTowerHUBRegion,
            RegionPrototype.TrainingRoomSHIELDRegion,
            RegionPrototype.HoloSimARegion1to60,
            RegionPrototype.XaviersMansionRegion,
            RegionPrototype.HelicarrierRegion,
            RegionPrototype.AsgardiaRegion,
            RegionPrototype.GenoshaHUBRegion,
            RegionPrototype.DangerRoomHubRegion,
            RegionPrototype.InvasionSafeAbodeRegion,
            RegionPrototype.NPERaftRegion,
            RegionPrototype.DailyGShockerSubwayRegionL60,
            RegionPrototype.DailyGSinisterLabRegionL60,
            RegionPrototype.BronxZooRegionL60,
            RegionPrototype.XManhattanRegion1to60,
            RegionPrototype.XManhattanRegion60Cosmic,
            RegionPrototype.BrooklynPatrolRegionL60,
            RegionPrototype.UpperMadripoorRegionL60,
            RegionPrototype.UpperMadripoorRegionL60Cosmic,
            RegionPrototype.UltronRaidRegionGreen,
            RegionPrototype.CH0101HellsKitchenRegion,
            RegionPrototype.CH0105NightclubRegion,
            RegionPrototype.CH0201ShippingYardRegion,
            RegionPrototype.CH0301MadripoorRegion,
            RegionPrototype.CH0701SavagelandRegion,
            RegionPrototype.CH0804LatveriaPCZRegion,
            RegionPrototype.CH0808DoomCastleRegion,
            RegionPrototype.CH0901NorwayPCZRegion,
            RegionPrototype.CH0904SiegePCZRegion,
            RegionPrototype.CosmicDoopSectorSpaceRegion,
            RegionPrototype.OpDailyBugleRegionL11To60
        };

        // TODO: Determine if a region is a hub from its prototype
        private static readonly RegionPrototype[] HubRegions = new RegionPrototype[]
        {
            RegionPrototype.AvengersTowerHUBRegion,
            RegionPrototype.NPEAvengersTowerHUBRegion,
            RegionPrototype.TrainingRoomSHIELDRegion,
            RegionPrototype.XaviersMansionRegion,
            RegionPrototype.HelicarrierRegion,
            RegionPrototype.AsgardiaRegion,
            RegionPrototype.GenoshaHUBRegion,
            RegionPrototype.DangerRoomHubRegion
        };

        private readonly EntityManager _entityManager;
        private readonly Dictionary<RegionPrototype, Region> _regionDict = new();

        public RegionManager(EntityManager entityManager)
        {
            _entityManager = entityManager;
        }

        public Region GetRegion(RegionPrototype prototype)
        {
            if (IsRegionAvailable(prototype))
            {
                if (_regionDict.TryGetValue(prototype, out Region region) == false)
                {
                    // Generate the region and create entities for it if needed
                    region = GenerateRegion(prototype);
                    CreateEntities(region);
                    _regionDict.Add(prototype, region);
                }

                return region;
            }
            else
            {
                Logger.Warn($"Region {prototype} is not available, falling back to NPEAvengersTowerHUBRegion");
                return GetRegion(RegionPrototype.NPEAvengersTowerHUBRegion);
            }
        }

        public static bool IsRegionAvailable(RegionPrototype prototype) => AvailableRegions.Contains(prototype);
        public static bool RegionIsHub(RegionPrototype prototype) => HubRegions.Contains(prototype);

        private static Region GenerateRegion(RegionPrototype prototype)
        {
            // TODO: loading data externally

            Region region = null;
            byte[] archiveData = Array.Empty<byte>();
            Area area;
            District district = null;

            switch (prototype)
            {
                case RegionPrototype.AvengersTowerHUBRegion:

                    archiveData = new byte[] {
                    };

                    region = new(RegionPrototype.AvengersTowerHUBRegion,
                        1488502313,
                        archiveData,
                        new(-5024f, -5024f, -2048f),
                        new(5024f, 5024f, 2048f),
                        new(10, DifficultyTier.Normal));

                    area = new(1, AreaPrototype.AvengersTowerHubArea, new(), true);
                    area.AddCell(new(1, GameDatabase.GetPrototypeId("Resource/Cells/DistrictCells/Avengers_Tower/AvengersTower_HUB.cell"), new()));

                    region.AddArea(area);

                    region.EntrancePosition = new(500f, 0f, 0f);
                    region.EntranceOrientation = new();
                    region.WaypointPosition = new(1575f, 0f, 0f);
                    region.WaypointOrientation = new();

                    break;

                case RegionPrototype.NPEAvengersTowerHUBRegion:

                    archiveData = new byte[] {
                        0xEF, 0x01, 0xE8, 0xC1, 0x02, 0x02, 0x00, 0x00, 0x00, 0x2C, 0xED, 0xC6,
                        0x05, 0x95, 0x80, 0x02, 0x0C, 0x00, 0x04, 0x9E, 0xCB, 0xD1, 0x93, 0xC7,
                        0xE8, 0xAF, 0xCC, 0xEE, 0x01, 0x06, 0x00, 0x8B, 0xE5, 0x02, 0x9E, 0xE6,
                        0x97, 0xCA, 0x0C, 0x01, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x04, 0x9B, 0xB2, 0x81, 0xF2, 0x83, 0xC6, 0xCD, 0x92, 0x10,
                        0x06, 0x00, 0xA2, 0xE0, 0x03, 0xBC, 0x88, 0xA0, 0x89, 0x0E, 0x01, 0x00,
                        0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xCC, 0xD7, 0xD1,
                        0xBE, 0xA9, 0xB0, 0xBB, 0xFE, 0x44, 0x06, 0x00, 0xCF, 0xF3, 0x04, 0xBC,
                        0xA4, 0xAD, 0xD3, 0x0A, 0x01, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0xC3, 0xBE, 0xB9, 0xC8, 0xD6, 0x8F, 0xAF, 0x8C, 0xE7,
                        0x01, 0x06, 0x00, 0xC7, 0x98, 0x05, 0xD6, 0x91, 0xB8, 0xA9, 0x0E, 0x01,
                        0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00
                    };

                    region = new(RegionPrototype.NPEAvengersTowerHUBRegion,
                        //1150669705055451881,
                        1488502313,
                        archiveData,
                        new(-5024f, -5024f, -2048f),
                        new(5024f, 5024f, 2048f),
                        new(10, DifficultyTier.Normal));

                    area = new(1, AreaPrototype.NPEAvengersTowerHubArea, new(), true);
                    area.AddCell(new(1, GameDatabase.GetPrototypeId("Resource/Cells/DistrictCells/Avengers_Tower/AvengersTowerNPE_HUB.cell"), new()));
                    area.CellList[0].AddEncounter(605211710028059265, 5, true);

                    region.AddArea(area);

                    region.EntrancePosition = new(1311f, 515.75f, 369f);
                    region.EntranceOrientation = new(-3.140625f, 0f, 0f);
                    region.WaypointPosition = new(536f, 862f, 341.5f);
                    region.WaypointOrientation = new(1.5625f, 0f, 0f);

                    break;

                case RegionPrototype.TrainingRoomSHIELDRegion:

                    archiveData = new byte[] {
                    };

                    region = new(RegionPrototype.TrainingRoomSHIELDRegion,
                        //1153032328761311238,
                        740100172,
                        archiveData,
                        new(-3250f, -3250f, -3250f),
                        new(3250f, 3250f, 3250f),
                        new(10, DifficultyTier.Normal));

                    area = new(1, AreaPrototype.TrainingRoomSHIELDArea, new(), true);
                    area.AddCell(new(1, GameDatabase.GetPrototypeId("Resource/Cells/DistrictCells/Training_Rooms/TrainingRoom_SHIELD_B.cell"), new()));

                    region.AddArea(area);

                    region.EntrancePosition = new(-2943.875f, 256f, 308f);
                    region.EntranceOrientation = new(-1.5625f, 0f, 0f);
                    region.WaypointPosition = new(-2943.875f, 256f, 308f);
                    region.WaypointOrientation = new();

                    break;

                case RegionPrototype.HoloSimARegion1to60:
                    archiveData = new byte[] {
                    };

                    region = new(RegionPrototype.HoloSimARegion1to60,
                        //1153032328761311241,
                        740100172,
                        archiveData,
                        new(-2432.0f, -2432.0f, -2432.0f),
                        new(2432.0f, 2432.0f, 2432.0f),
                        new(10, DifficultyTier.Normal));

                        area = new(1,(AreaPrototype)GameDatabase.GetPrototypeId("Regions/EndGame/TierX/HoloSim/HoloSimAArea.prototype"), new(), true);
                        area.AddCell(new(1, GameDatabase.GetPrototypeId("Resource/Cells/EndGame/DR_Survival_A.cell"), new()));

                        region.AddArea(area);

                    region.EntrancePosition = new(-2004.0f, -896.0f, 184.0f);
                    region.EntranceOrientation = new();
                    region.WaypointPosition = new(-2004.0f, -896.0f, 184.0f);
                    region.WaypointOrientation = new(1.5625f, 0f, 0f);

                    break;

                case RegionPrototype.XaviersMansionRegion:

                    archiveData = new byte[] {    
                    };

                    region = new(RegionPrototype.XaviersMansionRegion,
                        //1153032328761311239,
                        1640169729,
                        archiveData,
                        new(-6144f, -5120f, -1043f),
                        new(4096f, 9216f, 1024f),
                        new(28, DifficultyTier.Normal));

                    area = new(1, AreaPrototype.XaviersMansionArea, new(), true);

                    district = GameDatabase.Resource.DistrictDict["Resource/Districts/XaviersMansion.district"];
                    for (int i = 0; i < district.CellMarkerSet.Length; i++)
                        area.AddCell(new((uint)i + 1, GameDatabase.GetPrototypeId(district.CellMarkerSet[i].Resource), new()));

                    area.CellList[17].AddEncounter(15374827165380448803, 4, true);
                    area.CellList[15].AddEncounter(8642336607468261979, 7, true);
                    area.CellList[23].AddEncounter(4065272706848002543, 3, true);
                    area.CellList[10].AddEncounter(12198525011368022752, 1, true);

                    region.AddArea(area);

                    region.EntrancePosition = new(-2047f, 5136f, -75f);
                    region.EntranceOrientation = new();
                    region.WaypointPosition = new(-2047f, 5136f, -75f);
                    region.WaypointOrientation = new();

                    break;

                case RegionPrototype.HelicarrierRegion:

                    archiveData = new byte[] {
                    };

                    region = new(RegionPrototype.HelicarrierRegion,
                        //1153032354375335949,
                        1347063143,
                        archiveData,
                        new(-4352f, -4352f, -4352f),
                        new(4352f, 4352f, 4352f),
                        new(49, DifficultyTier.Normal));

                    area = new(1, AreaPrototype.HelicarrierArea, new(), true);
                    area.AddCell(new(1, GameDatabase.GetPrototypeId("Resource/Cells/DistrictCells/Helicarrier/Helicarrier_HUB.cell"), new()));

                    region.AddArea(area);

                    region.EntrancePosition = new(-405.75f, 1274.125f, 56f);
                    region.EntranceOrientation = new(0.78125f, 0f, 0f);
                    region.WaypointPosition = new(0.0f, 740.0f, 56f); // fixed
                    region.WaypointOrientation = new();

                    break;

                case RegionPrototype.AsgardiaRegion:

                    archiveData = new byte[] {
                    };

                    region = new(RegionPrototype.AsgardiaRegion,
                        //1153032354375335950,
                        2119981225,
                        archiveData,
                        new(-1152f, -5760f, -1152f),
                        new(5760f, 8064f, 1152f),
                        new(58, DifficultyTier.Normal));

                    area = new(1, AreaPrototype.AsgardiaArea, new(), true);

                    district = GameDatabase.Resource.DistrictDict["Resource/Districts/AsgardHubDistrict.district"];
                    for (int i = 0; i < district.CellMarkerSet.Length; i++)
                        area.AddCell(new((uint)i + 1, GameDatabase.GetPrototypeId(district.CellMarkerSet[i].Resource), new()));

                    region.AddArea(area);

                    region.EntrancePosition = new(1919.875f, 767.875f, 63f);
                    region.EntranceOrientation = new(3.140625f, 0f, 0f);
                    region.WaypointPosition = new(1919.875f, 767.875f, 63f);
                    region.WaypointOrientation = new();

                    break;

                case RegionPrototype.GenoshaHUBRegion:

                    archiveData = new byte[] {
                    };

                    region = new(RegionPrototype.GenoshaHUBRegion,
                        //1153032328761311240,
                        1922430980,
                        archiveData,
                        new(-11319f, -12336f, -2304f),
                        new(11319f, 12336f, 2304f),
                        new(60, DifficultyTier.Normal));

                    area = new(1, AreaPrototype.GenoshaHUBArea, new(), true);

                    district = GameDatabase.Resource.DistrictDict["Resource/Districts/GenoshaHUB.district"];
                    for (int i = 0; i < district.CellMarkerSet.Length; i++)
                        area.AddCell(new((uint)i + 1, GameDatabase.GetPrototypeId(district.CellMarkerSet[i].Resource), new()));

                    region.AddArea(area);

                    /*
                    Area entryArea = new(2, AreaPrototype.GenoshaHUBEntryArea, new(-11049f, -12336f, 0f), false);
                    entryArea.AddCell(new(18, GameDatabase.GetPrototypeId("Resource/Cells/DistrictCells/Genosha/GenoshaEntryArea/GenoshaEntry_X1Y1.cell"), new()));
                    region.AddArea(entryArea);*/
                    

                    region.EntrancePosition = new(3483.125f, 2724.875f, -1304f);
                    region.EntranceOrientation = new(2.046875f, 0.0f, 0.0f);
                    region.WaypointPosition = new(3483.125f, 2724.875f, -1304f);
                    region.WaypointOrientation = new();

                    break;

                case RegionPrototype.DangerRoomHubRegion:

                    archiveData = new byte[] {
                        0xEF, 0x01, 0xA8, 0x9B, 0x02, 0x07, 0x00, 0x00, 0x00, 0xB6, 0x80, 0x01,
                        0xE6, 0xCC, 0x99, 0xFB, 0x03, 0x2C, 0xFC, 0xA9, 0x02, 0xCA, 0x80, 0x03,
                        0xE6, 0xCC, 0x99, 0xFB, 0x03, 0x95, 0x80, 0x02, 0x12, 0xCA, 0x40, 0xE6,
                        0xCC, 0x99, 0xFB, 0x03, 0xA8, 0x80, 0x02, 0x80, 0x80, 0x80, 0x84, 0x04,
                        0xA8, 0xC0, 0x02, 0x9A, 0xB3, 0xE6, 0xF4, 0x03, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00
                    };

                    region = new(RegionPrototype.DangerRoomHubRegion,
                        //1154146333179728693,
                        1830444841,
                        archiveData,
                        new(-1664f, -1664f, -1664f),
                        new(1664f, 1664f, 1664f),
                        new(63, DifficultyTier.Heroic));

                    area = new(1, AreaPrototype.DangerRoomHubArea, new(), true);
                    area.AddCell(new(1, GameDatabase.GetPrototypeId("Resource/Cells/EndGame/EndlessDungeon/DangerRoom_LaunchTerminal.cell"), new()));

                    region.AddArea(area);

                    region.EntrancePosition = new(-384.125f, -301.375f, 308f);
                    region.EntranceOrientation = new();
                    region.WaypointPosition = new(-284f, -405f, 308f);
                    region.WaypointOrientation = new(2.640625f, 0f, 0f);

                    break;

                case RegionPrototype.InvasionSafeAbodeRegion:

                    archiveData = new byte[] {
                    };

                    region = new(RegionPrototype.InvasionSafeAbodeRegion,
                        //1153032354375335951,
                        1038711701,
                        archiveData,
                        new(-2304f, -1152f, -1152f),
                        new(2304f, 1152f, 1152f),
                        new(60, DifficultyTier.Normal));

                    area = new(2, AreaPrototype.InvasionSafeAbodeArea2, new(1152f, 0f, 0f), true);
                    area.AddCell(new(1, GameDatabase.GetPrototypeId("Resource/Cells/SecretInvasion/MadripoorInvasion/Invasion_SafehouseWithin.cell"), new()));
                    region.AddArea(area);

                    area = new(1, AreaPrototype.InvasionSafeAbodeArea1, new(-1152f, 0f, 0f), true);
                    area.AddCell(new(2, GameDatabase.GetPrototypeId("Resource/Cells/SecretInvasion/MadripoorInvasion/Invasion_Safehouse.cell"), new()));
                    region.AddArea(area);

                    region.EntrancePosition = new(893f, 0f, 60f);
                    region.EntranceOrientation = new(-0.78125f, 0f, 0f);
                    region.WaypointPosition = new(893f, 0f, 60f);
                    region.WaypointOrientation = new();

                    break;

                case RegionPrototype.OpDailyBugleRegionL11To60:
                    archiveData = new byte[] {
                    };

                    region = new(RegionPrototype.OpDailyBugleRegionL11To60,
                        1038711701,
                        archiveData,
                        new(-10240.0f,	-10240.0f,	-2048.0f),
                        new(10240.0f, 10240.0f, 2048.0f),
                        new(20, DifficultyTier.Normal));

                    string DailiBugleArea = "Regions/Operations/Events/DailyBugle/Areas/";
                    string DailiBugle = "Resource/Cells/EndGame/DangerDailies/DailyBugle/";

                    area = new(1, (AreaPrototype)GameDatabase.GetPrototypeId(DailiBugleArea + "DailyBugleLobbyEntryArea.prototype"), new(), true);
                    area.AddCell(new(1, GameDatabase.GetPrototypeId(DailiBugle + "DailyBugle_Trans/Daily_DailyBugle_Lobby_Entry_A.cell"), new(0.0f, 0.0f, 0.0f)));
                    region.AddArea(area);
                    area = new(2, (AreaPrototype)GameDatabase.GetPrototypeId(DailiBugleArea + "DailyBugleBasementArea.prototype"), new(), false);
                    area.AddCell(new(2, GameDatabase.GetPrototypeId(DailiBugle + "DailyBugle_A/Daily_DailyBugle_Basement_A.cell"), new(0.0f, 8192.0f, 0.0f)));
                    region.AddArea(area);
                    area = new(3, (AreaPrototype)GameDatabase.GetPrototypeId(DailiBugleArea + "DailyBugleArchivesArea.prototype"), new(), false);
                    area.AddCell(new(3, GameDatabase.GetPrototypeId(DailiBugle + "DailyBugle_A/Daily_DailyBugle_Archives_A.cell"), new(8192.0f, -2048.0f, 0.0f)));
                    region.AddArea(area);
                    area = new(4, (AreaPrototype)GameDatabase.GetPrototypeId(DailiBugleArea + "DailyBugleOfficeArea.prototype"), new(), false);
                    area.AddCell(new(4, GameDatabase.GetPrototypeId(DailiBugle + "DailyBugle_A/Daily_DailyBugle_Office_A.cell"), new(0.0f, -8192.0f, 0.0f)));
                    region.AddArea(area);
                    area = new(5, (AreaPrototype)GameDatabase.GetPrototypeId(DailiBugleArea + "DailyBugleRooftopBossArea.prototype"), new(), false);
                    area.AddCell(new(5, GameDatabase.GetPrototypeId(DailiBugle + "DailyBugle_Trans/Daily_DailyBugle_Roof_Boss_A.cell"), new(-8192.0f, 0.0f, 0.0f)));
                    region.AddArea(area);


                    region.EntrancePosition = new(1152.0f, -1296.0f, 48.0f);
                    region.EntranceOrientation = new(3.141641f, 0.0f, 0.0f);
                    region.WaypointPosition = new(1152.0f, -1296.0f, 48.0f);
                    region.WaypointOrientation = new();

                    break;

                case RegionPrototype.NPERaftRegion:
                    archiveData = new byte[] {
                    };
                    region = new(RegionPrototype.NPERaftRegion,
                        1038711701,
                        archiveData,
                        new(-1152.0f, 0.0f, -1152.0f),
                        new(8064.0f, 12672.0f, 1152.0f),
                        new(60, DifficultyTier.Normal));

                    AreaPrototype raftArea = (AreaPrototype)GameDatabase.GetPrototypeId("Regions/StoryRevamp/CH00Raft/TheRaftNPE/NPERaftArea.prototype");
                    area = new(1, raftArea, new(), true);

                    district = GameDatabase.Resource.DistrictDict["Resource/Districts/Raft_District.district"];

                    for (int i = 0; i < district.CellMarkerSet.Length; i++)
                        area.AddCell(new((uint)i + 1, GameDatabase.GetPrototypeId(district.CellMarkerSet[i].Resource), new()));

                    region.AddArea(area);

                    region.EntrancePosition = new(100.0f, 8700.0f, 0.0f);
                    region.EntranceOrientation = new(3.1415f, 0f, 0f); ;
                    region.WaypointPosition = new(0.0f, 2304.0f, 0.0f);
                    region.WaypointOrientation = new(0f, 0f, 0f);
                    break;

                case RegionPrototype.CH0101HellsKitchenRegion:
                    archiveData = new byte[] {
                    };
                    region = new(RegionPrototype.CH0101HellsKitchenRegion,
                        1883928786,
                        archiveData,
                        new(-1152.0f, -8064.0f, -1152.0f),
                        new(14976.0f, 14976.0f, 1152.0f),
                        new(60, DifficultyTier.Normal));

                    bool South = true;

                    if (South)
                    {

                        AreaPrototype CH0101HellsKitchenSouthArea = (AreaPrototype)GameDatabase.GetPrototypeId("Regions/StoryRevamp/CH01HellsKitchen/Brownstones/CH0101HellsKitchenSouthArea.prototype");
                        area = new(1, CH0101HellsKitchenSouthArea, new(), true);
                        district = GameDatabase.Resource.DistrictDict["Resource/Districts/Hells_Kitchen_Brownstones.district"];

                        for (int i = 0; i < district.CellMarkerSet.Length; i++)
                            area.AddCell(new((uint)i + 1, GameDatabase.GetPrototypeId(district.CellMarkerSet[i].Resource), new()));

                        region.AddArea(area);

                        region.EntrancePosition = new(5120.0f, 2176.0f, 635.0f);
                        region.EntranceOrientation = new(3.141592f, 0f, 0f);
                        region.WaypointPosition = new(5120.0f, 2176.0f, 635.0f);
                        region.WaypointOrientation = new(1.57082f, 0.0f, 0.0f);
                    }
                    // can be only one Area
                    else
                    {
                        
                        AreaPrototype CH0102HellsKitchenNorthArea = (AreaPrototype)GameDatabase.GetPrototypeId("Regions/StoryRevamp/CH01HellsKitchen/Brownstones/CH0102HellsKitchenNorthArea.prototype");
                        area = new(1, CH0102HellsKitchenNorthArea, new(), true);
                        district = GameDatabase.Resource.DistrictDict["Resource/Districts/Hells_Kitchen_Brownstones_B.district"];

                        for (int i = 0; i < district.CellMarkerSet.Length; i++)
                            area.AddCell(new((uint)(i + 1), GameDatabase.GetPrototypeId(district.CellMarkerSet[i].Resource), new()));

                        region.AddArea(area);

                        region.EntrancePosition = new(1236.0f, 950.0f, 0.0f);
                        region.EntranceOrientation = new(-1.57082f, 0f, 0f);
                        region.WaypointPosition = new(1236.0f, 950.0f, 0.0f);
                        region.WaypointOrientation = new(1.57082f, 0f, 0f);
                    }
                    break;

                case RegionPrototype.CH0105NightclubRegion:
                    
                    archiveData = new byte[] {
                    };
                    region = new(RegionPrototype.CH0105NightclubRegion,
                        1883928786,
                        archiveData,
                        new(-5760.0f, 0.0f, -1152.0f),
                        new(1152.0f, 10368.0f, 1152.0f),
                        new(60, DifficultyTier.Normal));

                    AreaPrototype Nightclub = (AreaPrototype)GameDatabase.GetPrototypeId("Regions/StoryRevamp/CH01HellsKitchen/Brownstones/Nightclub/CH01NightclubArea.prototype");
                    area = new(1, Nightclub, new(), true);

                    district = GameDatabase.Resource.DistrictDict["Resource/Districts/Hells_Kitchen_Nightclub.district"];

                    for (int i = 0; i < district.CellMarkerSet.Length; i++)
                        area.AddCell(new((uint)i + 1, GameDatabase.GetPrototypeId(district.CellMarkerSet[i].Resource), new()));

                    region.AddArea(area);

                    region.EntrancePosition = new(-4608.0f, 7100.0f, 0.0f);
                    region.EntranceOrientation = new(-1.5708f, 0f, 0f); ;
                    region.WaypointPosition = new(-880.0f, 6064.0f, 0.0f);
                    region.WaypointOrientation = new(2.35623f, 0.0f, 0.0f);

                    break;

                case RegionPrototype.CH0201ShippingYardRegion:
                    archiveData = new byte[] {
                    };
                    region = new(RegionPrototype.CH0201ShippingYardRegion,
                        1883928786,
                        archiveData, 
                        new(-1152.0f, -1152.0f, -1152.0f),
                        new(12672.0f, 14976.0f, 1152.0f),
                        new(60, DifficultyTier.Normal));

                    AreaPrototype ShippingYard = (AreaPrototype)GameDatabase.GetPrototypeId("Regions/StoryRevamp/CH02JerseyDocks/Areas/CH0201ShippingArea.prototype");
                    area = new(1, ShippingYard, new(), true);

                    district = GameDatabase.Resource.DistrictDict["Resource/Districts/Story/Ch02JerseyDocks/Ch02_JerseyDocks_Storage_Dist.district"];

                    for (int i = 0; i < district.CellMarkerSet.Length; i++)
                        area.AddCell(new((uint)i + 1, GameDatabase.GetPrototypeId(district.CellMarkerSet[i].Resource), new()));
                    region.AddArea(area);

                    region.EntrancePosition = new(48.0f, 4944.0f, 48.0f);
                    region.EntranceOrientation = new(-0.392705f, 0.0f, 0.0f); 
                    region.WaypointPosition = new(368.0f, 5040.0f, 0.0f);
                    region.WaypointOrientation = new(1.57082f, 0.0f, 0.0f);

                    break;

                case RegionPrototype.CH0301MadripoorRegion:
                    archiveData = new byte[] {
                    };
                    region = new(RegionPrototype.CH0301MadripoorRegion,
                        1883928786,
                        archiveData,
                        new(-10368.0f, -33408.0f, -1156.0f),
                        new(8064.0f, 31104.0f, 1156.0f),
                        new(60, DifficultyTier.Normal));

                    string Madripoor = "Resource/Cells/Madripoor/";
                    string MadripoorArea = "Regions/StoryRevamp/CH03Madripoor/";

                    area = new(1, (AreaPrototype) GameDatabase.GetPrototypeId(MadripoorArea + "Beach/BeachArea.prototype"), new(), false);
                    area.AddCell(new(93, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Cove_C/Madripoor_SuperPier_A_X1_Y0.cell"), new(0.0f, -20736.0f, -4.0f)));
                    area.AddCell(new(87, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Cove_C/Madripoor_Beach_C_NESW_A.cell"), new(0.0f, -25344.0f, -4.0f)));
                    area.AddCell(new(84, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Cove_C/Madripoor_Beach_C_NESW_B.cell"), new(0.0f, -27648.0f, -4.0f)));
                    area.AddCell(new(90, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Cove_C/Madripoor_Beach_C_NESW_C.cell"), new(0.0f, -23040.0f, -4.0f)));
                    area.AddCell(new(96, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Cove_C/Madripoor_SuperPier_A_X1_Y1.cell"), new(0.0f, -18432.0f, -4.0f)));
                    area.AddCell(new(99, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Cove_A/Madripoor_Beach_A_NESWcE_A.cell"), new(0.0f, -16128.0f, -4.0f)));
                    area.AddCell(new(83, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Trans/Beach_NEW_SCShipwreckEntry.cell"), new(-2304.0f, -27648.0f, -4.0f)));
                    area.AddCell(new(89, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Cove_B/Madripoor_Beach_B_NEW_A.cell"), new(-2304.0f, -23040.0f, -4.0f)));
                    area.AddCell(new(95, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Cove_C/Madripoor_SuperPier_A_X0_Y1.cell"), new(-2304.0f, -18432.0f, -4.0f)));
                    area.AddCell(new(92, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Cove_C/Madripoor_SuperPier_A_X0_Y0.cell"), new(-2304.0f, -20736.0f, -4.0f)));
                    area.AddCell(new(86, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Cove_A/Madripoor_Beach_A_NEW_A.cell"), new(-2304.0f, -25344.0f, -4.0f)));
                    area.AddCell(new(98, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Cove_B/Madripoor_Beach_B_NW_A.cell"), new(-2304.0f, -16128.0f, -4.0f)));
                    area.AddCell(new(97, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Cove_A/Madripoor_Beach_A_ESW_A.cell"), new(2304.0f, -18432.0f, -4.0f)));
                    area.AddCell(new(88, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Cove_A/Madripoor_Beach_A_ESW_B.cell"), new(2304.0f, -25344.0f, -4.0f)));
                    area.AddCell(new(85, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Cove_B/Madripoor_Beach_B_ESW_A.cell"), new(2304.0f, -27648.0f, -4.0f)));
                    area.AddCell(new(91, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Trans/Madripoor_Beach_ESW_CaveEntryA.cell"), new(2304.0f, -23040.0f, -4.0f)));
                    area.AddCell(new(94, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Trans/Madripoor_Beach_ESW_CaveEntryB.cell"), new(2304.0f, -20736.0f, -4.0f)));
                    area.AddCell(new(100, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Cove_B/Madripoor_Beach_B_SW_A.cell"), new(2304.0f, -16128.0f, -4.0f)));
                    area.AddCell(new(81, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Cove_C/Madripoor_Beach_C_Defense_A.cell"), new(0.0f, -29952.0f, -4.0f)));
                    area.AddCell(new(82, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Cove_A/Madripoor_Beach_A_ES_A.cell"), new(2304.0f, -29952.0f, -4.0f)));
                    area.AddCell(new(80, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Trans/Madripoor_Entry_B.cell"), new(-2304.0f, -29952.0f, -4.0f)));
                    region.AddArea(area);

                    area = new(2, (AreaPrototype)GameDatabase.GetPrototypeId(MadripoorArea + "Beach/BeachHydraOutpostArea.prototype"), new(), false);
                    area.AddCell(new(79, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Trans/Beach_BaseEntry_EW_A.cell"), new(3.0f, -13824.0f, -4.0f)));
                    region.AddArea(area);

                    area = new(3, (AreaPrototype)GameDatabase.GetPrototypeId(MadripoorArea + "BambooForest/HydraOutpostToForestArea.prototype"), new(), false);
                    area.AddCell(new(78, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Trans/Bamboo_Wide_BaseEntry_EW_A.cell"), new(0.0f, -11520.0f, -4.0f)));
                    region.AddArea(area);

                    area = new(4, (AreaPrototype) GameDatabase.GetPrototypeId(MadripoorArea + "BambooForest/BambooForestArea.prototype"), new(), false);
                    area.AddCell(new(70, GameDatabase.GetPrototypeId(Madripoor + "Bamboo_Forest_Wide_C/Bamboo_Village_X1_Y1_A.cell"), new(-2304.0f, -4608.0f, -4.0f)));
                    area.AddCell(new(71, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Trans/Bamboo_GroveEntry_NESW_A.cell"), new(0.0f, -4608.0f, -4.0f)));
                    area.AddCell(new(65, GameDatabase.GetPrototypeId(Madripoor + "Bamboo_Forest_Wide_C/Bamboo_Village_X1_Y0_A.cell"), new(-2304.0f, -6912.0f, -4.0f)));
                    area.AddCell(new(69, GameDatabase.GetPrototypeId(Madripoor + "Bamboo_Forest_Wide_C/Bamboo_Village_X0_Y1_A.cell"), new(-4608.0f, -4608.0f, -4.0f)));
                    area.AddCell(new(66, GameDatabase.GetPrototypeId(Madripoor + "Bamboo_Forest_C/Bamboo_Forest_POI_NESW_A.cell"), new(0.0f, -6912.0f, -4.0f)));
                    area.AddCell(new(64, GameDatabase.GetPrototypeId(Madripoor + "Bamboo_Forest_Wide_C/Bamboo_Village_X0_Y0_A.cell"), new(-4608.0f, -6912.0f, -4.0f)));
                    area.AddCell(new(76, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Trans/Bamboo_Forest_Wide_A_NESWcE_B.cell"), new(0.0f, -2304.0f, -4.0f)));
                    area.AddCell(new(75, GameDatabase.GetPrototypeId(Madripoor + "Bamboo_Forest_Wide/Bamboo_Forest_Wide_A_NSW_A.cell"), new(-2304.0f, -2304.0f, -4.0f)));
                    area.AddCell(new(74, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Trans/Bamboo_Wide_SCVillageEntry_A.cell"), new(-4608.0f, -2304.0f, -4.0f)));
                    area.AddCell(new(63, GameDatabase.GetPrototypeId(Madripoor + "Bamboo_Forest_Wide/Bamboo_Forest_Wide_A_NEW_A.cell"), new(-6912.0f, -6912.0f, -4.0f)));
                    area.AddCell(new(68, GameDatabase.GetPrototypeId(Madripoor + "Bamboo_Forest_Wide/Bamboo_Forest_Wide_A_NEW_A.cell"), new(-6912.0f, -4608.0f, -4.0f)));
                    area.AddCell(new(73, GameDatabase.GetPrototypeId(Madripoor + "Bamboo_Forest_Wide/Bamboo_Forest_Wide_A_NW_A.cell"), new(-6912.0f, -2304.0f, -4.0f)));
                    area.AddCell(new(67, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Trans/Bamboo_Wide_SCDojoEntry_ESW_A.cell"), new(2304.0f, -6912.0f, -4.0f)));
                    area.AddCell(new(72, GameDatabase.GetPrototypeId(Madripoor + "Bamboo_Forest_Wide/Bamboo_Forest_Wide_A_ESW_B.cell"), new(2304.0f, -4608.0f, -4.0f)));
                    area.AddCell(new(77, GameDatabase.GetPrototypeId(Madripoor + "Bamboo_Forest_Wide/Bamboo_Forest_Wide_A_SW_A.cell"), new(2304.0f, -2304.0f, -4.0f)));
                    area.AddCell(new(61, GameDatabase.GetPrototypeId(Madripoor + "Bamboo_Forest_Wide/Bamboo_Forest_Wide_A_NESWcW_A.cell"), new(0.0f, -9216.0f, -4.0f)));
                    area.AddCell(new(59, GameDatabase.GetPrototypeId(Madripoor + "Bamboo_Forest_Wide/Bamboo_Forest_Wide_A_NES_B.cell"), new(-4608.0f, -9216.0f, -4.0f)));
                    area.AddCell(new(60, GameDatabase.GetPrototypeId(Madripoor + "Bamboo_Forest_Wide/Bamboo_Forest_Wide_A_NES_A.cell"), new(-2304.0f, -9216.0f, -4.0f)));
                    area.AddCell(new(62, GameDatabase.GetPrototypeId(Madripoor + "Bamboo_Forest_Wide/Bamboo_Forest_Wide_A_ES_A.cell"), new(2304.0f, -9216.0f, -4.0f)));
                    area.AddCell(new(58, GameDatabase.GetPrototypeId(Madripoor + "Bamboo_Forest_Wide/Bamboo_Forest_Wide_A_NE_A.cell"), new(-6912.0f, -9216.0f, -4.0f)));
                    region.AddArea(area);

                    area = new(5, (AreaPrototype)GameDatabase.GetPrototypeId(MadripoorArea + "BambooForest/ForestToGladesArea.prototype"), new(), false);
                    area.AddCell(new(57, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Trans/Bamboo_Wide_GladesEntry_EW_A.cell"), new(0.0f, 0.0f, -4.0f)));
                    region.AddArea(area);

                    area = new(6, (AreaPrototype)GameDatabase.GetPrototypeId(MadripoorArea + "Cove/GladesToCoveArea.prototype"), new(), false);
                    area.AddCell(new(56, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Trans/Cove_GladesEntry_EW_A.cell"), new(0.0f, 2304.0f, -4.0f)));
                    region.AddArea(area);

                    area = new(7, (AreaPrototype)GameDatabase.GetPrototypeId(MadripoorArea + "Cove/CoveArea.prototype"), new(), false);
                    area.AddCell(new(51, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Shore_C/Madripoor_Shore_Grotto_Entry_A.cell"), new(0.0f, 11520.0f, -4.0f)));
                    area.AddCell(new(48, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Shore_C/Madripoor_SuperShore_A_X0_Y1.cell"), new(0.0f, 9216.0f, -4.0f)));
                    area.AddCell(new(45, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Shore_C/Madripoor_SuperShore_A_X0_Y0.cell"), new(0.0f, 6912.0f, -4.0f)));
                    area.AddCell(new(53, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Shore_A/Madripoor_Shore_A_NW_A.cell"), new(-2304.0f, 13824.0f, -4.0f)));
                    area.AddCell(new(54, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Shore_A/Madripoor_Shore_A_NESWcE_A.cell"), new(0.0f, 13824.0f, -4.0f)));
                    area.AddCell(new(44, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Shore_A/Madripoor_Shore_A_NEW_B.cell"), new(-2304.0f, 6912.0f, -4.0f)));
                    area.AddCell(new(47, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Shore_A/Madripoor_Shore_A_NEW_A.cell"), new(-2304.0f, 9216.0f, -4.0f)));
                    area.AddCell(new(50, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Shore_A/Madripoor_Shore_A_NEW_A.cell"), new(-2304.0f, 11520.0f, -4.0f)));
                    area.AddCell(new(46, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Shore_C/Madripoor_SuperShore_A_X1_Y0.cell"), new(2304.0f, 6912.0f, -4.0f)));
                    area.AddCell(new(49, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Shore_C/Madripoor_SuperShore_A_X1_Y1.cell"), new(2304.0f, 9216.0f, -4.0f)));
                    area.AddCell(new(52, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Trans/Shore_ESW_SCIslandEntry.cell"), new(2304.0f, 11520.0f, -4.0f)));
                    area.AddCell(new(55, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Shore_A/Madripoor_Shore_A_SW_A.cell"), new(2304.0f, 13824.0f, -4.0f)));
                    area.AddCell(new(42, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Shore_A/Madripoor_Shore_A_NESWcW_A.cell"), new(0.0f, 4608.0f, -4.0f)));
                    area.AddCell(new(43, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Shore_A/Madripoor_Shore_A_ES_A.cell"), new(2304.0f, 4608.0f, -4.0f)));
                    area.AddCell(new(41, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Shore_A/Madripoor_Shore_A_NE_A.cell"), new(-2304.0f, 4608.0f, -4.0f)));
                    region.AddArea(area);

                    area = new(8, (AreaPrototype)GameDatabase.GetPrototypeId(MadripoorArea + "LowTown/WasteTreatmentArea1.prototype"), new(), false);
                    area.AddCell(new(40, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Trans/Waste_Treatment_EW_C_Trans.cell"), new(0.0f, 16128.0f, -4.0f)));
                    region.AddArea(area);

                    area = new(9, (AreaPrototype)GameDatabase.GetPrototypeId(MadripoorArea + "LowTown/WasteTreatmentArea2.prototype"), new(), false);
                    area.AddCell(new(39, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Trans/Waste_Treatment_EW_A_Trans.cell"), new(0.0f, 18432.0f, 4.0f)));
                    region.AddArea(area);

                    area = new(10, (AreaPrototype)GameDatabase.GetPrototypeId(MadripoorArea + "LowTown/LowTownArea1.prototype"), new(), true);
                    area.AddCell(new(38, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Lower_C/Madripoor_Lower_C_SW_A.cell"), new(0.0f, 20736.0f, 4.0f)));
                    region.AddArea(area);
                    
                    area = new(11, (AreaPrototype)GameDatabase.GetPrototypeId(MadripoorArea + "LowTown/LowTownArea2.prototype"), new(), false);
                    area.AddCell(new(34, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Lower_A/Madripoor_Lower_A_NE_A.cell"), new(-4608.0f, 20736.0f, 4.0f)));
                    area.AddCell(new(37, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Trans/LowTown_SCAlleyEntry_N_A.cell"), new(-2304.0f, 23040.0f, 4.0f)));
                    area.AddCell(new(35, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Lower_B/Madripoor_Lower_B_NS_A.cell"), new(-2304.0f, 20736.0f, 4.0f)));
                    area.AddCell(new(36, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Lower_A/Madripoor_Lower_A_NEW_B.cell"), new(-4608.0f, 23040.0f, 4.0f)));
                    region.AddArea(area);

                    ulong fillerLower = GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Lower_A/Madripoor_Lower_A_FILLER_A.cell");

                    area = new(20, (AreaPrototype) GameDatabase.GetPrototypeId("DRAG/AreaGenerators/DynamicArea.prototype"), new(), false);
                    area.AddCell(new(101, fillerLower, new(-6912.0f, 23040.0f, 4.0f)));
                    area.AddCell(new(102, fillerLower, new(-4608.0f, 32256.0f, 4.0f)));
                    area.AddCell(new(103, fillerLower, new(-6912.0f, 32256.0f, 4.0f)));
                    area.AddCell(new(104, fillerLower, new(-9216.0f, 32256.0f, 4.0f)));
                    area.AddCell(new(105, fillerLower, new(-9216.0f, 29952.0f, 4.0f)));
                    area.AddCell(new(106, fillerLower, new(-9216.0f, 27648.0f, 4.0f)));
                    area.AddCell(new(107, fillerLower, new(-9216.0f, 25344.0f, 4.0f)));
                    area.AddCell(new(108, fillerLower, new(-2304.0f, 32256.0f, 4.0f)));
                    area.AddCell(new(109, fillerLower, new(-9216.0f, 23040.0f, 4.0f)));
                    area.AddCell(new(128, fillerLower, new(2304.0f, 32256.0f, 4.0f)));
                    area.AddCell(new(129, fillerLower, new(0.0f, 25344.0f, 4.0f)));
                    area.AddCell(new(130, fillerLower, new(0.0f, 32256.0f, 4.0f)));
                    area.AddCell(new(131, fillerLower, new(2304.0f, 25344.0f, 4.0f)));
                    area.AddCell(new(132, fillerLower, new(4608.0f, 25344.0f, 4.0f)));
                    area.AddCell(new(133, fillerLower, new(4608.0f, 27648.0f, 4.0f)));
                    area.AddCell(new(134, fillerLower, new(4608.0f, 32256.0f, 4.0f)));
                    area.AddCell(new(135, fillerLower, new(0.0f, 23040.0f, 4.0f)));
                    area.AddCell(new(136, fillerLower, new(2304.0f, 18432.0f, 4.0f)));
                    area.AddCell(new(137, fillerLower, new(2304.0f, 20736.0f, 4.0f)));
                    area.AddCell(new(138, fillerLower, new(2304.0f, 23040.0f, 4.0f)));
                    area.AddCell(new(139, fillerLower, new(-2304.0f, 18432.0f, 4.0f)));
                    area.AddCell(new(140, fillerLower, new(-4608.0f, 18432.0f, 4.0f)));
                    area.AddCell(new(141, fillerLower, new(-6912.0f, 20736.0f, 4.0f)));
                    area.AddCell(new(142, fillerLower, new(-6912.0f, 18432.0f, 4.0f)));
                    region.AddArea(area);

                    ulong fillerShore = GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Shore_A/Madripoor_Shore_A_FILLER_A.cell");

                    area = new(21, (AreaPrototype)GameDatabase.GetPrototypeId("DRAG/AreaGenerators/DynamicArea.prototype"), new(), false);
                    area.AddCell(new(110, fillerShore, new(-2304.0f, 2304.0f, -4.0f)));
                    area.AddCell(new(111, fillerShore, new(2304.0f, 16128.0f, -4.0f)));
                    area.AddCell(new(112, fillerShore, new(2304.0f, 2304.0f, -4.0f)));
                    area.AddCell(new(113, fillerShore, new(-2304.0f, 16128.0f, -4.0f)));
                    area.AddCell(new(114, fillerShore, new(4608.0f, 2304.0f, -4.0f)));
                    area.AddCell(new(115, fillerShore, new(-4608.0f, 16128.0f, -4.0f)));
                    area.AddCell(new(116, fillerShore, new(4608.0f, 4608.0f, -4.0f)));
                    area.AddCell(new(117, fillerShore, new(-4608.0f, 13824.0f, -4.0f)));
                    area.AddCell(new(118, fillerShore, new(4608.0f, 6912.0f, -4.0f)));
                    area.AddCell(new(119, fillerShore, new(-4608.0f, 11520.0f, -4.0f)));
                    area.AddCell(new(120, fillerShore, new(4608.0f, 9216.0f, -4.0f)));
                    area.AddCell(new(121, fillerShore, new(-4608.0f, 9216.0f, -4.0f)));
                    area.AddCell(new(122, fillerShore, new(4608.0f, 11520.0f, -4.0f)));
                    area.AddCell(new(123, fillerShore, new(-4608.0f, 6912.0f, -4.0f)));
                    area.AddCell(new(124, fillerShore, new(4608.0f, 13824.0f, -4.0f)));
                    area.AddCell(new(125, fillerShore, new(-4608.0f, 4608.0f, -4.0f)));
                    area.AddCell(new(126, fillerShore, new(4608.0f, 16128.0f, -4.0f)));
                    area.AddCell(new(127, fillerShore, new(-4608.0f, 2304.0f, -4.0f)));
                    region.AddArea(area);


                    area = new(22, (AreaPrototype)GameDatabase.GetPrototypeId(MadripoorArea + "LowTown/LowTownArea3.prototype"), new(), false);
                    area.AddCell(new(28, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Lower_A/Madripoor_Lower_A_NE_A.cell"), new(-6912.0f, 25344.0f, 4.0f)));
                    area.AddCell(new(32, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Lower_A/Madripoor_Lower_A_NW_A.cell"), new(-6912.0f, 29952.0f, 4.0f)));
                    area.AddCell(new(30, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Lower_C/Madripoor_Lower_BobaTeaPOI_A.cell"), new(-6912.0f, 27648.0f, 4.0f)));
                    area.AddCell(new(33, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Lower_A/Madripoor_Lower_A_SW_A.cell"), new(-4608.0f, 29952.0f, 4.0f)));
                    area.AddCell(new(31, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Trans/Madripoor_ESW_SCInstEntry.cell"), new(-4608.0f, 27648.0f, 4.0f)));
                    area.AddCell(new(29, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Lower_A/Madripoor_Lower_A_NESW_A.cell"), new(-4608.0f, 25344.0f, 4.0f)));
                    region.AddArea(area);

                    area = new(23, (AreaPrototype)GameDatabase.GetPrototypeId(MadripoorArea + "LowTown/LowTownPrincessBarArea.prototype"), new(), false);
                    area.AddCell(new(27, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Trans/Madripoor_Lower_PrincessBar.cell"), new(-2304.0f, 25344.0f, 4.0f))); 
                    region.AddArea(area);

                    area = new(24, (AreaPrototype)GameDatabase.GetPrototypeId(MadripoorArea + "LowTown/LowTownArea4.prototype"), new(), false);
                    area.AddCell(new(23, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Lower_A/Madripoor_Lower_A_ES_A.cell"), new(2304.0f, 27648.0f, 4.0f)));
                    area.AddCell(new(22, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Lower_A/Madripoor_Lower_A_NES_A.cell"), new(0.0f, 27648.0f, 4.0f)));
                    area.AddCell(new(24, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Trans/LowTown_SCForgottenEntry_A.cell"), new(-2304.0f, 29952.0f, 4.0f)));
                    area.AddCell(new(25, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Lower_A/Madripoor_Lower_A_NW_A.cell"), new(0.0f, 29952.0f, 4.0f)));
                    area.AddCell(new(21, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Lower_A/Madripoor_Lower_A_NEW_A.cell"), new(-2304.0f, 27648.0f, 4.0f)));
                    area.AddCell(new(26, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Lower_A/Madripoor_Lower_A_NSW_B.cell"), new(2304.0f, 29952.0f, 4.0f)));
                    region.AddArea(area);

                    area = new(25, (AreaPrototype)GameDatabase.GetPrototypeId(MadripoorArea + "LowTown/LowTownInterArea.prototype"), new(), false);
                    area.AddCell(new(20, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Trans/LowTown_To_High_NS_Exit_A.cell"), new(4608.0f, 29952.0f, 4.0f)));
                    region.AddArea(area);

                    area = new(26, (AreaPrototype)GameDatabase.GetPrototypeId(MadripoorArea + "LowTown/LowTownToTowerArea.prototype"), new(), false);
                    area.AddCell(new(19, GameDatabase.GetPrototypeId(Madripoor + "Madripoor_Trans/LowTown_To_High_S_Inter_A.cell"), new(6912.0f, 29952.0f, 4.0f)));
                    region.AddArea(area);


                    region.EntrancePosition = new(0.0f, 18432.0f, 4.0f);
                    region.EntranceOrientation = new(); ;
                    // Beach WaypointPosition -2050.0f, -29787.0f, -4.0f
                    // BambooForest WaypointPosition -240.0f, -7142.0f, -4.0f
                    // LowTown WaypointPosition 0.0f, 18432.0f, 4.0f
                    region.WaypointPosition = new(0.0f, 18432.0f, 4.0f); 
                    region.WaypointOrientation = new();
                    break;

                case RegionPrototype.CH0701SavagelandRegion:

                    archiveData = new byte[] {
                    };
                    region = new(RegionPrototype.CH0701SavagelandRegion,
                        1038711701,
                        archiveData,
                        new(-20736.0f, -18432.0f, -1152.0f),
                        new(16128.0f, 19584.0f, 1152.0f),
                        new(60, DifficultyTier.Normal));

                    string Savage = "Resource/Cells/Savagelands/";

                    area = new(1, (AreaPrototype)GameDatabase.GetPrototypeId("Regions/StoryRevamp/CH07SavageLand/Areas/DinoJungle/DinoJungleArea.prototype"), new(), true);

                    area.AddCell(new(85, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_Trans/Dino_Jungle_NESW_Entry_A.cell"), new(-5760.0f, 2304.0f, 0.0f)));
                    area.AddCell(new(103, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NESW_A.cell"), new(-5760.0f, 9216.0f, 0.0f)));
                    area.AddCell(new(82, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_B/Dino_Jungle_B_NESW_B.cell"), new(-12672.0f, 2304.0f, 0.0f)));
                    area.AddCell(new(114, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NSW_A.cell"), new(-8064.0f, 13824.0f, 0.0f)));
                    area.AddCell(new(90, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NESW_B.cell"), new(-8064.0f, 4608.0f, 0.0f)));
                    area.AddCell(new(106, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_B/Dino_Jungle_B_NESW_A.cell"), new(-12672.0f, 11520.0f, 0.0f)));
                    area.AddCell(new(88, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NESW_C.cell"), new(-12672.0f, 4608.0f, 0.0f)));
                    area.AddCell(new(83, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_Trans/Dino_Jungle_SacredValley_Entry.cell"), new(-10368.0f, 2304.0f, 0.0f)));
                    area.AddCell(new(89, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NESW_C.cell"), new(-10368.0f, 4608.0f, 0.0f)));
                    area.AddCell(new(116, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NESW_FILLER_A.cell"), new(-3456.0f, 13824.0f, 0.0f)));
                    area.AddCell(new(84, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NESW_C.cell"), new(-8064.0f, 2304.0f, 0.0f)));
                    area.AddCell(new(108, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_C/Dino_Jungle_Village_B.cell"), new(-8064.0f, 11520.0f, 0.0f)));
                    area.AddCell(new(102, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_C/Dino_Jungle_Village_A.cell"), new(-8064.0f, 9216.0f, 0.0f)));
                    area.AddCell(new(107, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_C/Dino_Jungle_Village_C.cell"), new(-10368.0f, 11520.0f, 0.0f)));
                    area.AddCell(new(97, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_C/Dino_Jungle_C_NESW_A.cell"), new(-5760.0f, 6912.0f, 0.0f)));
                    area.AddCell(new(95, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NESW_C.cell"), new(-10368.0f, 6912.0f, 0.0f)));
                    area.AddCell(new(96, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NESW_A.cell"), new(-8064.0f, 6912.0f, 0.0f)));
                    area.AddCell(new(94, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NESW_B.cell"), new(-12672.0f, 6912.0f, 0.0f)));
                    area.AddCell(new(101, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_C/Dino_Jungle_Village_D.cell"), new(-10368.0f, 9216.0f, 0.0f)));
                    area.AddCell(new(91, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NESW_A.cell"), new(-5760.0f, 4608.0f, 0.0f)));
                    area.AddCell(new(100, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NESW_A.cell"), new(-12672.0f, 9216.0f, 0.0f)));
                    area.AddCell(new(109, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NESWdNE_A.cell"), new(-5760.0f, 11520.0f, 0.0f)));
                    area.AddCell(new(113, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NSW_A.cell"), new(-10368.0f, 13824.0f, 0.0f)));
                    area.AddCell(new(81, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NEW_A.cell"), new(-14976.0f, 2304.0f, 0.0f)));
                    area.AddCell(new(112, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NSW_A.cell"), new(-12672.0f, 13824.0f, 0.0f)));
                    area.AddCell(new(93, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NEW_A.cell"), new(-14976.0f, 6912.0f, 0.0f)));
                    area.AddCell(new(87, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NEW_A.cell"), new(-14976.0f, 4608.0f, 0.0f)));
                    area.AddCell(new(105, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NEW_A.cell"), new(-14976.0f, 11520.0f, 0.0f)));
                    area.AddCell(new(99, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_C/Dino_Jungle_JeepWreck.cell"), new(-14976.0f, 9216.0f, 0.0f)));
                    area.AddCell(new(111, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NW_A.cell"), new(-14976.0f, 13824.0f, 0.0f)));
                    area.AddCell(new(86, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_ESW_A.cell"), new(-3456.0f, 2304.0f, 0.0f)));
                    area.AddCell(new(92, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_ESW_A.cell"), new(-3456.0f, 4608.0f, 0.0f)));
                    area.AddCell(new(98, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_ESW_A.cell"), new(-3456.0f, 6912.0f, 0.0f)));
                    area.AddCell(new(104, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_Trans/Dino_Jungle_DinoGrave_Entry.cell"), new(-3456.0f, 9216.0f, 0.0f)));
                    area.AddCell(new(115, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_SW_A.cell"), new(-5760.0f, 13824.0f, 0.0f)));
                    area.AddCell(new(110, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_SW_A.cell"), new(-3456.0f, 11520.0f, 0.0f)));
                    region.AddArea(area);

                    ulong fillerDino = GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NESW_FILLER_A.cell");
                    
                    area = new(24, (AreaPrototype)GameDatabase.GetPrototypeId("DRAG/AreaGenerators/DynamicArea.prototype"), new(), false);
                    // 117 - 208
                    uint areaid = 117;
                    float y;
                    for (uint i = 0; i < 17; i++)
                    {
                        y = 18432.0f - i * 2304.0f;
                        area.AddCell(new(areaid + i, fillerDino, new(-19584.0f, y, 0.0f)));
                        area.AddCell(new(areaid + 17 + i, fillerDino, new(-17280.0f, y, 0.0f)));
                        if (i == 13) continue;
                        area.AddCell(new(areaid + 17 * 2 + i, fillerDino, new(-1152.0f, y, 0.0f)));
                        area.AddCell(new(areaid + 17 * 3 + i, fillerDino, new(1152.0f, y, 0.0f)));
                    }

                    areaid = 117 + 17 * 4;
                    for (uint i = 0; i < 6; i++)
                    {
                        y = -14976.0f + i * 2304.0f;
                        area.AddCell(new(areaid + i, fillerDino, new(y, -18432.0f, 0.0f)));
                        area.AddCell(new(areaid + 6 + i, fillerDino, new(y, -16128.0f, 0.0f)));
                        area.AddCell(new(areaid + 6 * 2 + i, fillerDino, new(y, 16128.0f, 0.0f)));
                        area.AddCell(new(areaid + 6 * 3 + i, fillerDino, new(y, 18432.0f, 0.0f)));
                    }

                    region.AddArea(area);

                    area = new(2, (AreaPrototype)GameDatabase.GetPrototypeId("Regions/StoryRevamp/CH07SavageLand/Areas/TransitionAreas/RiverTransitionWestArea.prototype"), new(), false);
                    area.AddCell(new(75, GameDatabase.GetPrototypeId(Savage + "Savagelands_Trans/Dino_Jungle_SuperRiver_F.cell"), new(-14976.0f, 0.0f, 0.0f)));
                    area.AddCell(new(76, GameDatabase.GetPrototypeId(Savage + "Savagelands_Trans/Dino_Jungle_SuperRiver_E.cell"), new(-12672.0f, 0.0f, 0.0f)));
                    area.AddCell(new(77, GameDatabase.GetPrototypeId(Savage + "Savagelands_Trans/Dino_Jungle_SuperRiver_B.cell"), new(-10368.0f, 0.0f, 0.0f)));
                    area.AddCell(new(78, GameDatabase.GetPrototypeId(Savage + "Savagelands_Trans/Dino_Jungle_SuperRiver_E.cell"), new(-8064.0f, 0.0f, 0.0f)));
                    area.AddCell(new(79, GameDatabase.GetPrototypeId(Savage + "Savagelands_Trans/Dino_Jungle_SuperRiver_C.cell"), new(-5760.0f, 0.0f, 0.0f)));
                    area.AddCell(new(80, GameDatabase.GetPrototypeId(Savage + "Savagelands_Trans/Dino_Jungle_SuperRiver_A.cell"), new(-3456.0f, 0.0f, 0.0f)));
                    region.AddArea(area);

                    area = new(18, (AreaPrototype)GameDatabase.GetPrototypeId("Regions/StoryRevamp/CH07SavageLand/Areas/BroodJungle/BroodJungleArea.prototype"), new(), false);
                    area.AddCell(new(90, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NESW_B.cell"), new(-8064.0f, 4608.0f, 0.0f)));
                    area.AddCell(new(65, GameDatabase.GetPrototypeId(Savage + "Brood_Jungle_C/Brood_Jungle_HeavyJungle_3L3_B.cell"), new(-10368.0f, -4608.0f, 0.0f)));
                    area.AddCell(new(49, GameDatabase.GetPrototypeId(Savage + "Brood_Jungle_Trans/Brood_Jungle_SCInstEntry.cell"), new(-5760.0f, -11520.0f, 0.0f)));
                    area.AddCell(new(73, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_C/Dino_Jungle_MiniVillage.cell"), new(-5760.0f, -2304.0f, 0.0f)));
                    area.AddCell(new(55, GameDatabase.GetPrototypeId(Savage + "Brood_Jungle_Trans/Brood_Jungle_SCStationEntry_A.cell"), new(-5760.0f, -9216.0f, 0.0f)));
                    area.AddCell(new(71, GameDatabase.GetPrototypeId(Savage + "Brood_Jungle_C/Brood_Jungle_HeavyJungle_1x2_B.cell"), new(-10368.0f, -2304.0f, 0.0f)));
                    area.AddCell(new(70, GameDatabase.GetPrototypeId(Savage + "Brood_Jungle_C/Brood_Jungle_ScienceSpire.cell"), new(-12672.0f, -2304.0f, 0.0f)));
                    area.AddCell(new(60, GameDatabase.GetPrototypeId(Savage + "Brood_Jungle_Trans/Jungle_Brood_Caves_Entry_A.cell"), new(-8064.0f, -6912.0f, 0.0f)));
                    area.AddCell(new(58, GameDatabase.GetPrototypeId(Savage + "Brood_Jungle_Trans/Brood_Jungle_SHIELD_A.cell"), new(-12672.0f, -6912.0f, 0.0f)));
                    area.AddCell(new(66, GameDatabase.GetPrototypeId(Savage + "Brood_Jungle_C/Brood_Jungle_C_NESW_A.cell"), new(-8064.0f, -4608.0f, 0.0f)));
                    area.AddCell(new(64, GameDatabase.GetPrototypeId(Savage + "Brood_Jungle_C/Brood_Jungle_HeavyJungle_3L3_C.cell"), new(-12672.0f, -4608.0f, 0.0f)));
                    area.AddCell(new(48, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NESWdSE_A.cell"), new(-8064.0f, -11520.0f, 0.0f)));
                    area.AddCell(new(72, GameDatabase.GetPrototypeId(Savage + "Brood_Jungle_C/Brood_Jungle_HeavyJungle_1x2_A.cell"), new(-8064.0f, -2304.0f, 0.0f)));
                    area.AddCell(new(40, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NES_A.cell"), new(-12672.0f, -13824.0f, 0.0f)));
                    area.AddCell(new(61, GameDatabase.GetPrototypeId(Savage + "Brood_Jungle_C/Brood_Jungle_HeavyJungle_2x1_A.cell"), new(-5760.0f, -6912.0f, 0.0f)));
                    area.AddCell(new(59, GameDatabase.GetPrototypeId(Savage + "Brood_Jungle_C/Brood_Jungle_HeavyJungle_3L3_A.cell"), new(-10368.0f, -6912.0f, 0.0f)));
                    area.AddCell(new(67, GameDatabase.GetPrototypeId(Savage + "Brood_Jungle_C/Brood_Jungle_HeavyJungle_2x1_B.cell"), new(-5760.0f, -4608.0f, 0.0f)));
                    area.AddCell(new(46, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NESWdNE_A.cell"), new(-12672.0f, -11520.0f, 0.0f)));
                    area.AddCell(new(47, GameDatabase.GetPrototypeId(Savage + "Brood_Jungle_Trans/Brood_Jungle_SCTowerEntry_A.cell"), new(-10368.0f, -11520.0f, 0.0f)));
                    area.AddCell(new(54, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NESWdSW_A.cell"), new(-8064.0f, -9216.0f, 0.0f)));
                    area.AddCell(new(69, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NEW_A.cell"), new(-14976.0f, -2304.0f, 0.0f)));
                    area.AddCell(new(45, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NEW_A.cell"), new(-14976.0f, -11520.0f, 0.0f)));
                    area.AddCell(new(63, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NEW_A.cell"), new(-14976.0f, -4608.0f, 0.0f)));
                    area.AddCell(new(51, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NEW_A.cell"), new(-14976.0f, -9216.0f, 0.0f)));
                    area.AddCell(new(57, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NEW_A.cell"), new(-14976.0f, -6912.0f, 0.0f)));
                    area.AddCell(new(52, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NESWdNW_A.cell"), new(-12672.0f, -9216.0f, 0.0f)));
                    area.AddCell(new(50, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NESWcN_A.cell"), new(-3456.0f, -11520.0f, 0.0f)));
                    area.AddCell(new(74, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_ESW_A.cell"), new(-3456.0f, -2304.0f, 0.0f)));
                    area.AddCell(new(42, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NES_A.cell"), new(-8064.0f, -13824.0f, 0.0f)));
                    area.AddCell(new(56, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_ESW_A.cell"), new(-3456.0f, -9216.0f, 0.0f)));
                    area.AddCell(new(68, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_ESW_A.cell"), new(-3456.0f, -4608.0f, 0.0f)));
                    area.AddCell(new(62, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_ESW_A.cell"), new(-3456.0f, -6912.0f, 0.0f)));
                    area.AddCell(new(43, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NES_A.cell"), new(-5760.0f, -13824.0f, 0.0f)));
                    area.AddCell(new(53, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NES_A.cell"), new(-10368.0f, -9216.0f, 0.0f)));
                    area.AddCell(new(41, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NES_A.cell"), new(-10368.0f, -13824.0f, 0.0f)));
                    area.AddCell(new(44, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_ES_A.cell"), new(-3456.0f, -13824.0f, 0.0f)));
                    area.AddCell(new(39, GameDatabase.GetPrototypeId(Savage + "Dino_Jungle_A/Dino_Jungle_A_NE_A.cell"), new(-14976.0f, -13824.0f, 0.0f)));
                    region.AddArea(area);

                    area = new(21, (AreaPrototype)GameDatabase.GetPrototypeId("Regions/StoryRevamp/CH07SavageLand/Areas/TransitionAreas/RopeBridgeNorthArea.prototype"), new(), false);
                    area.AddCell(new(38, GameDatabase.GetPrototypeId(Savage + "RopeBridge_Trans/RopeBridge_NS_A.cell"), new(-1152.0f, -11520.0f, 0.0f)));
                    region.AddArea(area);

                    area = new(22, (AreaPrototype)GameDatabase.GetPrototypeId("Regions/StoryRevamp/CH07SavageLand/Areas/TransitionAreas/JungleToMarshSN.prototype"), new(), false);
                    area.AddCell(new(37, GameDatabase.GetPrototypeId(Savage + "Savagelands_Trans/JungleToMarsh_Trans_SN_A.cell"), new(1152.0f, -11520.0f, 0.0f)));
                    region.AddArea(area);

                    area = new(23, (AreaPrototype)GameDatabase.GetPrototypeId("Regions/StoryRevamp/CH07SavageLand/Areas/MutateMarsh/MutateMarshArea.prototype"), new(), false);
                    area.AddCell(new(20, GameDatabase.GetPrototypeId(Savage + "Marsh_C/Marsh_C_NESW_Island_1x1.cell"), new(5760.0f, -6912.0f, 0.0f)));
                    area.AddCell(new(11, GameDatabase.GetPrototypeId(Savage + "Marsh_B/Marsh_B_NESW_A.cell"), new(12672.0f, -11520.0f, 0.0f)));
                    area.AddCell(new(27, GameDatabase.GetPrototypeId(Savage + "Marsh_C/Marsh_C_NESW_B.cell"), new(8064.0f, -4608.0f, 0.0f)));
                    area.AddCell(new(15, GameDatabase.GetPrototypeId(Savage + "Marsh_Trans/Marsh_NESW_SabretoothEntry_A.cell"), new(8064.0f, -9216.0f, 0.0f)));
                    area.AddCell(new(16, GameDatabase.GetPrototypeId(Savage + "Marsh_A/Marsh_A_NESW_B.cell"), new(10368.0f, -9216.0f, 0.0f)));
                    area.AddCell(new(8, GameDatabase.GetPrototypeId(Savage + "Marsh_B/Marsh_B_NESW_B.cell"), new(5760.0f, -11520.0f, 0.0f)));
                    area.AddCell(new(10, GameDatabase.GetPrototypeId(Savage + "Marsh_Trans/Marsh_Mutate_Caves_Entry_A.cell"), new(10368.0f, -11520.0f, 0.0f)));
                    area.AddCell(new(26, GameDatabase.GetPrototypeId(Savage + "Marsh_A/Marsh_A_NESW_A.cell"), new(5760.0f, -4608.0f, 0.0f)));
                    area.AddCell(new(21, GameDatabase.GetPrototypeId(Savage + "Marsh_C/Marsh_C_NESW_Island_1x2_B.cell"), new(8064.0f, -6912.0f, 0.0f)));
                    area.AddCell(new(22, GameDatabase.GetPrototypeId(Savage + "Marsh_C/Marsh_C_NESW_Island_1x2_A.cell"), new(10368.0f, -6912.0f, 0.0f)));
                    area.AddCell(new(14, GameDatabase.GetPrototypeId(Savage + "Marsh_A/Marsh_A_NESW_C.cell"), new(5760.0f, -9216.0f, 0.0f)));
                    area.AddCell(new(17, GameDatabase.GetPrototypeId(Savage + "Marsh_C/Marsh_C_NESW_Island_A.cell"), new(12672.0f, -9216.0f, 0.0f)));
                    area.AddCell(new(23, GameDatabase.GetPrototypeId(Savage + "Marsh_C/Marsh_C_NESW_Island_B.cell"), new(12672.0f, -6912.0f, 0.0f)));
                    area.AddCell(new(9, GameDatabase.GetPrototypeId(Savage + "Marsh_B/Marsh_B_NESW_C.cell"), new(8064.0f, -11520.0f, 0.0f)));
                    area.AddCell(new(29, GameDatabase.GetPrototypeId(Savage + "Marsh_A/Marsh_A_NESW_C.cell"), new(12672.0f, -4608.0f, 0.0f)));
                    area.AddCell(new(28, GameDatabase.GetPrototypeId(Savage + "Marsh_C/Marsh_C_NESW_A.cell"), new(10368.0f, -4608.0f, 0.0f)));
                    area.AddCell(new(33, GameDatabase.GetPrototypeId(Savage + "Marsh_A/Marsh_A_NSW_A.cell"), new(8064.0f, -2304.0f, 0.0f)));
                    area.AddCell(new(35, GameDatabase.GetPrototypeId(Savage + "Marsh_Trans/Marsh_NSW_SCInstEntry.cell"), new(12672.0f, -2304.0f, 0.0f)));
                    area.AddCell(new(34, GameDatabase.GetPrototypeId(Savage + "Marsh_A/Marsh_A_NSW_A.cell"), new(10368.0f, -2304.0f, 0.0f)));
                    area.AddCell(new(2, GameDatabase.GetPrototypeId(Savage + "Marsh_A/Marsh_A_NES_A.cell"), new(5760.0f, -13824.0f, 0.0f)));
                    area.AddCell(new(32, GameDatabase.GetPrototypeId(Savage + "Marsh_Trans/Marsh_NSW_SCInstEntry_B.cell"), new(5760.0f, -2304.0f, 0.0f)));
                    area.AddCell(new(7, GameDatabase.GetPrototypeId(Savage + "Marsh_A/Marsh_A_NESWcS_A.cell"), new(3456.0f, -11520.0f, 0.0f)));
                    area.AddCell(new(19, GameDatabase.GetPrototypeId(Savage + "Marsh_A/Marsh_A_NEW_A.cell"), new(3456.0f, -6912.0f, 0.0f)));
                    area.AddCell(new(25, GameDatabase.GetPrototypeId(Savage + "Marsh_A/Marsh_A_NEW_A.cell"), new(3456.0f, -4608.0f, 0.0f)));
                    area.AddCell(new(13, GameDatabase.GetPrototypeId(Savage + "Marsh_A/Marsh_A_NEW_A.cell"), new(3456.0f, -9216.0f, 0.0f)));
                    area.AddCell(new(31, GameDatabase.GetPrototypeId(Savage + "Marsh_A/Marsh_A_NW_A.cell"), new(3456.0f, -2304.0f, 0.0f)));
                    area.AddCell(new(18, GameDatabase.GetPrototypeId(Savage + "Marsh_A/Marsh_A_ESW_A.cell"), new(14976.0f, -9216.0f, 0.0f)));
                    area.AddCell(new(30, GameDatabase.GetPrototypeId(Savage + "Marsh_A/Marsh_A_ESW_A.cell"), new(14976.0f, -4608.0f, 0.0f)));
                    area.AddCell(new(24, GameDatabase.GetPrototypeId(Savage + "Marsh_A/Marsh_A_ESW_A.cell"), new(14976.0f, -6912.0f, 0.0f)));
                    area.AddCell(new(12, GameDatabase.GetPrototypeId(Savage + "Marsh_Trans/Marsh_ESW_SCInstEntry.cell"), new(14976.0f, -11520.0f, 0.0f)));
                    area.AddCell(new(36, GameDatabase.GetPrototypeId(Savage + "Marsh_A/Marsh_A_SW_A.cell"), new(14976.0f, -2304.0f, 0.0f)));
                    area.AddCell(new(5, GameDatabase.GetPrototypeId(Savage + "Marsh_A/Marsh_A_NES_A.cell"), new(12672.0f, -13824.0f, 0.0f)));
                    area.AddCell(new(4, GameDatabase.GetPrototypeId(Savage + "Marsh_A/Marsh_A_NES_A.cell"), new(10368.0f, -13824.0f, 0.0f)));
                    area.AddCell(new(3, GameDatabase.GetPrototypeId(Savage + "Marsh_A/Marsh_A_NES_A.cell"), new(8064.0f, -13824.0f, 0.0f)));
                    area.AddCell(new(6, GameDatabase.GetPrototypeId(Savage + "Marsh_A/Marsh_A_ES_A.cell"), new(14976.0f, -13824.0f, 0.0f)));
                    area.AddCell(new(1, GameDatabase.GetPrototypeId(Savage + "Marsh_A/Marsh_A_NE_A.cell"), new(3456.0f, -13824.0f, 0.0f)));
                    region.AddArea(area);

                    ulong fillerMarch = GameDatabase.GetPrototypeId(Savage + "Marsh_A/Marsh_A_FILLER_A.cell");
                    area = new(25, (AreaPrototype)GameDatabase.GetPrototypeId("DRAG/AreaGenerators/DynamicArea.prototype"), new(), false);
                   
                    areaid = 209;
                    for (uint i = 0; i < 6; i++)
                    {
                        y = 14976.0f - i * 2304.0f;
                        area.AddCell(new(areaid + i, fillerMarch, new(y, -16128.0f, 0.0f)));
                        area.AddCell(new(areaid + 6 + i, fillerMarch, new(y, 0.0f, 0.0f)));
                    }

                    areaid = 209 + 6 * 2;
                    for (uint i = 0; i < 8; i++)
                    {
                        y = -16128.0f + i * 2304.0f;
                        area.AddCell(new(areaid + i, fillerMarch, new(17280.0f, y, 0.0f)));
                    }

                    region.AddArea(area);

                    region.EntrancePosition = new(-6100.0f, 2210.0f, 0.0f);
                    region.EntranceOrientation = new(); 
                    region.WaypointPosition = new(-6100.0f, 2210.0f, -10.0f);
                    region.WaypointOrientation = new();

                    break;

                case RegionPrototype.CH0804LatveriaPCZRegion:
                    archiveData = new byte[] {
                    };

                    region = new(RegionPrototype.CH0804LatveriaPCZRegion,
                        1901487720,
                        archiveData,
                        new(-9216.0f, -13824.0f, -1152.0f),
                        new(9216.0f, 13824.0f, 1152.0f),
                        new(60, DifficultyTier.Normal));

                    string Latveria = "Resource/Cells/Latveria/";
                    string LatveriaArea = "Regions/StoryRevamp/CH08Latveria/Areas/Latveria/Doomstadt/";

                    area = new(1, (AreaPrototype)GameDatabase.GetPrototypeId(LatveriaArea + "LatveriaPCZArea1.prototype"), new(), true);
                    area.AddCell(new(52, GameDatabase.GetPrototypeId(Latveria + "Courtyard_C/Courtyard_SuperBridgeA_X0_Y1.cell"), new(-3456.0f, -10368.0f, 0.0f)));
                    area.AddCell(new(51, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NESW_B.cell"), new(-5760.0f, -10368.0f, 0.0f)));
                    area.AddCell(new(56, GameDatabase.GetPrototypeId(Latveria + "Courtyard_C/Courtyard_SuperBridgeA_X0_Y2.cell"), new(-3456.0f, -8064.0f, 0.0f)));
                    area.AddCell(new(55, GameDatabase.GetPrototypeId(Latveria + "Courtyard_Trans/Courtyard_Objective_NSW_A.cell"), new(-5760.0f, -8064.0f, 0.0f)));
                    area.AddCell(new(50, GameDatabase.GetPrototypeId(Latveria + "Courtyard_Trans/Courtyard_Entry_A.cell"), new(-8064.0f, -10368.0f, 0.0f)));
                    area.AddCell(new(54, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NW_A.cell"), new(-8064.0f, -8064.0f, 0.0f)));
                    area.AddCell(new(53, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NESWcN_A.cell"), new(-1152.0f, -10368.0f, 0.0f)));
                    area.AddCell(new(57, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_SW_A.cell"), new(-1152.0f, -8064.0f, 0.0f)));
                    area.AddCell(new(47, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NES_B.cell"), new(-5760.0f, -12672.0f, 0.0f)));
                    area.AddCell(new(48, GameDatabase.GetPrototypeId(Latveria + "Courtyard_C/Courtyard_SuperBridgeA_X0_Y0.cell"), new(-3456.0f, -12672.0f, 0.0f)));
                    area.AddCell(new(49, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_ES_A.cell"), new(-1152.0f, -12672.0f, 0.0f)));
                    area.AddCell(new(46, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NE_A.cell"), new(-8064.0f, -12672.0f, 0.0f)));
                    region.AddArea(area);

                    area = new(2, (AreaPrototype)GameDatabase.GetPrototypeId(LatveriaArea + "LatveriaPCZArea2.prototype"), new(), false);
                    area.AddCell(new(36, GameDatabase.GetPrototypeId(Latveria + "Courtyard_B/Courtyard_B_NESW_C.cell"), new(5760.0f, -10368.0f, 0.0f)));
                    area.AddCell(new(44, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NSW_B.cell"), new(5760.0f, -5760.0f, 0.0f)));
                    area.AddCell(new(40, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NESW_A.cell"), new(5760.0f, -8064.0f, 0.0f)));
                    area.AddCell(new(39, GameDatabase.GetPrototypeId(Latveria + "Courtyard_Trans/Courtyard_TavernEntry_NESW_A.cell"), new(3456.0f, -8064.0f, 0.0f)));
                    area.AddCell(new(35, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NESW_D.cell"), new(3456.0f, -10368.0f, 0.0f)));
                    area.AddCell(new(43, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NESWcE_A.cell"), new(3456.0f, -5760.0f, 0.0f)));
                    area.AddCell(new(34, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NESWcS_A.cell"), new(1152.0f, -10368.0f, 0.0f)));
                    area.AddCell(new(38, GameDatabase.GetPrototypeId(Latveria + "Courtyard_Trans/Courtyard_Objective_NEW_A.cell"), new(1152.0f, -8064.0f, 0.0f)));
                    area.AddCell(new(42, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NW_A.cell"), new(1152.0f, -5760.0f, 0.0f)));
                    area.AddCell(new(41, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_ESW_A.cell"), new(8064.0f, -8064.0f, 0.0f)));
                    area.AddCell(new(37, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_ESW_B.cell"), new(8064.0f, -10368.0f, 0.0f)));
                    area.AddCell(new(45, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_SW_A.cell"), new(8064.0f, -5760.0f, 0.0f)));
                    area.AddCell(new(31, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NES_A.cell"), new(3456.0f, -12672.0f, 0.0f)));
                    area.AddCell(new(32, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NES_B.cell"), new(5760.0f, -12672.0f, 0.0f)));
                    area.AddCell(new(33, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_ES_A.cell"), new(8064.0f, -12672.0f, 0.0f)));
                    area.AddCell(new(30, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NE_A.cell"), new(1152.0f, -12672.0f, 0.0f)));
                    region.AddArea(area);

                    area = new(3, (AreaPrototype)GameDatabase.GetPrototypeId(LatveriaArea + "LatveriaPCZArea3.prototype"), new(), false);
                    area.AddCell(new(23, GameDatabase.GetPrototypeId(Latveria + "Courtyard_C/Courtyard_SuperWallA_X0_Y1.cell"), new(3456.0f, 3456.0f, 0.0f)));
                    area.AddCell(new(15, GameDatabase.GetPrototypeId(Latveria + "Courtyard_B/Courtyard_B_NESW_B.cell"), new(3456.0f, -1152.0f, 0.0f)));
                    area.AddCell(new(24, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NESW_E.cell"), new(5760.0f, 3456.0f, 0.0f)));
                    area.AddCell(new(16, GameDatabase.GetPrototypeId(Latveria + "Courtyard_Trans/Courtyard_Objective_NESW_A.cell"), new(5760.0f, -1152.0f, 0.0f)));
                    area.AddCell(new(20, GameDatabase.GetPrototypeId(Latveria + "Courtyard_B/Courtyard_B_NESW_C.cell"), new(5760.0f, 1152.0f, 0.0f)));
                    area.AddCell(new(28, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NSW_B.cell"), new(5760.0f, 5760.0f, 0.0f)));
                    area.AddCell(new(19, GameDatabase.GetPrototypeId(Latveria + "Courtyard_C/Courtyard_SuperWallA_X0_Y0.cell"), new(3456.0f, 1152.0f, 0.0f)));
                    area.AddCell(new(27, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NESWcE_A.cell"), new(3456.0f, 5760.0f, 0.0f)));
                    area.AddCell(new(14, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NEW_A.cell"), new(1152.0f, -1152.0f, 0.0f)));
                    area.AddCell(new(18, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NEW_B.cell"), new(1152.0f, 1152.0f, 0.0f)));
                    area.AddCell(new(22, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NEW_A.cell"), new(1152.0f, 3456.0f, 0.0f)));
                    area.AddCell(new(26, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NW_A.cell"), new(1152.0f, 5760.0f, 0.0f)));
                    area.AddCell(new(25, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_ESW_B.cell"), new(8064.0f, 3456.0f, 0.0f)));
                    area.AddCell(new(21, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_ESW_A.cell"), new(8064.0f, 1152.0f, 0.0f)));
                    area.AddCell(new(17, GameDatabase.GetPrototypeId(Latveria + "Courtyard_Trans/Courtyard_TRCryptEntry_ESW_A.cell"), new(8064.0f, -1152.0f, 0.0f)));
                    area.AddCell(new(29, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_SW_A.cell"), new(8064.0f, 5760.0f, 0.0f)));
                    area.AddCell(new(11, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NESWcW_A.cell"), new(3456.0f, -3456.0f, 0.0f)));
                    area.AddCell(new(12, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NES_B.cell"), new(5760.0f, -3456.0f, 0.0f)));
                    area.AddCell(new(13, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_ES_A.cell"), new(8064.0f, -3456.0f, 0.0f)));
                    area.AddCell(new(10, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NE_A.cell"), new(1152.0f, -3456.0f, 0.0f)));
                    region.AddArea(area);

                    area = new(5, (AreaPrototype)GameDatabase.GetPrototypeId(LatveriaArea + "LatveriaPCZArea4.prototype"), new(), false);
                    area.AddCell(new(5, GameDatabase.GetPrototypeId(Latveria + "Courtyard_SafeArea_A/Courtyard_SafeArea_NESW_A.cell"), new(3456.0f, 10368.0f, 0.0f)));
                    area.AddCell(new(8, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NSW_C.cell"), new(3456.0f, 12672.0f, 0.0f)));
                    area.AddCell(new(4, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NEW_B.cell"), new(1152.0f, 10368.0f, 0.0f)));
                    area.AddCell(new(7, GameDatabase.GetPrototypeId(Latveria + "Courtyard_SafeArea_A/Courtyard_SafeArea_NW_A.cell"), new(1152.0f, 12672.0f, 0.0f)));
                    area.AddCell(new(6, GameDatabase.GetPrototypeId(Latveria + "Courtyard_Trans/Courtyard_Exit_ESW_A.cell"), new(5760.0f, 10368.0f, 0.0f)));
                    area.AddCell(new(9, GameDatabase.GetPrototypeId(Latveria + "Courtyard_SafeArea_A/Courtyard_SafeArea_SW_A.cell"), new(5760.0f, 12672.0f, 0.0f)));
                    area.AddCell(new(2, GameDatabase.GetPrototypeId(Latveria + "Courtyard_A/Courtyard_A_NESWcW_A.cell"), new(3456.0f, 8064.0f, 0.0f)));
                    area.AddCell(new(3, GameDatabase.GetPrototypeId(Latveria + "Courtyard_SafeArea_A/Courtyard_SafeArea_ES_A.cell"), new(5760.0f, 8064.0f, 0.0f)));
                    area.AddCell(new(1, GameDatabase.GetPrototypeId(Latveria + "Courtyard_SafeArea_A/Courtyard_SafeArea_NE_A.cell"), new(1152.0f, 8064.0f, 0.0f)));
                    region.AddArea(area);

                    region.EntrancePosition = new(-8064.0f, -10368.0f, 0.0f);
                    region.EntranceOrientation = new();
                    region.WaypointPosition = new(-8064.0f, -10368.0f, 0.0f);
                    region.WaypointOrientation = new();

                    break;

               case RegionPrototype.CH0808DoomCastleRegion:
                    archiveData = new byte[] {
                    };

                    region = new(RegionPrototype.CH0808DoomCastleRegion,
                        1901487720,
                        archiveData,
                        new(-9216.0f, -21120.0f, -3008.0f),
                        new(9216.0f, 21120.0f, 3008.0f),
                        new(60, DifficultyTier.Normal));

                    string Castle = "Resource/Cells/Latveria/Doomstadt_Castle_A/";
                    string CastleArea = "Regions/StoryRevamp/CH08Latveria/Areas/Latveria/CastleDoom/";
                    uint cellid = 1;
                    areaid = 1;

                    area = new(areaid++, (AreaPrototype)GameDatabase.GetPrototypeId(CastleArea + "Exterior/CastleExteriorAreaAEntry.prototype"), new(), true);
                    area.AddCell(new(cellid++, GameDatabase.GetPrototypeId(Castle + "Latveria_Castle_Exterior_A.cell"), new(6912.0f, -18816.0f, 192.0f)));
                    region.AddArea(area);
                    area = new(areaid++, (AreaPrototype)GameDatabase.GetPrototypeId(CastleArea + "Exterior/CastleExteriorAreaD.prototype"), new(), false);
                    area.AddCell(new(cellid++, GameDatabase.GetPrototypeId(Castle + "Latveria_Castle_Exterior_D.cell"), new(6912.0f, -14208.0f, 704.0f)));
                    region.AddArea(area);
                    area = new(areaid++, (AreaPrototype)GameDatabase.GetPrototypeId(CastleArea + "Exterior/CastleExteriorAreaF.prototype"), new(), false);
                    area.AddCell(new(cellid++, GameDatabase.GetPrototypeId(Castle + "Latveria_Castle_Exterior_F.cell"), new(6912.0f, -9600.0f, 704.0f)));
                    region.AddArea(area);
                    area = new(areaid++, (AreaPrototype)GameDatabase.GetPrototypeId(CastleArea + "Exterior/CastleExteriorBossArea.prototype"), new(), false);
                    area.AddCell(new(cellid++, GameDatabase.GetPrototypeId(Castle + "Latveria_Castle_Exterior_Boss.cell"), new(6912.0f, -4992.0f, 192.0f)));
                    region.AddArea(area);
                    area = new(areaid++, (AreaPrototype)GameDatabase.GetPrototypeId(CastleArea + "Exterior/CastleExteriorArrayArea.prototype"), new(), false);
                    area.AddCell(new(cellid++, GameDatabase.GetPrototypeId(Castle + "Latveria_Castle_Exterior_Array.cell"), new(6912.0f, -384.0f, 192.0f)));
                    region.AddArea(area);
                    area = new(areaid++, (AreaPrototype)GameDatabase.GetPrototypeId(CastleArea + "Exterior/CastleExteriorAreaGBridge.prototype"), new(), false);
                    area.AddCell(new(cellid++, GameDatabase.GetPrototypeId(Castle + "Latveria_Castle_Ext_G_Bridge.cell"), new(6912.0f, 4224.0f, 192.0f)));
                    region.AddArea(area);
                    area = new(areaid++, (AreaPrototype)GameDatabase.GetPrototypeId(CastleArea + "Interior/CastleInteriorAreaA.prototype"), new(), false);
                    area.AddCell(new(cellid++, GameDatabase.GetPrototypeId(Castle + "Latveria_Castle_Interior_A.cell"), new(6912.0f, 8832.0f, 320.0f)));
                    region.AddArea(area);
                    area = new(areaid++, (AreaPrototype)GameDatabase.GetPrototypeId(CastleArea + "Interior/CastleInteriorAreaB.prototype"), new(), false);
                    area.AddCell(new(cellid++, GameDatabase.GetPrototypeId(Castle + "Latveria_Castle_Interior_B.cell"), new(6912.0f, 13440.0f, 320.0f)));
                    region.AddArea(area);
                    area = new(areaid++, (AreaPrototype)GameDatabase.GetPrototypeId(CastleArea + "Interior/CastleInteriorAreaC.prototype"), new(), false);
                    area.AddCell(new(cellid++, GameDatabase.GetPrototypeId(Castle + "Latveria_Castle_Interior_C.cell"), new(6912.0f, 18048.0f, 320.0f)));
                    region.AddArea(area);
                    area = new(areaid++, (AreaPrototype)GameDatabase.GetPrototypeId(CastleArea + "Interior/CastleDoomBotEstablishingArea.prototype"), new(), false);
                    area.AddCell(new(cellid++, GameDatabase.GetPrototypeId(Castle + "Latveria_Castle_Interior_D2.cell"), new(2304.0f, 18816.0f, 320.0f)));
                    region.AddArea(area);
                    area = new(areaid++, (AreaPrototype)GameDatabase.GetPrototypeId(CastleArea + "Interior/CastleDoomBotFactoryArea.prototype"), new(), false);
                    area.AddCell(new(cellid++, GameDatabase.GetPrototypeId(Castle + "Latveria_Castle_Interior_D.cell"), new(-2304.0f, 18816.0f, 320.0f)));
                    region.AddArea(area);
                    area = new(areaid++, (AreaPrototype)GameDatabase.GetPrototypeId(CastleArea + "Interior/CastleInteriorAreaE.prototype"), new(), false);
                    area.AddCell(new(cellid++, GameDatabase.GetPrototypeId(Castle + "Latveria_Castle_Interior_E.cell"), new(-6912.0f, 18816.0f, 320.0f)));
                    region.AddArea(area);
                    area = new(areaid++, (AreaPrototype)GameDatabase.GetPrototypeId(CastleArea + "Interior/CastleInteriorAreaF.prototype"), new(), false);
                    area.AddCell(new(cellid++, GameDatabase.GetPrototypeId(Castle + "Latveria_Castle_Interior_F.cell"), new(-6912.0f, 14208.0f, 320.0f)));
                    region.AddArea(area);
                    area = new(areaid++, (AreaPrototype)GameDatabase.GetPrototypeId(CastleArea + "Interior/CastleInteriorPowerGenArea.prototype"), new(), false);
                    area.AddCell(new(cellid++, GameDatabase.GetPrototypeId(Castle + "Latveria_Castle_Interior_G.cell"), new(-2304.0f, 14208.0f, 320.0f)));
                    region.AddArea(area);
                    area = new(areaid++, (AreaPrototype)GameDatabase.GetPrototypeId(CastleArea + "Interior/CastleInteriorAreaH.prototype"), new(), false);
                    area.AddCell(new(cellid++, GameDatabase.GetPrototypeId(Castle + "Latveria_Castle_Interior_H.cell"), new(2304.0f, 14208.0f, 320.0f)));
                    region.AddArea(area);
                    area = new(areaid++, (AreaPrototype)GameDatabase.GetPrototypeId(CastleArea + "Interior/CastleElevatorToDoomArea.prototype"), new(), false);
                    area.AddCell(new(cellid++, GameDatabase.GetPrototypeId(Castle + "Latveria_Castle_Interior_I_2.cell"), new(2304.0f, 9600.0f, -704.0f)));
                    region.AddArea(area);

                    region.EntrancePosition = new(6016.0f, -18464.0f, 192.0f);
                    region.EntranceOrientation = new(-2.35623f, 0.0f, 0.0f);
                    region.WaypointPosition = new(6016.0f, -18464.0f, 192.0f);
                    region.WaypointOrientation = new(2.35623f, 0.0f, 0.0f);

                    break;

               case RegionPrototype.CH0901NorwayPCZRegion:
                    archiveData = new byte[] {
                    };

                    region = new(RegionPrototype.CH0901NorwayPCZRegion,
                        1901487720,
                        archiveData,
                        new(-13824.0f, -18432.0f, -1152.0f),
                        new(13824.0f, 18432.0f, 1152.0f),
                        new(60, DifficultyTier.Normal));

                    string Norway = "Resource/Cells/Asgard/Norway/";

                    area = new(1, (AreaPrototype)GameDatabase.GetPrototypeId("Regions/StoryRevamp/CH09Asgard/Areas/Norway/NorwayPCZAreaA.prototype"), new(), true);
                    area.AddCell(new(58, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_NESW_B.cell"), new(1152.0f, -5760.0f, 0.0f)));
                    area.AddCell(new(50, GameDatabase.GetPrototypeId(Norway + "Norway_Trans/Norway_ROAD_ES_A.cell"), new(-3456.0f, -8064.0f, 0.0f)));
                    area.AddCell(new(49, GameDatabase.GetPrototypeId(Norway + "Norway_Trans/Norway_ROAD_NW_A.cell"), new(-5760.0f, -8064.0f, 0.0f)));
                    area.AddCell(new(56, GameDatabase.GetPrototypeId(Norway + "Norway_C/Norway_OM_SHIELDOutpost_B.cell"), new(-3456.0f, -5760.0f, 0.0f)));
                    area.AddCell(new(72, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(3456.0f, -1152.0f, 0.0f)));
                    area.AddCell(new(62, GameDatabase.GetPrototypeId(Norway + "Norway_Trans/Norway_ROAD_NS_A.cell"), new(-3456.0f, -3456.0f, 0.0f)));
                    area.AddCell(new(51, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_NESW_A.cell"), new(-1152.0f, -8064.0f, 0.0f)));
                    area.AddCell(new(61, GameDatabase.GetPrototypeId(Norway + "Norway_C/Norway_OM_BossEvent_A.cell"), new(-5760.0f, -3456.0f, 0.0f)));
                    area.AddCell(new(71, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(-1152.0f, -12672.0f, 0.0f)));
                    area.AddCell(new(55, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_NESW_A.cell"), new(-5760.0f, -5760.0f, 0.0f)));
                    area.AddCell(new(52, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_NESW_B.cell"), new(1152.0f, -8064.0f, 0.0f)));
                    area.AddCell(new(43, GameDatabase.GetPrototypeId(Norway + "Norway_C/Norway_C_NESW_Entry_A.cell"), new(-5760.0f, -10368.0f, 0.0f)));
                    area.AddCell(new(63, GameDatabase.GetPrototypeId(Norway + "Norway_Trans/Norway_ROAD_ESW_A.cell"), new(-1152.0f, -3456.0f, 0.0f)));
                    area.AddCell(new(57, GameDatabase.GetPrototypeId(Norway + "Norway_C/Norway_OM_SHIELDOutpost_A.cell"), new(-1152.0f, -5760.0f, 0.0f)));
                    area.AddCell(new(64, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_NESWdNE_A.cell"), new(1152.0f, -3456.0f, 0.0f)));
                    area.AddCell(new(69, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_NESWcE_A.cell"), new(-1152.0f, -1152.0f, 0.0f)));
                    area.AddCell(new(67, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_NSW_A.cell"), new(-5760.0f, -1152.0f, 0.0f)));
                    area.AddCell(new(68, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_NSW_A.cell"), new(-3456.0f, -1152.0f, 0.0f)));
                    area.AddCell(new(46, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_NESWdSW_A.cell"), new(1152.0f, -10368.0f, 0.0f)));
                    area.AddCell(new(60, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_NEW_A.cell"), new(-8064.0f, -3456.0f, 0.0f)));
                    area.AddCell(new(48, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_NEW_A.cell"), new(-8064.0f, -8064.0f, 0.0f)));
                    area.AddCell(new(54, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_NEW_A.cell"), new(-8064.0f, -5760.0f, 0.0f)));
                    area.AddCell(new(42, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_NEW_A.cell"), new(-8064.0f, -10368.0f, 0.0f)));
                    area.AddCell(new(66, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_NW_A.cell"), new(-8064.0f, -1152.0f, 0.0f)));
                    area.AddCell(new(44, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_NESWdNW_A.cell"), new(-3456.0f, -10368.0f, 0.0f)));
                    area.AddCell(new(59, GameDatabase.GetPrototypeId(Norway + "Norway_C/Norway_TR_Entry_Portal_C.cell"), new(3456.0f, -5760.0f, 0.0f)));
                    area.AddCell(new(53, GameDatabase.GetPrototypeId(Norway + "Norway_C/Norway_TR_Entry_Portal_B.cell"), new(3456.0f, -8064.0f, 0.0f)));
                    area.AddCell(new(47, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_ESW_A.cell"), new(3456.0f, -10368.0f, 0.0f)));
                    area.AddCell(new(70, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_SW_A.cell"), new(1152.0f, -1152.0f, 0.0f)));
                    area.AddCell(new(38, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_NES_A.cell"), new(-5760.0f, -12672.0f, 0.0f)));
                    area.AddCell(new(65, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_SW_A.cell"), new(3456.0f, -3456.0f, 0.0f)));
                    area.AddCell(new(45, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_NES_B.cell"), new(-1152.0f, -10368.0f, 0.0f)));
                    area.AddCell(new(39, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_ES_A.cell"), new(-3456.0f, -12672.0f, 0.0f)));
                    area.AddCell(new(41, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_ES_A.cell"), new(3456.0f, -12672.0f, 0.0f)));
                    area.AddCell(new(40, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_NE_A.cell"), new(1152.0f, -12672.0f, 0.0f)));
                    area.AddCell(new(37, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_NE_A.cell"), new(-8064.0f, -12672.0f, 0.0f)));
                    region.AddArea(area);

                    area = new(2, (AreaPrototype)GameDatabase.GetPrototypeId("Regions/StoryRevamp/CH09Asgard/Areas/Norway/NorwayPCZAreaC.prototype"), new(), false);
                    area.AddCell(new(9, GameDatabase.GetPrototypeId(Norway + "Norway_C/Norway_Super_PyreOMEvent_A.cell"), new(1152.0f, 3456.0f, 0.0f)));
                    area.AddCell(new(17, GameDatabase.GetPrototypeId(Norway + "Norway_C/Norway_C_NESW_B.cell"), new(5760.0f, 5760.0f, 0.0f)));
                    area.AddCell(new(35, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(-3456.0f, 12672.0f, 0.0f)));
                    area.AddCell(new(3, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_NES_A.cell"), new(1152.0f, 1152.0f, 0.0f)));
                    area.AddCell(new(16, GameDatabase.GetPrototypeId(Norway + "Norway_Trans/Norway_ROAD_ES_A.cell"), new(3456.0f, 5760.0f, 0.0f)));
                    area.AddCell(new(8, GameDatabase.GetPrototypeId(Norway + "Norway_Trans/Norway_ROAD_NW_A.cell"), new(-1152.0f, 3456.0f, 0.0f)));
                    area.AddCell(new(28, GameDatabase.GetPrototypeId(Norway + "Norway_Trans/Norway_Asgard_Entry_A.cell"), new(3456.0f, 10368.0f, 0.0f)));
                    area.AddCell(new(22, GameDatabase.GetPrototypeId(Norway + "Norway_Trans/Norway_ROAD_EW_B.cell"), new(3456.0f, 8064.0f, 0.0f)));
                    area.AddCell(new(10, GameDatabase.GetPrototypeId(Norway + "Norway_C/Norway_C_NESW_A.cell"), new(3456.0f, 3456.0f, 0.0f)));
                    area.AddCell(new(21, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_NESW_A.cell"), new(1152.0f, 8064.0f, 0.0f)));
                    area.AddCell(new(23, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_NESW_B.cell"), new(5760.0f, 8064.0f, 0.0f)));
                    area.AddCell(new(14, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_NESW_A.cell"), new(-1152.0f, 5760.0f, 0.0f)));
                    area.AddCell(new(29, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_NESW_A.cell"), new(5760.0f, 10368.0f, 0.0f)));
                    area.AddCell(new(20, GameDatabase.GetPrototypeId(Norway + "Norway_Trans/Norway_Event_AreaC_A.cell"), new(-1152.0f, 8064.0f, 0.0f)));
                    area.AddCell(new(36, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(-1152.0f, 12672.0f, 0.0f)));
                    area.AddCell(new(15, GameDatabase.GetPrototypeId(Norway + "Norway_C/Norway_Super_PyreOMEvent_B.cell"), new(1152.0f, 5760.0f, 0.0f)));
                    area.AddCell(new(11, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_NESW_B.cell"), new(5760.0f, 3456.0f, 0.0f)));
                    area.AddCell(new(27, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_NESWdES_A.cell"), new(1152.0f, 10368.0f, 0.0f)));
                    area.AddCell(new(32, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_NSW_A.cell"), new(3456.0f, 12672.0f, 0.0f)));
                    area.AddCell(new(26, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_NSW_A.cell"), new(-1152.0f, 10368.0f, 0.0f)));
                    area.AddCell(new(33, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_NSW_A.cell"), new(5760.0f, 12672.0f, 0.0f)));
                    area.AddCell(new(7, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_NEW_A.cell"), new(-3456.0f, 3456.0f, 0.0f)));
                    area.AddCell(new(13, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_NEW_A.cell"), new(-3456.0f, 5760.0f, 0.0f)));
                    area.AddCell(new(19, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_NEW_A.cell"), new(-3456.0f, 8064.0f, 0.0f)));
                    area.AddCell(new(25, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_NW_A.cell"), new(-3456.0f, 10368.0f, 0.0f)));
                    area.AddCell(new(31, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_NW_A.cell"), new(1152.0f, 12672.0f, 0.0f)));
                    area.AddCell(new(18, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_ESW_A.cell"), new(8064.0f, 5760.0f, 0.0f)));
                    area.AddCell(new(24, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_ESW_A.cell"), new(8064.0f, 8064.0f, 0.0f)));
                    area.AddCell(new(12, GameDatabase.GetPrototypeId(Norway + "Norway_C/Norway_DarkForest_Entry.cell"), new(8064.0f, 3456.0f, 0.0f)));
                    area.AddCell(new(30, GameDatabase.GetPrototypeId(Norway + "Norway_C/Norway_TR_Entry_Portal.cell"), new(8064.0f, 10368.0f, 0.0f)));
                    area.AddCell(new(34, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_SW_A.cell"), new(8064.0f, 12672.0f, 0.0f)));
                    area.AddCell(new(2, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_NESWcW_A.cell"), new(-1152.0f, 1152.0f, 0.0f)));
                    area.AddCell(new(4, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_NES_A.cell"), new(3456.0f, 1152.0f, 0.0f)));
                    area.AddCell(new(5, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_NES_B.cell"), new(5760.0f, 1152.0f, 0.0f)));
                    area.AddCell(new(6, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_ES_A.cell"), new(8064.0f, 1152.0f, 0.0f)));
                    area.AddCell(new(1, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_NE_A.cell"), new(-3456.0f, 1152.0f, 0.0f)));
                    region.AddArea(area);

                    area = new(3, (AreaPrototype)GameDatabase.GetPrototypeId("DRAG/AreaGenerators/DynamicArea.prototype"), new(), false);
                    area.AddCell(new(73, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(-8064.0f, -14976.0f, 0.0f)));
                    area.AddCell(new(74, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(-5760.0f, -14976.0f, 0.0f)));
                    area.AddCell(new(75, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(-3456.0f, -14976.0f, 0.0f)));
                    area.AddCell(new(76, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(-1152.0f, -14976.0f, 0.0f)));
                    area.AddCell(new(77, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(1152.0f, -14976.0f, 0.0f)));
                    area.AddCell(new(78, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(-5760.0f, 1152.0f, 0.0f)));
                    area.AddCell(new(79, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(3456.0f, -14976.0f, 0.0f)));
                    area.AddCell(new(80, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(-8064.0f, 1152.0f, 0.0f)));
                    area.AddCell(new(81, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(5760.0f, -14976.0f, 0.0f)));
                    area.AddCell(new(82, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(-10368.0f, 1152.0f, 0.0f)));
                    area.AddCell(new(83, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(8064.0f, -14976.0f, 0.0f)));
                    area.AddCell(new(84, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(-12672.0f, 1152.0f, 0.0f)));
                    area.AddCell(new(85, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(5760.0f, -12672.0f, 0.0f)));
                    area.AddCell(new(86, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(-10368.0f, -1152.0f, 0.0f)));
                    area.AddCell(new(87, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(5760.0f, -10368.0f, 0.0f)));
                    area.AddCell(new(88, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(-10368.0f, -3456.0f, 0.0f)));
                    area.AddCell(new(89, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(5760.0f, -8064.0f, 0.0f)));
                    area.AddCell(new(90, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(-10368.0f, -5760.0f, 0.0f)));
                    area.AddCell(new(91, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(5760.0f, -5760.0f, 0.0f)));
                    area.AddCell(new(92, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(-10368.0f, -8064.0f, 0.0f)));
                    area.AddCell(new(93, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(5760.0f, -3456.0f, 0.0f)));
                    area.AddCell(new(94, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(-10368.0f, -10368.0f, 0.0f)));
                    area.AddCell(new(95, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(5760.0f, -1152.0f, 0.0f)));
                    area.AddCell(new(96, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(-10368.0f, -12672.0f, 0.0f)));
                    area.AddCell(new(97, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(-10368.0f, -14976.0f, 0.0f)));
                    area.AddCell(new(98, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(-10368.0f, -17280.0f, 0.0f)));
                    area.AddCell(new(99, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(-8064.0f, -17280.0f, 0.0f)));
                    area.AddCell(new(100, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(-5760.0f, -17280.0f, 0.0f)));
                    area.AddCell(new(101, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(-3456.0f, -17280.0f, 0.0f)));
                    area.AddCell(new(102, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(-1152.0f, -17280.0f, 0.0f)));
                    area.AddCell(new(103, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(1152.0f, -17280.0f, 0.0f)));
                    area.AddCell(new(104, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(-5760.0f, 3456.0f, 0.0f)));
                    area.AddCell(new(105, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(3456.0f, -17280.0f, 0.0f)));
                    area.AddCell(new(106, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(-8064.0f, 3456.0f, 0.0f)));
                    area.AddCell(new(107, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(5760.0f, -17280.0f, 0.0f)));
                    area.AddCell(new(108, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(-10368.0f, 3456.0f, 0.0f)));
                    area.AddCell(new(109, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(8064.0f, -17280.0f, 0.0f)));
                    area.AddCell(new(110, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(-12672.0f, 3456.0f, 0.0f)));
                    area.AddCell(new(111, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(8064.0f, -12672.0f, 0.0f)));
                    area.AddCell(new(112, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(-12672.0f, -1152.0f, 0.0f)));
                    area.AddCell(new(113, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(8064.0f, -10368.0f, 0.0f)));
                    area.AddCell(new(114, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(-12672.0f, -3456.0f, 0.0f)));
                    area.AddCell(new(115, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(8064.0f, -8064.0f, 0.0f)));
                    area.AddCell(new(116, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(-12672.0f, -5760.0f, 0.0f)));
                    area.AddCell(new(117, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(8064.0f, -5760.0f, 0.0f)));
                    area.AddCell(new(118, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(-12672.0f, -8064.0f, 0.0f)));
                    area.AddCell(new(119, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(8064.0f, -3456.0f, 0.0f)));
                    area.AddCell(new(120, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(-12672.0f, -10368.0f, 0.0f)));
                    area.AddCell(new(121, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(8064.0f, -1152.0f, 0.0f)));
                    area.AddCell(new(122, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(-12672.0f, -12672.0f, 0.0f)));
                    area.AddCell(new(123, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(-12672.0f, -14976.0f, 0.0f)));
                    area.AddCell(new(124, GameDatabase.GetPrototypeId(Norway + "Norway_A/Norway_A_FILLER_A.cell"), new(-12672.0f, -17280.0f, 0.0f)));
                    area.AddCell(new(125, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(8064.0f, 14976.0f, 0.0f)));
                    area.AddCell(new(126, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(5760.0f, 14976.0f, 0.0f)));
                    area.AddCell(new(127, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(3456.0f, 14976.0f, 0.0f)));
                    area.AddCell(new(128, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(1152.0f, 14976.0f, 0.0f)));
                    area.AddCell(new(129, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(-1152.0f, 14976.0f, 0.0f)));
                    area.AddCell(new(130, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(-3456.0f, 14976.0f, 0.0f)));
                    area.AddCell(new(131, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(10368.0f, -1152.0f, 0.0f)));
                    area.AddCell(new(132, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(-5760.0f, 14976.0f, 0.0f)));
                    area.AddCell(new(133, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(12672.0f, -1152.0f, 0.0f)));
                    area.AddCell(new(134, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(-8064.0f, 14976.0f, 0.0f)));
                    area.AddCell(new(135, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(10368.0f, 1152.0f, 0.0f)));
                    area.AddCell(new(136, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(-5760.0f, 12672.0f, 0.0f)));
                    area.AddCell(new(137, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(10368.0f, 3456.0f, 0.0f)));
                    area.AddCell(new(138, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(-5760.0f, 10368.0f, 0.0f)));
                    area.AddCell(new(139, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(10368.0f, 5760.0f, 0.0f)));
                    area.AddCell(new(140, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(-5760.0f, 8064.0f, 0.0f)));
                    area.AddCell(new(141, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(10368.0f, 8064.0f, 0.0f)));
                    area.AddCell(new(142, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(-5760.0f, 5760.0f, 0.0f)));
                    area.AddCell(new(143, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(10368.0f, 10368.0f, 0.0f)));
                    area.AddCell(new(144, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(10368.0f, 12672.0f, 0.0f)));
                    area.AddCell(new(145, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(10368.0f, 14976.0f, 0.0f)));
                    area.AddCell(new(146, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(10368.0f, 17280.0f, 0.0f)));
                    area.AddCell(new(147, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(8064.0f, 17280.0f, 0.0f)));
                    area.AddCell(new(148, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(5760.0f, 17280.0f, 0.0f)));
                    area.AddCell(new(149, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(3456.0f, 17280.0f, 0.0f)));
                    area.AddCell(new(150, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(1152.0f, 17280.0f, 0.0f)));
                    area.AddCell(new(151, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(-1152.0f, 17280.0f, 0.0f)));
                    area.AddCell(new(152, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(-3456.0f, 17280.0f, 0.0f)));
                    area.AddCell(new(153, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(10368.0f, -3456.0f, 0.0f)));
                    area.AddCell(new(154, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(-5760.0f, 17280.0f, 0.0f)));
                    area.AddCell(new(155, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(12672.0f, -3456.0f, 0.0f)));
                    area.AddCell(new(156, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(-8064.0f, 17280.0f, 0.0f)));
                    area.AddCell(new(157, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(12672.0f, 1152.0f, 0.0f)));
                    area.AddCell(new(158, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(-8064.0f, 12672.0f, 0.0f)));
                    area.AddCell(new(159, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(12672.0f, 3456.0f, 0.0f)));
                    area.AddCell(new(160, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(-8064.0f, 10368.0f, 0.0f)));
                    area.AddCell(new(161, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(12672.0f, 5760.0f, 0.0f)));
                    area.AddCell(new(162, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(-8064.0f, 8064.0f, 0.0f)));
                    area.AddCell(new(163, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(12672.0f, 8064.0f, 0.0f)));
                    area.AddCell(new(164, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(-8064.0f, 5760.0f, 0.0f)));
                    area.AddCell(new(165, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(12672.0f, 10368.0f, 0.0f)));
                    area.AddCell(new(166, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(12672.0f, 12672.0f, 0.0f)));
                    area.AddCell(new(167, GameDatabase.GetPrototypeId(Norway + "Norway_AreaC/Norway_AreaC_FILLER_A.cell"), new(12672.0f, 14976.0f, 0.0f)));
                    area.AddCell(new(168, GameDatabase.GetPrototypeId(Norway + "Norway_Common/Norway_Common_FILLER_A.cell"), new(12672.0f, 17280.0f, 0.0f)));
                    region.AddArea(area);

                    region.EntrancePosition = new(-5648.0f, -10752.0f, 0.0f);
                    region.EntranceOrientation = new();
                    region.WaypointPosition = new(-5648.0f, -10752.0f, 0.0f); 
                    region.WaypointOrientation = new(3.141640f, 0.0f, 0.0f);

                    break;

                case RegionPrototype.CH0904SiegePCZRegion:
                    archiveData = new byte[] {
                    };

                    region = new(RegionPrototype.CH0904SiegePCZRegion,
                        1901487720,
                        archiveData,
                        new(-9216.0f, -9216.0f, -1152.0f),
                        new(9216.0f, 9216.0f, 1152.0f),
                        new(60, DifficultyTier.Normal));

                    string SiegeCity = "Resource/Cells/Asgard/SiegePCZ/SiegeCity/";
                    area = new(1, (AreaPrototype) GameDatabase.GetPrototypeId("Regions/StoryRevamp/CH09Asgard/Areas/Asgard/LowerAsgard/SiegeS2NCityArea.prototype"), new(), true);
                    area.AddCell(new(26, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_Super/SiegeCity_SuperFour_X0Y1_A.cell"), new(1152.0f, 3456.0f, 0.0f)));
                    area.AddCell(new(35, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_A/SiegeCity_A_FILLER_A.cell"), new(-5760.0f, -5760.0f, 0.0f)));
                    area.AddCell(new(3, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCityTrans/Estate_TR_Entry_NES_A.cell"), new(1152.0f, -5760.0f, 0.0f)));
                    area.AddCell(new(19, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_S2N_B/SiegeCityS2N_B_NESW_B.cell"), new(-1152.0f, 1152.0f, 0.0f)));
                    area.AddCell(new(7, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_S2N_B/SiegeCityS2N_B_NESW_A.cell"), new(-1152.0f, -3456.0f, 0.0f)));
                    area.AddCell(new(15, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_S2N_C/SiegeCityS2N_C_NESW_A.cell"), new(3456.0f, -1152.0f, 0.0f)));
                    area.AddCell(new(13, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_Super/SiegeCity_Super_VerticalTop_A.cell"), new(-1152.0f, -1152.0f, 0.0f)));
                    area.AddCell(new(21, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_Super/SiegeCity_SuperFour_X1Y0_A.cell"), new(3456.0f, 1152.0f, 0.0f)));
                    area.AddCell(new(20, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_Super/SiegeCity_SuperFour_X0Y0_A.cell"), new(1152.0f, 1152.0f, 0.0f)));
                    area.AddCell(new(36, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_A/SiegeCity_A_FILLER_A.cell"), new(5760.0f, -5760.0f, 0.0f)));
                    area.AddCell(new(14, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCityTrans/SiegeCity_WP_NESW_A.cell"), new(1152.0f, -1152.0f, 0.0f)));
                    area.AddCell(new(30, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCityTrans/SiegeCity_POI_Ship_B.cell"), new(-3456.0f, 5760.0f, 0.0f)));
                    area.AddCell(new(12, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_Super/SiegeCity_Super_VerticalBot_A.cell"), new(-3456.0f, -1152.0f, 0.0f)));
                    area.AddCell(new(8, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_A/SiegeCity_A_NESW_A.cell"), new(1152.0f, -3456.0f, 0.0f)));
                    area.AddCell(new(25, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_A/SiegeCity_A_NESW_B.cell"), new(-1152.0f, 3456.0f, 0.0f)));
                    area.AddCell(new(27, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_Super/SiegeCity_SuperFour_X1Y1_A.cell"), new(3456.0f, 3456.0f, 0.0f)));
                    area.AddCell(new(18, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_A/SiegeCity_A_NESWdES_A.cell"), new(-3456.0f, 1152.0f, 0.0f)));
                    area.AddCell(new(32, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_S2N_D/SiegeCityS2N_D_NSW_C.cell"), new(1152.0f, 5760.0f, 0.0f)));
                    area.AddCell(new(31, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCityTrans/Restaurant_TR_Entry_NSW_A.cell"), new(-1152.0f, 5760.0f, 0.0f)));
                    area.AddCell(new(33, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_S2N_D/SiegeCityS2N_D_NSW_B.cell"), new(3456.0f, 5760.0f, 0.0f)));
                    area.AddCell(new(6, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_A/SiegeCity_A_NESWdSW_A.cell"), new(-3456.0f, -3456.0f, 0.0f)));
                    area.AddCell(new(24, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_A/SiegeCity_A_NESWdSW_A.cell"), new(-3456.0f, 3456.0f, 0.0f)));
                    area.AddCell(new(11, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_A/SiegeCity_A_NEW_C.cell"), new(-5760.0f, -1152.0f, 0.0f)));
                    area.AddCell(new(29, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_S2N_D/SiegeCityS2N_D_NW_A.cell"), new(-5760.0f, 5760.0f, 0.0f)));
                    area.AddCell(new(17, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_S2N_D/SiegeCityS2N_D_NW_B.cell"), new(-5760.0f, 1152.0f, 0.0f)));
                    area.AddCell(new(9, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_A/SiegeCity_A_NESWdNW_B.cell"), new(3456.0f, -3456.0f, 0.0f)));
                    area.AddCell(new(16, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_S2N_D/SiegeCityS2N_D_ESW_A.cell"), new(5760.0f, -1152.0f, 0.0f)));
                    area.AddCell(new(22, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_S2N_C/SiegeCityS2N_C_ESW_A.cell"), new(5760.0f, 1152.0f, 0.0f)));
                    area.AddCell(new(28, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCityTrans/SiegeCity_POI_Ship_A.cell"), new(5760.0f, 3456.0f, 0.0f)));
                    area.AddCell(new(34, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCityTrans/SiegeCity_OM_DoorDefense_A.cell"), new(5760.0f, 5760.0f, 0.0f)));
                    area.AddCell(new(2, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_A/SiegeCity_A_NES_C.cell"), new(-1152.0f, -5760.0f, 0.0f)));
                    area.AddCell(new(10, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_S2N_D/SiegeCityS2N_D_ES_B.cell"), new(5760.0f, -3456.0f, 0.0f)));
                    area.AddCell(new(4, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_S2N_D/SiegeCityS2N_D_ES_A.cell"), new(3456.0f, -5760.0f, 0.0f)));
                    area.AddCell(new(23, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCityTrans/SiegeCity_Entry_A.cell"), new(-5760.0f, 3456.0f, 0.0f)));
                    area.AddCell(new(5, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_A/SiegeCity_A_NE_A.cell"), new(-5760.0f, -3456.0f, 0.0f)));
                    area.AddCell(new(1, GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_A/SiegeCity_A_NE_B.cell"), new(-3456.0f, -5760.0f, 0.0f)));
                    region.AddArea(area);

                    ulong fillerSiege = GameDatabase.GetPrototypeId(SiegeCity + "SiegeCity_A/SiegeCity_A_FILLER_A.cell");

                    area = new(2, (AreaPrototype)GameDatabase.GetPrototypeId("DRAG/AreaGenerators/DynamicArea.prototype"), new(), false);

                    areaid = 37;
                    for (uint i = 0; i < 8; i++)
                        for (uint j = 0; j < 8; j++)
                            if (i == 0 || j == 0 || i == 7 || j == 7)
                                area.AddCell(new(areaid++, fillerSiege, new(-8064.0f + i * 2304.0f, -8064.0f + j * 2304.0f, 0.0f)));

                    region.AddArea(area);

                    region.EntrancePosition = new(-5760.0f, 3456.0f, 0.0f);
                    region.EntranceOrientation = new();
                    region.WaypointPosition = new(-5760.0f, 3456.0f, 0.0f);
                    region.WaypointOrientation = new();
                    break;

                case RegionPrototype.DailyGShockerSubwayRegionL60:

                    archiveData = new byte[] {
                    };

                    region = new(RegionPrototype.DailyGShockerSubwayRegionL60,
                        1901487720,
                        archiveData,
                        new(-5633f, -9600f, -2176f),
                        new(5633f, 9600f, 2176f),
                        new(11, DifficultyTier.Normal));

                    area = new(1, AreaPrototype.DailyGSubwayFactoryGen1Area, new(-3456.5f, -7424f, 0f), true);
                    area.AddCell(new(13, GameDatabase.GetPrototypeId("Resource/Cells/EndGame/DangerDailies/ShockerSubway/Daily_ShockerSubway_A_E_A.cell"), new()));
                    region.AddArea(area);

                    area = new(2, AreaPrototype.DailyGSubwayFactoryGen1Area, new(-3456.5f, -3072.001f, 0f), false);
                    area.AddCell(new(12, GameDatabase.GetPrototypeId("Resource/Cells/Hells_Kitchen_01/ShockerSubway/ShockerSubway_A/ShockerSubway_A_NW_A.cell"), new()));
                    region.AddArea(area);

                    area = new(3, AreaPrototype.DailyGSubwayFactoryGen1Area, new(-128.5f, -3072.001f, 0f), false);
                    area.AddCell(new(8, GameDatabase.GetPrototypeId("Resource/Cells/ReuseableInstances/Factory_A/Factory_A_NES_A.cell"), new()));
                    area.AddCell(new(9, GameDatabase.GetPrototypeId("Resource/Cells/ReuseableInstances/Factory_B/Factory_B_ES_A.cell"), new(2304f, 0f, 0f)));
                    area.AddCell(new(11, GameDatabase.GetPrototypeId("Resource/Cells/ReuseableInstances/Factory_A/Factory_A_ESW_A.cell"), new(2304f, 2304f, 0f)));
                    region.AddArea(area);

                    area = new(4, AreaPrototype.DailyGSubwayFactoryGen1Area, new(2175.5f, 1535.999f, 0f), false);
                    area.AddCell(new(2, GameDatabase.GetPrototypeId("Resource/Cells/ReuseableInstances/Factory_A/Factory_A_NEW_A.cell"), new()));
                    area.AddCell(new(3, GameDatabase.GetPrototypeId("Resource/Cells/ReuseableInstances/Factory_A/Factory_A_S_A.cell"), new(2304f, 0f, 0f)));
                    area.AddCell(new(4, GameDatabase.GetPrototypeId("Resource/Cells/ReuseableInstances/Factory_A/Factory_A_EW_A.cell"), new(0f, 2304f, 0f)));
                    area.AddCell(new(7, GameDatabase.GetPrototypeId("Resource/Cells/ReuseableInstances/Factory_A/Factory_A_FILLER_A.cell"), new(2304f, 2304f, 0f)));
                    area.AddCell(new(5, GameDatabase.GetPrototypeId("Resource/Cells/ReuseableInstances/Factory_B/Factory_B_NW_A.cell"), new(0f, 4608f, 0f)));
                    area.AddCell(new(6, GameDatabase.GetPrototypeId("Resource/Cells/ReuseableInstances/Factory_B/Factory_B_ES_A.cell"), new(2304f, 4608f, 0f)));
                    region.AddArea(area);

                    area = new(5, AreaPrototype.DailyGSubwayFactoryGen1Area, new(4480.5044f, 8448f, 0f), false);
                    area.AddCell(new(1, GameDatabase.GetPrototypeId("Resource/Cells/EndGame/DangerDailies/ShockerSubway/Daily_Shocker_Boss_A.cell"), new()));
                    region.AddArea(area);

                    region.EntrancePosition = new(-3376.5f, -8016f, 56f);
                    region.EntranceOrientation = new(1.5625f, 0f, 0f);
                    region.WaypointPosition = new(-3376.5f, -8016f, 56f);
                    region.WaypointOrientation = new();

                    break;

                case RegionPrototype.DailyGSinisterLabRegionL60:

                    archiveData = new byte[] {
                    };

                    region = new(RegionPrototype.DailyGSinisterLabRegionL60,
                        1901487720,
                        archiveData,
                        new(-1792.0f, -10752.0f, -1792.0f),
                        new(1792.0f, 10752.0f, 1792.0f),
                        new(11, DifficultyTier.Normal));

                    area = new(1, (AreaPrototype)GameDatabase.GetPrototypeId("Regions/EndGame/Terminals/Green/SinistersLab/Areas/DailyGSinisterLabEntryArea.prototype"), new(), true);
                    area.AddCell(new(6, GameDatabase.GetPrototypeId("Resource/Cells/EndGame/DangerDailies/SinisterLab/SinisterLabTerminal_Entry_A.cell"), new(0.0f, -8960.0f, 0.0f)));
                    region.AddArea(area);

                    area = new(2, (AreaPrototype)GameDatabase.GetPrototypeId("Regions/StoryRevamp/CH07SavageLand/Areas/MutateMarsh/MutateCaves/SinisterLab/SinisterLabBAreaReaverConstruction.prototype"), new(), false);                    
                    area.AddCell(new(5, GameDatabase.GetPrototypeId("Resource/Cells/Savagelands/SinisterLab/SinisterLab_A/SinisterLab_B.cell"), new(0.0f, -5376.0f, 0.0f)));
                    region.AddArea(area);

                    area = new(3, (AreaPrototype)GameDatabase.GetPrototypeId("Regions/StoryRevamp/CH07SavageLand/Areas/MutateMarsh/MutateCaves/SinisterLab/SinisterLabDAreaCloneMonitoring.prototype"), new(), false);                    
                    area.AddCell(new(4, GameDatabase.GetPrototypeId("Resource/Cells/Savagelands/SinisterLab/SinisterLab_A/SinisterLab_D.cell"), new(0.0f, -1792.0f, 0.0f)));
                    region.AddArea(area);

                    area = new(4, (AreaPrototype)GameDatabase.GetPrototypeId("Regions/StoryRevamp/CH07SavageLand/Areas/MutateMarsh/MutateCaves/SinisterLab/SinisterLabEAreaGeneticResearch.prototype"), new(), false);
                    area.AddCell(new(3, GameDatabase.GetPrototypeId("Resource/Cells/Savagelands/SinisterLab/SinisterLab_A/SinisterLab_E.cell"), new(0.0f, 1792.0f, 0.0f)));
                    region.AddArea(area);

                    area = new(5, (AreaPrototype)GameDatabase.GetPrototypeId("Regions/StoryRevamp/CH07SavageLand/Areas/MutateMarsh/MutateCaves/SinisterLab/SinisterLabGAreaCommunications.prototype"), new(), false);
                    area.AddCell(new(2, GameDatabase.GetPrototypeId("Resource/Cells/Savagelands/SinisterLab/SinisterLab_A/SinisterLab_G.cell"), new(0.0f, 5376.0f, 0.0f)));
                    region.AddArea(area);

                    area = new(6, (AreaPrototype)GameDatabase.GetPrototypeId("Regions/EndGame/Terminals/Green/SinistersLab/Areas/DailyGSinisterLabBossArea.prototype"), new(), false);
                    area.AddCell(new(1, GameDatabase.GetPrototypeId("Resource/Cells/EndGame/DangerDailies/SinisterLab/SinisterLabTerminal_Boss_A.cell"), new(0.0f, 8960.0f, 0.0f)));
                    region.AddArea(area);

                    region.EntrancePosition = new(1000.0f, -10100.0f, 0.0f);
                    region.EntranceOrientation = new(3.14159f, 0f, 0f);
                    region.WaypointPosition = new(-500.0f, 6025.0f, 0.0f);
                    // 0.0f, 10060.0f, 0.0f boss
                    region.WaypointOrientation = new();
                    
                    break;
                    
                case RegionPrototype.BronxZooRegionL60:
                    archiveData = new byte[] {
                    };
                    region = new(RegionPrototype.BronxZooRegionL60,
                        1038711701,
                        archiveData,
                        new(-4480.0f, -10944.0f, -1152.0f),
                        new(20864.0f, 2880.0f, 1152.0f),
                        new(60, DifficultyTier.Normal));

                    string Rzoo = "Resource/Cells/Bronx_Zoo/";
                    
                    area = new(1, (AreaPrototype) GameDatabase.GetPrototypeId("Regions/EndGame/OneShotMissions/NonChapterBound/BronxZoo/ZooAreas/ZooArea1SN.prototype"), new(), true);

                    area.AddCell(new(24, GameDatabase.GetPrototypeId(Rzoo+"Bronx_Zoo_Trans/Bronx_Zoo_NE_Entry_A.cell"), new(-1024.0f, -7488.0f, 0.0f)));
                    area.AddCell(new(9, GameDatabase.GetPrototypeId(Rzoo+"Bronx_Zoo_Trans/Bronx_Zoo_NEW_Entry_A.cell"), new(-1024.0f, -5184.0f, 0.0f)));
                    area.AddCell(new(13, GameDatabase.GetPrototypeId(Rzoo+ "Bronx_Zoo_Pens_B/Bronx_Zoo_OpenPen_NEW_A.cell"), new(-1024.0f, -2880.0f, 0.0f))); // Bronx_Zoo_A/Bronx_Zoo_A_NEW_B.cell
                    area.AddCell(new(14, GameDatabase.GetPrototypeId(Rzoo+"Bronx_Zoo_Trans/Bronx_Zoo_TR_Stadium_Entry_A.cell"), new(-1024.0f, -576.0f, 0.0f)));
                    area.AddCell(new(22, GameDatabase.GetPrototypeId(Rzoo+"Bronx_Zoo_Trans/Bronx_Zoo_NES_Entry_A.cell"), new(1280.0f, -7488.0f, 0.0f)));
                    area.AddCell(new(3, GameDatabase.GetPrototypeId(Rzoo+"Bronx_Zoo_NESW_Attractions_A/Bronx_Zoo_FoodCourt_NESW_A.cell"), new(1280.0f, -5184.0f, 0.0f)));
                    area.AddCell(new(5, GameDatabase.GetPrototypeId(Rzoo+"Bronx_Zoo_NESW_Attractions_A/Bronx_Zoo_Carousel_NESW_A.cell"), new(1280.0f, -2880.0f, 0.0f)));
                    area.AddCell(new(11, GameDatabase.GetPrototypeId(Rzoo+"Bronx_Zoo_A/Bronx_Zoo_A_NSW_B.cell"), new(1280.0f, -576.0f, 0.0f)));

                    area.AddCell(new(19, GameDatabase.GetPrototypeId(Rzoo+ "Bronx_Zoo_Pens_B/Bronx_Zoo_OpenPen_NES_A.cell"), new(3584.0f, -7488.0f, 0.0f)));//Bronx_Zoo_A/Bronx_Zoo_A_NES_B.cell
                    area.AddCell(new(21, GameDatabase.GetPrototypeId(Rzoo+"Bronx_Zoo_A/Bronx_Zoo_A_NES_A.cell"), new(5888.0f, -7488.0f, 0.0f)));
                    area.AddCell(new(20, GameDatabase.GetPrototypeId(Rzoo+ "Bronx_Zoo_A/Bronx_Zoo_A_NES_B.cell"), new(8192.0f, -7488.0f, 0.0f)));//Bronx_Zoo_Pens_B/Bronx_Zoo_OpenPen_NES_A.cell
                    area.AddCell(new(23, GameDatabase.GetPrototypeId(Rzoo+"Bronx_Zoo_Trans/Bronx_Zoo_TR_Employee_Entry_A.cell"), new(10496.0f, -7488.0f, 0.0f)));  

                    area.AddCell(new(1, GameDatabase.GetPrototypeId(Rzoo+"Bronx_Zoo_A/Bronx_Zoo_A_NESW_A.cell"), new(8192.0f, -2880.0f, 0.0f)));
                    area.AddCell(new(4, GameDatabase.GetPrototypeId(Rzoo+"Bronx_Zoo_NESW_Attractions_A/Bronx_Zoo_Theater_NESW_A.cell"), new(8192.0f, -5184.0f, 0.0f)));                    
                    area.AddCell(new(6, GameDatabase.GetPrototypeId(Rzoo+"Bronx_Zoo_A/Bronx_Zoo_A_NSW_A.cell"), new(5888.0f, -576.0f, 0.0f)));

                    area.AddCell(new(7, GameDatabase.GetPrototypeId(Rzoo+"Bronx_Zoo_Pens_A/Bronx_Zoo_Pen_TopLeft_A.cell"), new(5888.0f, -5184.0f, 0.0f)));
                    area.AddCell(new(12, GameDatabase.GetPrototypeId(Rzoo+"Bronx_Zoo_Pens_A/Bronx_Zoo_Pen_TopRight_A.cell"), new(5888.0f, -2880.0f, 0.0f)));
                    area.AddCell(new(2, GameDatabase.GetPrototypeId(Rzoo+"Bronx_Zoo_Pens_A/Bronx_Zoo_Pen_BotLeft_A.cell"), new(3584.0f, -5184.0f, 0.0f)));                    
                    area.AddCell(new(15, GameDatabase.GetPrototypeId(Rzoo+"Bronx_Zoo_Pens_A/Bronx_Zoo_Pen_BotRight_A.cell"), new(3584.0f, -2880.0f, 0.0f)));
                    
                    area.AddCell(new(8, GameDatabase.GetPrototypeId(Rzoo+"Bronx_Zoo_A/Bronx_Zoo_A_NSW_B.cell"), new(8192.0f, -576.0f, 0.0f)));                 
                    area.AddCell(new(10, GameDatabase.GetPrototypeId(Rzoo+"Bronx_Zoo_Pens_B/Bronx_Zoo_OpenPen_NSW_A.cell"), new(3584.0f, -576.0f, 0.0f)));                    
                                        
                    area.AddCell(new(16, GameDatabase.GetPrototypeId(Rzoo+"Bronx_Zoo_A/Bronx_Zoo_A_NESWcN_A.cell"), new(10496.0f, -5184.0f, 0.0f))); 
                    area.AddCell(new(17, GameDatabase.GetPrototypeId(Rzoo+ "Bronx_Zoo_A/Bronx_Zoo_A_NESWdNE_A.cell"), new(10496.0f, -2880.0f, 0.0f))); // Bronx_Zoo_Pens_B/Bronx_Zoo_OpenPen_ESW_A
                    area.AddCell(new(18, GameDatabase.GetPrototypeId(Rzoo+"Bronx_Zoo_Trans/Bronx_Zoo_TR_Aquarium_Entry_A.cell"), new(10496.0f, -576.0f, 0.0f)));
                  
                    region.AddArea(area);

                    ulong filler = GameDatabase.GetPrototypeId(Rzoo + "Bronx_Zoo_A/Bronx_Zoo_A_FILLER_A.cell");

                    // fillers for ZooArea1SN

                    area = new(10, (AreaPrototype)GameDatabase.GetPrototypeId("DRAG/AreaGenerators/DynamicArea.prototype"), new(), false);
                    area.AddCell(new(201, filler, new(-1024.0f, -9792.0f, 0.0f)));
                    area.AddCell(new(202, filler, new(10496.0f, 1728.0f, 0.0f)));
                    area.AddCell(new(203, filler, new(1280.0f, -9792.0f, 0.0f)));
                    area.AddCell(new(204, filler, new(8192.0f, 1728.0f, 0.0f)));
                    area.AddCell(new(205, filler, new(3584.0f, -9792.0f, 0.0f)));
                    area.AddCell(new(206, filler, new(5888.0f, 1728.0f, 0.0f)));
                    area.AddCell(new(207, filler, new(5888.0f, -9792.0f, 0.0f)));
                    area.AddCell(new(208, filler, new(3584.0f, 1728.0f, 0.0f)));
                    area.AddCell(new(209, filler, new(8192.0f, -9792.0f, 0.0f)));
                    area.AddCell(new(210, filler, new(1280.0f, 1728.0f, 0.0f)));
                    area.AddCell(new(211, filler, new(10496.0f, -9792.0f, 0.0f)));
                    area.AddCell(new(212, filler, new(-1024.0f, 1728.0f, 0.0f)));
                    area.AddCell(new(213, filler, new(12800.0f, -9792.0f, 0.0f)));
                    area.AddCell(new(214, filler, new(-3328.0f, 1728.0f, 0.0f)));
                    area.AddCell(new(215, filler, new(-3328.0f, -576.0f, 0.0f)));
                    area.AddCell(new(216, filler, new(-3328.0f, -2880.0f, 0.0f)));
                    area.AddCell(new(217, filler, new(-3328.0f, -5184.0f, 0.0f)));
                    area.AddCell(new(218, filler, new(12800.0f, -576.0f, 0.0f)));
                    area.AddCell(new(219, filler, new(-3328.0f, -7488.0f, 0.0f)));
                    area.AddCell(new(220, filler, new(12800.0f, 1728.0f, 0.0f)));
                    area.AddCell(new(221, filler, new(-3328.0f, -9792.0f, 0.0f)));
                    region.AddArea(area);

                    area = new(2, (AreaPrototype) GameDatabase.GetPrototypeId("Regions/EndGame/OneShotMissions/NonChapterBound/BronxZoo/ZooAreas/ZooArea2.prototype"), new(), false);
                    area.AddCell(new(122, GameDatabase.GetPrototypeId(Rzoo + "Bronx_Zoo_Trans/Bronx_Zoo_CagedCivisMission_A.cell"), new(15104.0f, -5184.0f, 0.0f)));
                    area.AddCell(new(125, GameDatabase.GetPrototypeId(Rzoo + "Bronx_Zoo_Trans/Bronx_Zoo_NSW_JungleExit_A.cell"), new(15104.0f, -2880.0f, 0.0f)));
                    area.AddCell(new(121, GameDatabase.GetPrototypeId(Rzoo + "Bronx_Zoo_A/Bronx_Zoo_A_NESWcS_A.cell"), new(12800.0f, -5184.0f, 0.0f)));
                    area.AddCell(new(124, GameDatabase.GetPrototypeId(Rzoo + "Bronx_Zoo_Trans/Bronx_Zoo_NW_JungleExit_A.cell"), new(12800.0f, -2880.0f, 0.0f)));
                    area.AddCell(new(123, GameDatabase.GetPrototypeId(Rzoo + "Bronx_Zoo_Pens_B/Bronx_Zoo_OpenPen_ESW_A.cell"), new(17408.0f, -5184.0f, 0.0f)));
                    area.AddCell(new(126, GameDatabase.GetPrototypeId(Rzoo + "Bronx_Zoo_Trans/Bronx_Zoo_SW_JungleExit_A.cell"), new(17408.0f, -2880.0f, 0.0f)));
                    area.AddCell(new(119, GameDatabase.GetPrototypeId(Rzoo + "Bronx_Zoo_A/Bronx_Zoo_A_NES_B.cell"), new(15104.0f, -7488.0f, 0.0f))); // Bronx_Zoo_A/Bronx_Zoo_A_NES_B.cell
                    area.AddCell(new(120, GameDatabase.GetPrototypeId(Rzoo + "Bronx_Zoo_A/Bronx_Zoo_A_ES_A.cell"), new(17408.0f, -7488.0f, 0.0f)));
                    area.AddCell(new(118, GameDatabase.GetPrototypeId(Rzoo + "Bronx_Zoo_A/Bronx_Zoo_A_NE_A.cell"), new(12800.0f, -7488.0f, 0.0f)));
                    region.AddArea(area);

                    // fillers for ZooArea2

                    area = new(11, (AreaPrototype) GameDatabase.GetPrototypeId("DRAG/AreaGenerators/DynamicArea.prototype"), new(), false);
                    area.AddCell(new(222, filler, new(17408.0f, -576.0f, 0.0f)));
                    area.AddCell(new(223, filler, new(15104.0f, -9792.0f, 0.0f)));
                    area.AddCell(new(224, filler, new(15104.0f, -576.0f, 0.0f)));
                    area.AddCell(new(225, filler, new(17408.0f, -9792.0f, 0.0f)));
                    area.AddCell(new(226, filler, new(19712.0f, -9792.0f, 0.0f)));
                    area.AddCell(new(227, filler, new(19712.0f, -7488.0f, 0.0f)));
                    area.AddCell(new(228, filler, new(19712.0f, -5184.0f, 0.0f)));
                    area.AddCell(new(229, filler, new(19712.0f, -2880.0f, 0.0f)));
                    area.AddCell(new(230, filler, new(19712.0f, -576.0f, 0.0f)));
                    region.AddArea(area);

                    region.EntrancePosition = new(-1024.0f, -8100.0f, 0.0f); 
                    region.EntranceOrientation = new(1.5625f, 0f, 0f); ;
                    region.WaypointPosition = new(15104.0f, -3100.0f, 0.0f);
                    region.WaypointOrientation = new();
                    
                    break;

                case RegionPrototype.UltronRaidRegionGreen:
                    archiveData = new byte[] {
                    };

                    region = new(RegionPrototype.UltronRaidRegionGreen,
                        1883928786,
                        archiveData,
                        new(-1152.0f, -1152.0f, -1152.0f),
                        new(19584.0f, 19584.0f, 1152.0f),
                        new(60, DifficultyTier.Normal));                    

                    AreaPrototype CentralPark = (AreaPrototype)GameDatabase.GetPrototypeId("Regions/EndGame/TierX/UltronGameMode/UltronRaidMainArea.prototype");
                    area = new(1, CentralPark, new(), true);

                    district = GameDatabase.Resource.DistrictDict["Resource/Districts/CentralParkUltronDistrict.district"];

                    for (int i = 0; i < district.CellMarkerSet.Length; i++)
                        area.AddCell(new((uint)i + 1, GameDatabase.GetPrototypeId(district.CellMarkerSet[i].Resource), new()));

                    region.AddArea(area);

                    region.EntrancePosition = new(4900.0f, 100.0f, 0.0f);
                    region.EntranceOrientation = new(1.570796f, 0.0f, 0.0f); 
                    region.WaypointPosition =  new(16428.0f, 11820.0f, 0.0f);
                    region.WaypointOrientation = new(1.570796f, 0.0f, 0.0f);

                    break;

                case RegionPrototype.CosmicDoopSectorSpaceRegion:
                    archiveData = new byte[] {
                    };

                    region = new(RegionPrototype.CosmicDoopSectorSpaceRegion,
                        1883928786,
                        archiveData,
                        new(-3456.0f, -8064.0f, -1152.0f),
                        new(3456.0f, 8064.0f, 1152.0f),
                        new(60, DifficultyTier.Normal));

                    string DoopSector = "Resource/Cells/EndGame/CosmicDoopSector/";
                    area = new(1, (AreaPrototype)GameDatabase.GetPrototypeId("Regions/EndGame/Special/CosmicDoopSectorSpace/CosmicDoopSectorSpaceAreaA.prototype"), new(), true);
                    area.AddCell(new(13, GameDatabase.GetPrototypeId(DoopSector + "CosmicDoopSector_A/CosmicDoopSector_NESW_A.cell"), new(0.0f, 0.0f, 0.0f)));
                    area.AddCell(new(16, GameDatabase.GetPrototypeId(DoopSector + "CosmicDoopSector_A/CosmicDoopSector_NESW_B.cell"), new(0.0f, 2304.0f, 0.0f)));
                    area.AddCell(new(7, GameDatabase.GetPrototypeId(DoopSector + "CosmicDoopSector_A/CosmicDoopSector_NESW_C.cell"), new(0.0f, -4608.0f, 0.0f)));
                    area.AddCell(new(19, GameDatabase.GetPrototypeId(DoopSector + "CosmicDoopSector_A/CosmicDoopSector_NESW_B.cell"), new(0.0f, 4608.0f, 0.0f)));
                    area.AddCell(new(10, GameDatabase.GetPrototypeId(DoopSector + "CosmicDoopSector_A/CosmicDoopSector_NESW_A.cell"), new(0.0f, -2304.0f, 0.0f)));
                    area.AddCell(new(18, GameDatabase.GetPrototypeId(DoopSector + "CosmicDoopSector_A/CosmicDoopSector_NEW_A.cell"), new(-2304.0f, 4608.0f, 0.0f)));
                    area.AddCell(new(22, GameDatabase.GetPrototypeId(DoopSector + "CosmicDoopSector_A/CosmicDoopSector_NSW_A.cell"), new(0.0f, 6912.0f, 0.0f)));
                    area.AddCell(new(6, GameDatabase.GetPrototypeId(DoopSector + "CosmicDoopSector_A/CosmicDoopSector_NEW_A.cell"), new(-2304.0f, -4608.0f, 0.0f)));
                    area.AddCell(new(12, GameDatabase.GetPrototypeId(DoopSector + "CosmicDoopSector_A/CosmicDoopSector_NEW_A.cell"), new(-2304.0f, 0.0f, 0.0f)));
                    area.AddCell(new(15, GameDatabase.GetPrototypeId(DoopSector + "CosmicDoopSector_A/CosmicDoopSector_NEW_A.cell"), new(-2304.0f, 2304.0f, 0.0f)));
                    area.AddCell(new(9, GameDatabase.GetPrototypeId(DoopSector + "CosmicDoopSector_A/CosmicDoopSector_NEW_A.cell"), new(-2304.0f, -2304.0f, 0.0f)));
                    area.AddCell(new(21, GameDatabase.GetPrototypeId(DoopSector + "CosmicDoopSector_A/CosmicDoopSector_NW_A.cell"), new(-2304.0f, 6912.0f, 0.0f)));
                    area.AddCell(new(14, GameDatabase.GetPrototypeId(DoopSector + "CosmicDoopSector_A/CosmicDoopSector_ESW_A.cell"), new(2304.0f, 0.0f, 0.0f)));
                    area.AddCell(new(11, GameDatabase.GetPrototypeId(DoopSector + "CosmicDoopSector_A/CosmicDoopSector_ESW_A.cell"), new(2304.0f, -2304.0f, 0.0f)));
                    area.AddCell(new(20, GameDatabase.GetPrototypeId(DoopSector + "CosmicDoopSector_A/CosmicDoopSector_ESW_A.cell"), new(2304.0f, 4608.0f, 0.0f)));
                    area.AddCell(new(8, GameDatabase.GetPrototypeId(DoopSector + "CosmicDoopSector_A/CosmicDoopSector_ESW_A.cell"), new(2304.0f, -4608.0f, 0.0f)));
                    area.AddCell(new(17, GameDatabase.GetPrototypeId(DoopSector + "CosmicDoopSector_A/CosmicDoopSector_ESW_A.cell"), new(2304.0f, 2304.0f, 0.0f)));
                    area.AddCell(new(23, GameDatabase.GetPrototypeId(DoopSector + "CosmicDoopSector_A/CosmicDoopSector_SW_A.cell"), new(2304.0f, 6912.0f, 0.0f)));
                    area.AddCell(new(4, GameDatabase.GetPrototypeId(DoopSector + "CosmicDoopSectorTrans/CosmicDoopSector_Entry_A.cell"), new(0.0f, -6912.0f, 0.0f)));
                    area.AddCell(new(5, GameDatabase.GetPrototypeId(DoopSector + "CosmicDoopSector_A/CosmicDoopSector_ES_A.cell"), new(2304.0f, -6912.0f, 0.0f)));
                    area.AddCell(new(3, GameDatabase.GetPrototypeId(DoopSector + "CosmicDoopSector_A/CosmicDoopSector_NE_A.cell"), new(-2304.0f, -6912.0f, 0.0f)));
                    region.AddArea(area);

                    region.EntrancePosition = new(0.0f, -6912.0f, 0.0f);
                    region.EntranceOrientation = new();
                    region.WaypointPosition = new(0.0f, -6912.0f, 0.0f);
                    region.WaypointOrientation = new();

                    break;

                case RegionPrototype.BrooklynPatrolRegionL60:

                    archiveData = new byte[] {
                    };

                    region = new(RegionPrototype.BrooklynPatrolRegionL60,
                        1883928786,
                        archiveData,
                        new(-11520.0f, -8064.0f, -1152.0f),
                        new(11520.0f, 8064.0f, 1152.0f),
                        new(10, DifficultyTier.Normal));

                    string DocksPatrol = "Resource/Cells/EndGame/BrooklynDocksPatrol/";
                    string Shipping_C = "Resource/Cells/Brooklyn/Shipping_C/";

                    area = new(1, (AreaPrototype)GameDatabase.GetPrototypeId("Regions/EndGame/TierX/PatrolBrooklyn/Areas/DocksPatrolAreaA.prototype"), new(), true);
                    area.AddCell(new(22, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_Dockyard_A/DP_Dockyard_A_ES_A.cell"), new(8064.0f, -2304.0f, 0.0f)));
                    area.AddCell(new(30, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_WareSuper_C_Bot.cell"), new(5760.0f, 4608.0f, 0.0f)));
                    area.AddCell(new(21, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_WareSuper_B_Top.cell"), new(5760.0f, -2304.0f, 0.0f)));
                    area.AddCell(new(29, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_Dockyard_A/DP_Dockyard_A_NW_A.cell"), new(3456.0f, 4608.0f, 0.0f)));
                    area.AddCell(new(20, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_WareSuper_B_Bot.cell"), new(3456.0f, -2304.0f, 0.0f)));
                    area.AddCell(new(28, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_WareSuper_Top_A.cell"), new(8064.0f, 2304.0f, 0.0f)));
                    area.AddCell(new(31, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_WareSuper_C_Top.cell"), new(8064.0f, 4608.0f, 0.0f)));
                    area.AddCell(new(25, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_Dockyard_A/DP_Dockyard_A_ESW_A.cell"), new(8064.0f, 0.0f, 0.0f)));
                    area.AddCell(new(26, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_WareSuper_Bot_A.cell"), new(3456.0f, 2304.0f, 0.0f)));
                    area.AddCell(new(24, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_FoodTrucks_A.cell"), new(5760.0f, 0.0f, 0.0f)));
                    area.AddCell(new(23, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_Dockyard_A/DP_Dockyard_A_NESW_A.cell"), new(3456.0f, 0.0f, 0.0f)));
                    area.AddCell(new(27, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_WareSuper_Center_A.cell"), new(5760.0f, 2304.0f, 0.0f)));
                    region.AddArea(area);

                    area = new(2, (AreaPrototype)GameDatabase.GetPrototypeId("Regions/EndGame/TierX/PatrolBrooklyn/Areas/DocksPatrolBridgeTransitionNS.prototype"), new(), false);
                    area.AddCell(new(17, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_BridgeA_Left_A.cell"), new(1152.0f, -2304.0f, 0.0f)));
                    area.AddCell(new(18, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_BridgeA_Center_A.cell"), new(1152.0f, 0.0f, 0.0f)));
                    area.AddCell(new(19, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_BridgeA_Right_A.cell"), new(1152.0f, 2304.0f, 0.0f)));
                    region.AddArea(area);

                    area = new(4, (AreaPrototype)GameDatabase.GetPrototypeId("Regions/EndGame/TierX/PatrolBrooklyn/Areas/DocksPatrolAreaB.prototype"), new(), false);
                    area.AddCell(new(1, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_Shipping_A/DP_Shipping_A_NE_A.cell"), new(-8064.0f, -4608.0f, 0.0f)));
                    area.AddCell(new(9, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_Shipping_A/DP_Shipping_A_NEW_A.cell"), new(-8064.0f, 0.0f, 0.0f)));
                    area.AddCell(new(4, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_Shipping_A/DP_Shipping_A_ES_A.cell"), new(-1152.0f, -4608.0f, 0.0f)));
                    area.AddCell(new(2, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_Shipping_A/DP_Shipping_A_NES_A.cell"), new(-5760.0f, -4608.0f, 0.0f)));
                    area.AddCell(new(3, GameDatabase.GetPrototypeId(DocksPatrol + "DP_Shipping_A_NES_B.cell"), new(-3456.0f, -4608.0f, 0.0f)));
                    area.AddCell(new(13, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_CargoShip_A_1.cell"), new(-8064.0f, 2304.0f, 0.0f)));
                    area.AddCell(new(5, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_Shipping_A/DP_Shipping_A_NEW_A.cell"), new(-8064.0f, -2304.0f, 0.0f)));
                    area.AddCell(new(15, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_CargoShip_A_3.cell"), new(-3456.0f, 2304.0f, 0.0f)));
                    area.AddCell(new(14, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_CargoShip_A_0.cell"), new(-5760.0f, 2304.0f, 0.0f)));
                    area.AddCell(new(16, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_Shipping_A/DP_Shipping_A_NSW_A.cell"), new(-1152.0f, 2304.0f, 0.0f)));
                    area.AddCell(new(12, GameDatabase.GetPrototypeId(Shipping_C + "Shipping_AIMSub_X1_Y0_A.cell"), new(-1152.0f, 0.0f, 0.0f)));
                    area.AddCell(new(7, GameDatabase.GetPrototypeId(Shipping_C + "Shipping_CrateWorld_X1_Y0_A.cell"), new(-3456.0f, -2304.0f, 0.0f)));
                    area.AddCell(new(11, GameDatabase.GetPrototypeId(Shipping_C + "Shipping_AIMSub_X0_Y0_A.cell"), new(-3456.0f, 0.0f, 0.0f)));
                    area.AddCell(new(10, GameDatabase.GetPrototypeId(Shipping_C + "DocksPatrol_CrateWorld_X0_Y1_A.cell"), new(-5760.0f, 0.0f, 0.0f)));
                    area.AddCell(new(6, GameDatabase.GetPrototypeId(Shipping_C + "DocksPatrol_CrateWorld_X0_Y0_A.cell"), new(-5760.0f, -2304.0f, 0.0f)));
                    area.AddCell(new(8, GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_Shipping_A/DP_Shipping_A_NESW_A.cell"), new(-1152.0f, -2304.0f, 0.0f)));
                    region.AddArea(area);

                    // Filler
                    ulong Dockyard_Filler = GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_Dockyard_A/DP_Dockyard_A_FILLER_A.cell");
                    ulong Shipping_Filler = GameDatabase.GetPrototypeId(DocksPatrol + "DocksPatrol_Shipping_A/DP_Shipping_A_FILLER_A.cell");

                    area = new(5, (AreaPrototype)GameDatabase.GetPrototypeId("DRAG/AreaGenerators/DynamicArea.prototype"), new(), false);
                    area.AddCell(new(32, Dockyard_Filler, new(3456.0f, -4608.0f, 0.0f)));
                    area.AddCell(new(33, Dockyard_Filler, new(8064.0f, 6912.0f, 0.0f)));
                    area.AddCell(new(34, Dockyard_Filler, new(5760.0f, -4608.0f, 0.0f)));
                    area.AddCell(new(35, Dockyard_Filler, new(5760.0f, 6912.0f, 0.0f)));
                    area.AddCell(new(36, Dockyard_Filler, new(8064.0f, -4608.0f, 0.0f)));
                    area.AddCell(new(37, Dockyard_Filler, new(3456.0f, 6912.0f, 0.0f)));
                    area.AddCell(new(38, Dockyard_Filler, new(10368.0f, -4608.0f, 0.0f)));
                    area.AddCell(new(39, Dockyard_Filler, new(1152.0f, 6912.0f, 0.0f)));
                    area.AddCell(new(40, Dockyard_Filler, new(10368.0f, -2304.0f, 0.0f)));
                    area.AddCell(new(41, Dockyard_Filler, new(1152.0f, 4608.0f, 0.0f)));
                    area.AddCell(new(42, Dockyard_Filler, new(10368.0f, 0.0f, 0.0f)));
                    area.AddCell(new(43, Dockyard_Filler, new(10368.0f, 2304.0f, 0.0f)));
                    area.AddCell(new(44, Dockyard_Filler, new(10368.0f, 4608.0f, 0.0f)));
                    area.AddCell(new(45, Dockyard_Filler, new(10368.0f, 6912.0f, 0.0f)));
                    area.AddCell(new(46, Dockyard_Filler, new(1152.0f, -4608.0f, 0.0f)));

                    area.AddCell(new(47, Shipping_Filler, new(-8064.0f, -6912.0f, 0.0f)));
                    area.AddCell(new(48, Shipping_Filler, new(-1152.0f, 4608.0f, 0.0f)));
                    area.AddCell(new(49, Shipping_Filler, new(-5760.0f, -6912.0f, 0.0f)));
                    area.AddCell(new(50, Shipping_Filler, new(-3456.0f, 4608.0f, 0.0f)));
                    area.AddCell(new(51, Shipping_Filler, new(-3456.0f, -6912.0f, 0.0f)));
                    area.AddCell(new(52, Shipping_Filler, new(-5760.0f, 4608.0f, 0.0f)));
                    area.AddCell(new(53, Shipping_Filler, new(-1152.0f, -6912.0f, 0.0f)));
                    area.AddCell(new(54, Shipping_Filler, new(-8064.0f, 4608.0f, 0.0f)));
                    area.AddCell(new(55, Shipping_Filler, new(1152.0f, -6912.0f, 0.0f)));
                    area.AddCell(new(56, Shipping_Filler, new(-10368.0f, 4608.0f, 0.0f)));
                    area.AddCell(new(57, Shipping_Filler, new(-10368.0f, 2304.0f, 0.0f)));
                    area.AddCell(new(58, Shipping_Filler, new(-10368.0f, 0.0f, 0.0f)));
                    area.AddCell(new(59, Shipping_Filler, new(-10368.0f, -2304.0f, 0.0f)));
                    area.AddCell(new(60, Shipping_Filler, new(-10368.0f, -4608.0f, 0.0f)));
                    area.AddCell(new(61, Shipping_Filler, new(-10368.0f, -6912.0f, 0.0f)));
                    region.AddArea(area);

                    region.EntrancePosition = new(1152.0f, 0.0f, 0.0f);
                    region.EntranceOrientation = new(0f, 0f, 0f);
                    region.WaypointPosition = new(1152.0f, 0.0f, 0.0f);
                    region.WaypointOrientation = new(1.57082f, 0f, 0f);

                    break;

                case RegionPrototype.UpperMadripoorRegionL60:

                    archiveData = new byte[] {
                    };

                    region = new(RegionPrototype.UpperMadripoorRegionL60,
                        1883928786,
                        archiveData, 
                        new(-1152.0f, -1152.0f, -1152.0f), 
                        new(21888.0f, 24192.0f, 1152.0f),
                        new(10, DifficultyTier.Normal));

                    area = new(1, (AreaPrototype) GameDatabase.GetPrototypeId("Regions/Story/CH10SecretInvasion/UpperMadripoor/UpperMadripoorAreaA.prototype"), new(), true);

                    district = GameDatabase.Resource.DistrictDict["Resource/Districts/MadripoorHightownDistrict.district"];
                    for (int i = 0; i < district.CellMarkerSet.Length; i++)
                        area.AddCell(new((uint)i + 1, GameDatabase.GetPrototypeId(district.CellMarkerSet[i].Resource), new()));
                    
                    region.AddArea(area);

                    region.EntrancePosition = new(20665.0f, 15910.0f, 0.0f);
                    region.EntranceOrientation = new(-1.914437f, 0.0f, 0.0f);
                    region.WaypointPosition = new(20665.0f, 15910.0f, 0.0f);
                    region.WaypointOrientation = new(1.914437f, 0.0f, 0.0f);

                    break;

                case RegionPrototype.UpperMadripoorRegionL60Cosmic:

                    archiveData = new byte[] {
                    };

                    region = new(RegionPrototype.UpperMadripoorRegionL60Cosmic,
                        1883928786,
                        archiveData,
                        new(-1152.0f, -1152.0f, -1152.0f),
                        new(21888.0f, 24192.0f, 1152.0f),
                        new(63, DifficultyTier.Superheroic));

                    area = new(1, (AreaPrototype)GameDatabase.GetPrototypeId("Regions/Story/CH10SecretInvasion/UpperMadripoor/UpperMadripoorAreaA.prototype"), new(), true);

                    district = GameDatabase.Resource.DistrictDict["Resource/Districts/MadripoorHightownDistrict.district"];
                    for (int i = 0; i < district.CellMarkerSet.Length; i++)
                        area.AddCell(new((uint)i + 1, GameDatabase.GetPrototypeId(district.CellMarkerSet[i].Resource), new()));

                    region.AddArea(area);

                    region.EntrancePosition = new(20665.0f, 15910.0f, 0.0f);
                    region.EntranceOrientation = new(-1.914437f, 0.0f, 0.0f);
                    region.WaypointPosition = new(20665.0f, 15910.0f, 0.0f);
                    region.WaypointOrientation = new(1.914437f, 0.0f, 0.0f);

                    break;

                case RegionPrototype.XManhattanRegion1to60:

                    archiveData = new byte[] {
                    };

                    region = new(RegionPrototype.XManhattanRegion1to60,
                        1883928786,
                        archiveData,
                        new(-1152f, -1152f, -1152f),
                        new(12672f, 12672f, 1152f),
                        new(10, DifficultyTier.Normal));


                    area = new(1, AreaPrototype.XManhattanArea1, new(), true);

                    district = GameDatabase.Resource.DistrictDict["Resource/Districts/MidtownStatic/MidtownStatic_A.district"];
                    for (int i = 0; i < district.CellMarkerSet.Length; i++)
                        area.AddCell(new((uint)i + 1, GameDatabase.GetPrototypeId(district.CellMarkerSet[i].Resource), new()));

                    region.AddArea(area);

                    region.EntrancePosition = new(12131.125f, 7102.125f, 48f);
                    region.EntranceOrientation = new();
                    region.WaypointPosition = new(11952f, 7040f, 48f);
                    region.WaypointOrientation = new(1.5625f, 0f, 0f);

                    break;

                case RegionPrototype.XManhattanRegion60Cosmic:

                    archiveData = new byte[] {
                        0xEF, 0x01, 0xCF, 0x8F, 0x01, 0x07, 0x00, 0x00, 0x00, 0xB6, 0x80, 0x01,
                        0x9A, 0xB3, 0xE6, 0x80, 0x04, 0x2C, 0x88, 0x18, 0xCA, 0x80, 0x03, 0x9A,
                        0xB3, 0xE6, 0x80, 0x04, 0x95, 0x80, 0x02, 0x1A, 0xCA, 0x40, 0x9A, 0xB3,
                        0xE6, 0x80, 0x04, 0xA8, 0x80, 0x02, 0x80, 0x80, 0x80, 0x88, 0x04, 0xA8,
                        0xC0, 0x02, 0xB8, 0xBD, 0x94, 0xF0, 0x03, 0x00, 0x16, 0xAE, 0xD6, 0xFD,
                        0xEF, 0xD6, 0x84, 0xE1, 0x9B, 0x83, 0x01, 0x08, 0x00, 0xE5, 0x91, 0x01,
                        0x00, 0x05, 0x00, 0x00, 0x06, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01,
                        0x01, 0x06, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x02, 0x02, 0x06, 0x00,
                        0x00, 0x01, 0x01, 0x00, 0x00, 0x03, 0x03, 0x06, 0x00, 0x00, 0x01, 0x01,
                        0x00, 0x00, 0x04, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0E,
                        0xE4, 0x82, 0x1E, 0xAC, 0xC0, 0x1F, 0xE1, 0xD9, 0x20, 0x87, 0xA2, 0x21,
                        0xE1, 0xB2, 0x21, 0x9A, 0xA0, 0x22, 0x9E, 0xF7, 0x22, 0xCC, 0xFE, 0x22,
                        0x9D, 0xB3, 0x23, 0x99, 0xA1, 0x24, 0xF1, 0xB4, 0x24, 0xDF, 0xCF, 0x24,
                        0xD9, 0xDA, 0x24, 0x8F, 0x99, 0x25, 0x05, 0x8E, 0xDA, 0xC2, 0x88, 0xFD,
                        0xE7, 0x87, 0xD1, 0x2D, 0x0A, 0xA0, 0x9F, 0x93, 0xD5, 0xF4, 0xAF, 0x49,
                        0xFF, 0xE3, 0x01, 0x96, 0xCE, 0x96, 0x91, 0x0F, 0x01, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xCB, 0x98, 0xD1, 0xBC, 0xF7,
                        0xB6, 0xE8, 0x8A, 0x22, 0x0A, 0xE0, 0xB1, 0xE9, 0xD9, 0xF4, 0xAF, 0x49,
                        0xD4, 0xF1, 0x02, 0xB8, 0xC2, 0xD1, 0xA7, 0x0D, 0x01, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF4, 0xA5, 0x8C, 0x85, 0xC3,
                        0xAA, 0xC9, 0xCE, 0xDF, 0x01, 0x06, 0x00, 0xFE, 0x91, 0x01, 0xEE, 0x95,
                        0x80, 0xA3, 0x0C, 0x02, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x01, 0x01, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x09, 0xE4,
                        0x82, 0x1E, 0xE1, 0xD9, 0x20, 0x9A, 0xA0, 0x22, 0x9E, 0xF7, 0x22, 0x99,
                        0xA1, 0x24, 0xF1, 0xB4, 0x24, 0xDF, 0xCF, 0x24, 0xD9, 0xDA, 0x24, 0x8F,
                        0x99, 0x25, 0xC1, 0xFD, 0xA4, 0xD3, 0x8E, 0x9C, 0xE1, 0xCA, 0x55, 0x08,
                        0x00, 0xFA, 0xE6, 0x01, 0x00, 0x02, 0x00, 0x00, 0x06, 0x00, 0x00, 0x05,
                        0x05, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x0E, 0xE4, 0x82, 0x1E, 0xAC, 0xC0, 0x1F, 0xE1, 0xD9, 0x20, 0x87, 0xA2,
                        0x21, 0xE1, 0xB2, 0x21, 0x9A, 0xA0, 0x22, 0x9E, 0xF7, 0x22, 0xCC, 0xFE,
                        0x22, 0x9D, 0xB3, 0x23, 0x99, 0xA1, 0x24, 0xF1, 0xB4, 0x24, 0xDF, 0xCF,
                        0x24, 0xD9, 0xDA, 0x24, 0x8F, 0x99, 0x25, 0x98, 0x8A, 0xBD, 0xC5, 0xE4,
                        0xE9, 0xB8, 0xA0, 0xEF, 0x01, 0x08, 0x00, 0xB0, 0x87, 0x03, 0x00, 0x05,
                        0x00, 0x00, 0x06, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x01, 0x06,
                        0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x02, 0x02, 0x06, 0x00, 0x00, 0x01,
                        0x01, 0x00, 0x00, 0x03, 0x03, 0x06, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00,
                        0x04, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0E, 0xE4, 0x82,
                        0x1E, 0xAC, 0xC0, 0x1F, 0xE1, 0xD9, 0x20, 0x87, 0xA2, 0x21, 0xE1, 0xB2,
                        0x21, 0x9A, 0xA0, 0x22, 0x9E, 0xF7, 0x22, 0xCC, 0xFE, 0x22, 0x9D, 0xB3,
                        0x23, 0x99, 0xA1, 0x24, 0xF1, 0xB4, 0x24, 0xDF, 0xCF, 0x24, 0xD9, 0xDA,
                        0x24, 0x8F, 0x99, 0x25, 0x05, 0xFB, 0x8B, 0x9C, 0x86, 0xB3, 0x90, 0xFD,
                        0xB7, 0x26, 0x06, 0x00, 0x8F, 0xCF, 0x04, 0xCE, 0x9F, 0xBD, 0x95, 0x0B,
                        0x03, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01,
                        0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x02, 0x04, 0x00, 0x00,
                        0x00, 0x14, 0x00, 0x00, 0x0E, 0xE4, 0x82, 0x1E, 0xAC, 0xC0, 0x1F, 0xE1,
                        0xD9, 0x20, 0x87, 0xA2, 0x21, 0xE1, 0xB2, 0x21, 0x9A, 0xA0, 0x22, 0x9E,
                        0xF7, 0x22, 0xCC, 0xFE, 0x22, 0x9D, 0xB3, 0x23, 0x99, 0xA1, 0x24, 0xF1,
                        0xB4, 0x24, 0xDF, 0xCF, 0x24, 0xD9, 0xDA, 0x24, 0x8F, 0x99, 0x25, 0xEB,
                        0xF0, 0xE8, 0x8A, 0xA1, 0x82, 0xBB, 0xFB, 0x5D, 0x0A, 0x00, 0x80, 0xB0,
                        0x01, 0xD6, 0xFA, 0xC8, 0x8F, 0x09, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x01, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00,
                        0x00, 0x02, 0x02, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x03, 0x03,
                        0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x04, 0x04, 0x00, 0x00, 0x00,
                        0x00, 0x01, 0x00, 0x00, 0x05, 0x05, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x0E, 0xE4, 0x82, 0x1E, 0xAC, 0xC0, 0x1F, 0xE1, 0xD9, 0x20, 0x87,
                        0xA2, 0x21, 0xE1, 0xB2, 0x21, 0x9A, 0xA0, 0x22, 0x9E, 0xF7, 0x22, 0xCC,
                        0xFE, 0x22, 0x9D, 0xB3, 0x23, 0x99, 0xA1, 0x24, 0xF1, 0xB4, 0x24, 0xDF,
                        0xCF, 0x24, 0xD9, 0xDA, 0x24, 0x8F, 0x99, 0x25, 0x96, 0x89, 0xD9, 0xD8,
                        0xFA, 0xFC, 0xB7, 0xE9, 0x43, 0x0A, 0xA0, 0x86, 0xAD, 0xDD, 0xF4, 0xAF,
                        0x49, 0xA1, 0xB2, 0x02, 0xFA, 0xEF, 0xE1, 0xE0, 0x08, 0x01, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xE9, 0x89, 0xC5, 0xA0,
                        0xF2, 0xC5, 0xE2, 0xFD, 0x4B, 0x0A, 0xA0, 0xD3, 0xD1, 0x80, 0xF5, 0xAF,
                        0x49, 0x9F, 0xC2, 0x02, 0xC4, 0xAE, 0xBE, 0xDA, 0x0E, 0x02, 0x00, 0x00,
                        0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x01, 0x99, 0xA1, 0x24, 0xC9, 0xA3, 0xE9, 0xA6,
                        0xC9, 0x87, 0xCD, 0x9F, 0x4F, 0x08, 0x00, 0xAA, 0xD9, 0x02, 0x00, 0x04,
                        0x00, 0x00, 0x06, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x01, 0x06,
                        0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x02, 0x02, 0x06, 0x00, 0x00, 0x01,
                        0x01, 0x00, 0x00, 0x03, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x0E, 0xE4, 0x82, 0x1E, 0xAC, 0xC0, 0x1F, 0xE1, 0xD9, 0x20, 0x87, 0xA2,
                        0x21, 0xE1, 0xB2, 0x21, 0x9A, 0xA0, 0x22, 0x9E, 0xF7, 0x22, 0xCC, 0xFE,
                        0x22, 0x9D, 0xB3, 0x23, 0x99, 0xA1, 0x24, 0xF1, 0xB4, 0x24, 0xDF, 0xCF,
                        0x24, 0xD9, 0xDA, 0x24, 0x8F, 0x99, 0x25, 0x05, 0x9A, 0xBB, 0xAF, 0xC8,
                        0x86, 0xEF, 0xD4, 0x92, 0x09, 0x06, 0x00, 0xD5, 0xF3, 0x04, 0xB6, 0x9B,
                        0xA3, 0xC3, 0x0E, 0x03, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x01, 0x01, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x02,
                        0x02, 0x00, 0x00, 0x03, 0x03, 0x00, 0x00, 0x00, 0x9D, 0xF0, 0xE0, 0xE4,
                        0xEE, 0xD6, 0x87, 0xF4, 0x11, 0x06, 0x00, 0x81, 0xCF, 0x02, 0xB2, 0xBC,
                        0xF7, 0x87, 0x03, 0x03, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x01, 0x01, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x02,
                        0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xA3, 0xB0, 0xE1, 0x97,
                        0x98, 0xB3, 0xB7, 0xB9, 0xA0, 0x01, 0x08, 0x00, 0xF4, 0x90, 0x03, 0x00,
                        0x05, 0x00, 0x00, 0x06, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x01,
                        0x06, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x02, 0x02, 0x06, 0x00, 0x00,
                        0x01, 0x01, 0x00, 0x00, 0x03, 0x03, 0x06, 0x00, 0x00, 0x01, 0x01, 0x00,
                        0x00, 0x04, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0E, 0xE4,
                        0x82, 0x1E, 0xAC, 0xC0, 0x1F, 0xE1, 0xD9, 0x20, 0x87, 0xA2, 0x21, 0xE1,
                        0xB2, 0x21, 0x9A, 0xA0, 0x22, 0x9E, 0xF7, 0x22, 0xCC, 0xFE, 0x22, 0x9D,
                        0xB3, 0x23, 0x99, 0xA1, 0x24, 0xF1, 0xB4, 0x24, 0xDF, 0xCF, 0x24, 0xD9,
                        0xDA, 0x24, 0x8F, 0x99, 0x25, 0xBA, 0xAE, 0xA3, 0xC0, 0xB8, 0xF7, 0x95,
                        0x80, 0xA6, 0x01, 0x06, 0x00, 0xF3, 0x97, 0x03, 0x8A, 0xAD, 0xC7, 0xF4,
                        0x01, 0x03, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
                        0x01, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x02, 0x02, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFE, 0xBC, 0x8D, 0xB4, 0xAF, 0xAD,
                        0xEF, 0xB3, 0x62, 0x06, 0x00, 0xCF, 0x9E, 0x03, 0xC0, 0xFD, 0xBE, 0x9D,
                        0x07, 0x03, 0x00, 0x00, 0x06, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01,
                        0x01, 0x04, 0x00, 0x00, 0x02, 0x04, 0x00, 0x00, 0x02, 0x02, 0x04, 0xC0,
                        0xF2, 0xEA, 0xCB, 0xF5, 0xAF, 0x49, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0E,
                        0xE4, 0x82, 0x1E, 0xAC, 0xC0, 0x1F, 0xE1, 0xD9, 0x20, 0x87, 0xA2, 0x21,
                        0xE1, 0xB2, 0x21, 0x9A, 0xA0, 0x22, 0x9E, 0xF7, 0x22, 0xCC, 0xFE, 0x22,
                        0x9D, 0xB3, 0x23, 0x99, 0xA1, 0x24, 0xF1, 0xB4, 0x24, 0xDF, 0xCF, 0x24,
                        0xD9, 0xDA, 0x24, 0x8F, 0x99, 0x25, 0x05, 0xDA, 0xC9, 0xFD, 0xFC, 0x8E,
                        0x9C, 0xEF, 0xBB, 0xD1, 0x01, 0x08, 0x00, 0xD0, 0xA6, 0x03, 0x00, 0x05,
                        0x00, 0x00, 0x06, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x01, 0x06,
                        0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x02, 0x02, 0x06, 0x00, 0x00, 0x01,
                        0x01, 0x00, 0x00, 0x03, 0x03, 0x06, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00,
                        0x04, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0E, 0xE4, 0x82,
                        0x1E, 0xAC, 0xC0, 0x1F, 0xE1, 0xD9, 0x20, 0x87, 0xA2, 0x21, 0xE1, 0xB2,
                        0x21, 0x9A, 0xA0, 0x22, 0x9E, 0xF7, 0x22, 0xCC, 0xFE, 0x22, 0x9D, 0xB3,
                        0x23, 0x99, 0xA1, 0x24, 0xF1, 0xB4, 0x24, 0xDF, 0xCF, 0x24, 0xD9, 0xDA,
                        0x24, 0x8F, 0x99, 0x25, 0x83, 0xE3, 0xB9, 0x8A, 0xD7, 0xDB, 0xE2, 0xED,
                        0xB2, 0x01, 0x08, 0x00, 0x86, 0xDD, 0x04, 0x00, 0x07, 0x00, 0x00, 0x06,
                        0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x01, 0x06, 0x00, 0x00, 0x01,
                        0x01, 0x00, 0x00, 0x02, 0x02, 0x06, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00,
                        0x03, 0x03, 0x06, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x04, 0x04, 0x06,
                        0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x05, 0x05, 0x06, 0x00, 0x00, 0x01,
                        0x01, 0x00, 0x00, 0x06, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x0E, 0xE4, 0x82, 0x1E, 0xAC, 0xC0, 0x1F, 0xE1, 0xD9, 0x20, 0x87, 0xA2,
                        0x21, 0xE1, 0xB2, 0x21, 0x9A, 0xA0, 0x22, 0x9E, 0xF7, 0x22, 0xCC, 0xFE,
                        0x22, 0x9D, 0xB3, 0x23, 0x99, 0xA1, 0x24, 0xF1, 0xB4, 0x24, 0xDF, 0xCF,
                        0x24, 0xD9, 0xDA, 0x24, 0x8F, 0x99, 0x25, 0xF8, 0xFD, 0xA4, 0xBD, 0xD2,
                        0xCA, 0xC2, 0xA2, 0x0D, 0x02, 0x00, 0x8B, 0xB5, 0x03, 0x00, 0x00, 0x0E,
                        0xE4, 0x82, 0x1E, 0xAC, 0xC0, 0x1F, 0xE1, 0xD9, 0x20, 0x87, 0xA2, 0x21,
                        0xE1, 0xB2, 0x21, 0x9A, 0xA0, 0x22, 0x9E, 0xF7, 0x22, 0xCC, 0xFE, 0x22,
                        0x9D, 0xB3, 0x23, 0x99, 0xA1, 0x24, 0xF1, 0xB4, 0x24, 0xDF, 0xCF, 0x24,
                        0xD9, 0xDA, 0x24, 0x8F, 0x99, 0x25, 0xA0, 0xD9, 0xEC, 0x92, 0xDA, 0x8C,
                        0xED, 0xE9, 0xC8, 0x01, 0x06, 0x00, 0xB3, 0xE0, 0x03, 0xEA, 0x8F, 0xB2,
                        0xC0, 0x02, 0x03, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x01, 0x01, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x02, 0x02,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xAD, 0xE6, 0x85, 0xFD, 0xEB,
                        0xC3, 0xBC, 0x9A, 0x26, 0x06, 0x00, 0xBA, 0xE9, 0x04, 0xF6, 0x98, 0x99,
                        0xBD, 0x06, 0x03, 0x00, 0x00, 0x0A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x01, 0x01, 0x0A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x02, 0x04,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x9E, 0xF7, 0x22, 0xD9, 0xDA,
                        0x24, 0x03, 0xF0, 0x96, 0x8D, 0xE1, 0xD2, 0xE4, 0xA6, 0xF0, 0xA3, 0x01,
                        0x08, 0x00, 0xD2, 0xBB, 0x05, 0x00, 0x04, 0x00, 0x00, 0x06, 0x00, 0x00,
                        0x01, 0x01, 0x00, 0x00, 0x01, 0x01, 0x06, 0x00, 0x00, 0x01, 0x01, 0x00,
                        0x00, 0x02, 0x02, 0x06, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x03, 0x03,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0E, 0xE4, 0x82, 0x1E, 0xAC,
                        0xC0, 0x1F, 0xE1, 0xD9, 0x20, 0x87, 0xA2, 0x21, 0xE1, 0xB2, 0x21, 0x9A,
                        0xA0, 0x22, 0x9E, 0xF7, 0x22, 0xCC, 0xFE, 0x22, 0x9D, 0xB3, 0x23, 0x99,
                        0xA1, 0x24, 0xF1, 0xB4, 0x24, 0xDF, 0xCF, 0x24, 0xD9, 0xDA, 0x24, 0x8F,
                        0x99, 0x25, 0x00, 0x02, 0xBF, 0x9B, 0x02, 0xCF, 0x9E, 0x03, 0x00, 0xBB,
                        0x8A, 0x8C, 0xC2, 0x92, 0xC7, 0xA2, 0xD2, 0x71, 0x00, 0xBD, 0xD7, 0x03,
                        0xCF, 0x9E, 0x03, 0x00, 0x02, 0x02, 0x00, 0xC8, 0x88, 0x99, 0x95, 0xB2,
                        0x09, 0x00, 0x00
                    };

                    region = new(RegionPrototype.XManhattanRegion60Cosmic,
                        1883928786,
                        archiveData,
                        new(-1152f, -1152f, -1152f),
                        new(12672f, 12672f, 1152f),
                        new(63, DifficultyTier.Superheroic));


                    area = new(1, AreaPrototype.XManhattanArea1, new(), true);

                    district = GameDatabase.Resource.DistrictDict["Resource/Districts/MidtownStatic/MidtownStatic_A.district"];
                    for (int i = 0; i < district.CellMarkerSet.Length; i++)
                        area.AddCell(new((uint)i + 1, GameDatabase.GetPrototypeId(district.CellMarkerSet[i].Resource), new()));

                    region.AddArea(area);

                    region.EntrancePosition = new(12131.125f, 7102.125f, 48f);
                    region.EntranceOrientation = new();
                    region.WaypointPosition = new(11952f, 7040f, 48f);
                    region.WaypointOrientation = new(1.5625f, 0f, 0f);

                    break;

            }

            return region;
        }

    }
}

