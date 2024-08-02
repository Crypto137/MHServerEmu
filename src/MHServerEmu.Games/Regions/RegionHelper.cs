using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.UI.Widgets;

namespace MHServerEmu.Games.Regions
{
    public static class RegionHelper
    {
        private static readonly RegionPrototypeId[] PatrolRegions = new RegionPrototypeId[]
        {
            RegionPrototypeId.XManhattanRegion1to60,
            RegionPrototypeId.XManhattanRegion60Cosmic,
            RegionPrototypeId.BrooklynPatrolRegionL60,
            RegionPrototypeId.BrooklynPatrolRegionL60Cosmic,
            RegionPrototypeId.UpperMadripoorRegionL60,
            RegionPrototypeId.UpperMadripoorRegionL60Cosmic,
        };

        public static bool TEMP_IsPatrolRegion(PrototypeId regionProtoRef)
        {
            return PatrolRegions.Contains((RegionPrototypeId)regionProtoRef);
        }

        public static void TEMP_InitializeHardcodedRegionData(Region region)
        {
            switch ((RegionPrototypeId)region.PrototypeDataRef)
            {
                // dumped
                case RegionPrototypeId.NPEAvengersTowerHUBRegion:
                    region.Properties[PropertyEnum.BonusItemFindBonusDifficultyMult] = 6;

                    // Missions/Prototypes/BonusMissions/WeeklyEventMissions/CosmicWeekMissionController.prototype
                    region.MissionManager.InsertMission(new((PrototypeId)9033745879814514175, 0x64A2F98F));

                    // Missions/Prototypes/Hubs/AvengersTower/Discoveries/BenUrichAndJessicaJones.prototype
                    region.MissionManager.InsertMission(new((PrototypeId)12163941636897120859, 0x7094021E));

                    // Missions/Prototypes/BonusMissions/WeeklyEventMissions/OmegaMissionVendorController.prototype
                    region.MissionManager.InsertMission(new((PrototypeId)15923097160974411372, 0x5535A91E));

                    // Missions/Prototypes/BonusMissions/WeeklyEventMissions/ARMORWeekMissionController.prototype
                    region.MissionManager.InsertMission(new((PrototypeId)16864913917856392610, 0x7297046B));

                    break;

                case RegionPrototypeId.XaviersMansionRegion:
                    /*
                    area.CellList[17].AddEncounter(15374827165380448803, 4, true);
                    area.CellList[15].AddEncounter(8642336607468261979, 7, true);
                    area.CellList[23].AddEncounter(4065272706848002543, 3, true);
                    area.CellList[10].AddEncounter(12198525011368022752, 1, true);
                    */
                    break;

                // dumped
                case RegionPrototypeId.DangerRoomHubRegion:
                    region.Properties[PropertyEnum.BonusItemFindBonusDifficultyMult] = 9;
                    region.Properties[PropertyEnum.DamageRegionMobToPlayer] = 4f;
                    region.Properties[PropertyEnum.DamageRegionPlayerToMob] = 0.3f;
                    region.Properties[PropertyEnum.ExperienceBonusPct] = 0.9f;
                    region.Properties[PropertyEnum.LootBonusCreditsPct] = 0.9f;
                    region.Properties[PropertyEnum.LootBonusXPPct] = 0.9f;

                    break;

                // dumped
                case RegionPrototypeId.XManhattanRegion60Cosmic:
                    region.Properties[PropertyEnum.BonusItemFindBonusDifficultyMult] = 13;
                    region.Properties[PropertyEnum.DamageRegionMobToPlayer] = 8f;
                    region.Properties[PropertyEnum.DamageRegionPlayerToMob] = 0.13f;
                    region.Properties[PropertyEnum.ExperienceBonusPct] = 2.4f;
                    region.Properties[PropertyEnum.LootBonusCreditsPct] = 2.4f;
                    region.Properties[PropertyEnum.LootBonusXPPct] = 2.4f;

                    #region Missions

                    region.MissionManager.InsertMission(new(MissionState.Completed,
                        TimeSpan.Zero,
                        (PrototypeId)3667304362440335589,   // Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventNinjas.prototype
                        0x0,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x3, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x4, MissionObjectiveState.Invalid, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 491876, 516140, 535777, 545031, 547169, 561178, 572318, 573260, 579997, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    region.MissionManager.InsertMission(new(MissionState.Failed,
                        new(1613518100500000),
                        (PrototypeId)5751045088227960741,   // Missions/Prototypes/PVEEndgame/PatrolMidtown/Discoveries/Midtown/MidtownPlayingBaseball.prototype
                        0x7912D38B,
                        new MissionObjective[] { new(0x0, 0x0, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        Array.Empty<ulong>(),
                        false));

                    region.MissionManager.InsertMission(new(MissionState.Failed,
                        new(1613518149500000),
                        (PrototypeId)9358696252006540917,   // Missions/Prototypes/PVEEndgame/PatrolMidtown/Discoveries/Midtown/MidtownCalisthenics.prototype
                        0x6A7A309C,
                        new MissionObjective[] { new(0x0, 0x0, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        Array.Empty<ulong>(),
                        false));

                    region.MissionManager.InsertMission(new(MissionState.Active,
                        TimeSpan.Zero,
                        (PrototypeId)3672161157557464473,   // Missions/Prototypes/PVEEndgame/StaticScenarios/ControllerChecks/CivilWarDeathCheckOpenWorldCrossbones.prototype
                        0x62300577,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Active, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Available, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 491876, 535777, 561178, 572318, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    region.MissionManager.InsertMission(new(MissionState.Completed,
                        TimeSpan.Zero,
                        (PrototypeId)5826455758384603593,   // Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventDoombots.prototype
                        0x0,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x5, 0x5, 0x0, 0x0),
                            new(0x1, 0x0, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 491876, 516140, 535777, 545031, 547169, 561178, 572318, 573260, 579997, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    region.MissionManager.InsertMission(new(MissionState.Completed,
                        TimeSpan.Zero,
                        (PrototypeId)9898965535468037097,   // Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventFrightfulFour.prototype
                        0x0,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x3, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x4, MissionObjectiveState.Invalid, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 491876, 516140, 535777, 545031, 547169, 561178, 572318, 573260, 579997, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    region.MissionManager.InsertMission(new(MissionState.Active,
                        TimeSpan.Zero,
                        (PrototypeId)15008190206338409193,  // Missions/Prototypes/Achievements/Challenges/AchievementPunisherMidtownTimed.prototype
                        0x5957A7E7,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Active, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Active, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x14, 0x0, 0x0) },
                        new ulong[] { 491876, 516140, 535777, 545031, 547169, 561178, 572318, 573260, 579997, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    region.MissionManager.InsertMission(new(MissionState.Failed,
                        TimeSpan.Zero,
                        (PrototypeId)4425023419088315138,   // Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventBrotherhood.prototype
                        0x48F91EAB,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Invalid, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x1, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Invalid, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x1, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Invalid, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x1, 0x0, 0x0),
                            new(0x3, MissionObjectiveState.Invalid, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x1, 0x0, 0x0),
                            new(0x4, MissionObjectiveState.Invalid, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x1, 0x0, 0x0),
                            new(0x5, MissionObjectiveState.Failed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 491876, 516140, 535777, 545031, 547169, 561178, 572318, 573260, 579997, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    region.MissionManager.InsertMission(new(MissionState.Failed,
                        new(1613518186500000),
                        (PrototypeId)7753181439701952032,   // Missions/Prototypes/PVEEndgame/PatrolMidtown/Discoveries/Midtown/MidtownTrafficStop.prototype
                        0x460C3BFD,
                        new MissionObjective[] { new(0x0, 0x0, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        Array.Empty<ulong>(),
                        false));

                    region.MissionManager.InsertMission(new(MissionState.Failed,
                        new(1613518556500000),
                        (PrototypeId)8164724054894847468,   // Missions/Prototypes/PVEEndgame/PatrolMidtown/Discoveries/Midtown/MidtownCarAccident.prototype
                        0x75A7CBA2,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x1, 0x0, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 594073, },
                        false));

                    region.MissionManager.InsertMission(new(MissionState.Completed,
                        TimeSpan.Zero,
                        (PrototypeId)8733075101353582604,   // Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventHood.prototype
                        0x0,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x3, MissionObjectiveState.Invalid, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 491876, 516140, 535777, 545031, 547169, 561178, 572318, 573260, 579997, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    region.MissionManager.InsertMission(new(MissionState.Active,
                        TimeSpan.Zero,
                        (PrototypeId)15924448993393518092,  // Missions/Prototypes/PVEEndgame/PatrolMidtown/Discoveries/Midtown/MidtownBankRobbery.prototype
                        0x743466DB,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Active, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Available, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Available, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x3, 0x3, 0x0, 0x0) },
                        Array.Empty<ulong>(),
                        false));

                    region.MissionManager.InsertMission(new(MissionState.Active,
                        TimeSpan.Zero,
                        (PrototypeId)8492342449826966212,   // Missions/Prototypes/PVEEndgame/PatrolMidtown/Discoveries/Midtown/MidtownArsonistsAgainstPolice.prototype
                        0x187EEF19,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Active, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Available, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Available, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        Array.Empty<ulong>(),
                        false));

                    region.MissionManager.InsertMission(new(MissionState.Completed,
                        TimeSpan.Zero,
                        (PrototypeId)10149215495678468529,  // Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventMalekith.prototype
                        0x0,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x3, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x4, MissionObjectiveState.Invalid, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 491876, 516140, 535777, 545031, 547169, 561178, 572318, 573260, 579997, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    region.MissionManager.InsertMission(new(MissionState.Active,
                        TimeSpan.Zero,
                        (PrototypeId)10315340690132511222,  // Missions/Prototypes/PVEEndgame/PatrolMidtown/Discoveries/Midtown/MidtownGroceryThugsStandoff.prototype
                        0xF48EB45,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Active, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Available, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Available, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        Array.Empty<ulong>(),
                        false));

                    region.MissionManager.InsertMission(new(MissionState.Active,
                        TimeSpan.Zero,
                        (PrototypeId)10490887443555427166,  // Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventMegaSentinel.prototype
                        0x39D7DF60,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Active, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x2, 0x4, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Active, new(1613519345000000), Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 491876, 516140, 535777, 545031, 547169, 561178, 572318, 573260, 579997, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    region.MissionManager.InsertMission(new(MissionState.Completed,
                        TimeSpan.Zero,
                        (PrototypeId)10686139501369173095,  // Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventMisterSinister.prototype
                        0x0,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x3, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x4, MissionObjectiveState.Invalid, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 491876, 516140, 535777, 545031, 547169, 561178, 572318, 573260, 579997, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    region.MissionManager.InsertMission(new(MissionState.Completed,
                        TimeSpan.Zero,
                        (PrototypeId)15354703907328566055,  // Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventSinisterSix.prototype
                        0x0,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x3, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x4, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x5, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x6, MissionObjectiveState.Invalid, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 491876, 516140, 535777, 545031, 547169, 561178, 572318, 573260, 579997, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    region.MissionManager.InsertMission(new(MissionState.Inactive,
                        TimeSpan.Zero,
                        (PrototypeId)11060757254749691901,  // Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventBase.prototype
                        0x0,
                        Array.Empty<MissionObjective>(),
                        new ulong[] { 491876, 516140, 535777, 545031, 547169, 561178, 572318, 573260, 579997, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    region.MissionManager.InsertMission(new(MissionState.Active,
                        TimeSpan.Zero,
                        (PrototypeId)12166721969729972671,  // Missions/Prototypes/PVEEndgame/PatrolMidtown/Discoveries/Midtown/MidtownMaggiaShakedownStand.prototype
                        0x140643F5,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Active, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Available, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Available, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        Array.Empty<ulong>(),
                        false));

                    region.MissionManager.InsertMission(new(MissionState.Active,
                        TimeSpan.Zero,
                        (PrototypeId)15669697143252068358,  // Missions/Prototypes/PVEEndgame/PatrolMidtown/Discoveries/Midtown/MidtownMaggiaKidnapping.prototype
                        0x33D3263B,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Skipped, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Skipped, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Active, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 572318, 601433, },
                        false));

                    region.MissionManager.InsertMission(new(MissionState.Completed,
                        TimeSpan.Zero,
                        (PrototypeId)17742095572202693358,  // Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventGreenGoblin.prototype
                        0x0,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x3, MissionObjectiveState.Invalid, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 491876, 516140, 535777, 545031, 547169, 561178, 572318, 573260, 579997, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    #endregion

                    // Widget: UI/MetaGame/MissionName.prototype
                    // Context: Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventMegaSentinel.prototype
                    var missionTextWidget = region.UIDataProvider.GetWidget<UIWidgetMissionText>((PrototypeId)7164846210465729875, (PrototypeId)10490887443555427166);
                    missionTextWidget.SetText((LocaleStringId)8188822000559654203, LocaleStringId.Invalid);

                    // Widget: UI/MetaGame/TimeRemainingStoryMode2.prototype
                    // Context: Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventMegaSentinel.prototype
                    var genericFractionWidget = region.UIDataProvider.GetWidget<UIWidgetGenericFraction>((PrototypeId)11932510257277768241, (PrototypeId)10490887443555427166);
                    genericFractionWidget.SetCount(1, 1);
                    genericFractionWidget.SetTimeRemaining(251550);

                    break;

                // custom
                case RegionPrototypeId.SurturRaidRegionGreen:
                    var deathsWidget = region.UIDataProvider.GetWidget<UIWidgetGenericFraction>((PrototypeId)11858833950498362027, PrototypeId.Invalid);
                    deathsWidget.SetCount(30, 30);
                    region.UIDataProvider.GetWidget<UIWidgetEntityIconsSyncData>((PrototypeId)1133155547537679647, PrototypeId.Invalid);
                    region.UIDataProvider.GetWidget<UIWidgetEntityIconsSyncData>((PrototypeId)478583290767352422, PrototypeId.Invalid);

                    break;
            }
        }

        public static void DumpRegionToJson(Region region)
        {
            RegionDump regionDump = new();

            foreach (var areaKvp in region.Areas)
            {
                AreaDump areaDump = new(areaKvp.Value.PrototypeDataRef, areaKvp.Value.Origin);
                regionDump.Add(areaKvp.Key, areaDump);

                foreach (var cellKvp in areaKvp.Value.Cells)
                    areaDump.Cells.Add(cellKvp.Key, new(cellKvp.Value.Prototype.ToString(), cellKvp.Value.AreaPosition));
            }

            FileHelper.SerializeJson(Path.Combine(FileHelper.ServerRoot, "RegionDumps", $"{region.PrototypeName}_{region.RandomSeed}.json"),
                regionDump, new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });
        }

        private class RegionDump : Dictionary<uint, AreaDump> { }

        private class AreaDump
        {
            public ulong PrototypeDataRef { get; set; }
            public Vector3 Origin { get; set; }
            public SortedDictionary<uint, CellDump> Cells { get; set; } = new();

            public AreaDump(PrototypeId areaProtoRef, Vector3 origin)
            {
                PrototypeDataRef = (ulong)areaProtoRef;
                Origin = origin;
            }
        }

        private class CellDump
        {
            public string PrototypeName { get; set; }
            public Vector3 Position { get; set; }

            public CellDump(string prototypeName, Vector3 position)
            {
                PrototypeName = prototypeName;
                Position = position;
            }
        }
    }
}
