using MHServerEmu.Commands.Attributes;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("unlock", "Provides commands for unlock.", AccountUserLevel.Admin)]
    public class Unlock : CommandGroup
    {
        [Command("waypoints", "Unlock all waypoints.\nUsage: unlock waypoints")]
        public string Waypoints(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            var player = playerConnection.Player;

            foreach (PrototypeId waypointRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<WaypointPrototype>(PrototypeIterateFlags.NoAbstract))
                player.UnlockWaypoint(waypointRef);

            return "Waypoints unlocked";
        }

        public enum StoryMissionsPrototypeId : ulong
        {
            CH00RaftTutorial = 3988755028944361303,
            CH00NPEEternitySplinter = 15270503549571702218,
            CH00NPETrainingRoom = 17508547083537161214,

            CH01Main1CleaningUpTheKitchen = 12039607666173157509,
            CH01Main2VenomsVengeance = 2180395688807505533, 
            CH01Main3ShockerUnchained = 12387850922441449176,
            CH01Main4VillainousPursuit = 11885196984710995869, 
            CH01Main5TabletOfLifeAndTime = 2018163037961003023,

            CH02M1PursuingtheHood = 9023005140006673904,
            CH02M2AIMLab = 6152302201091463663,
            CH02M3CleaningtheYard = 6864579066306502076,
            CH02M4AnImportantTask = 17450495646416838123,
            CH02M5SomethingFishy = 11958716054822329725,
            CH02M6BargeIn = 16551603174138714725,

            CH03M1MeetInMadripoor = 2411365884406996002,
            CH03M2TheLostPatrol = 16450625401512008553,
            CH03M3SnakesintheGrass = 2379423066362748050,
            CH03M4EyesofSHIELD = 14553657398579174106,
            CH03M5TheMuramasaBlade = 13760316042511653991,
            CH03M6TheTabletChase = 3645611232768629657,

            CH04M1CorruptionInBlue = 488113815984479278,
            CH04M2PoisonInTheStreets = 7132716867153894677,
            CH04M3ClutchesOfTheKingPin = 689101055940042173,
            CH04M4ToppleTheKingpin = 4577923555173997602,
            CH04M5TroubleatXaviers = 5881376551054089266,

            CH05M1PurificationCrusade = 13548972618267237983,
            CH05M2MorlockUnderground = 11480444822467255824,
            CH05M3ChurchOfPurification = 18246490693870165708,

            CH06M1DangerousTechInFortStryker = 7510168730973381101,
            CH06M2CleansingWrath = 7381688785141701814,
            CH06M3StrykerUnderSiege = 12384591629388815900,

            CH07M1JetToTheJungle = 5782860060075695120,
            CH07M2InfestationMostVile = 13095692361079332412,
            CH07M3MutateGenesis = 6064609115356273572,
            CH07M4ASinisterPlan = 15811526009050176420,
            CH07M5AVisitWithSHIELD = 12833355164438633709,

            CH08M1StarktechAndTheIntelligencia = 4572735373997777149,
            CH08M2SmashHYDRA = 7765219607455406493,
            CH08M3ADoomedWorld = 6934907781807217259,
            CH08M4VictoryLap = 16876035236001814968,
            CH08Side3DoomsLethalLegion = 17642491112597102012,

            CH09M1AFrostyReception = 7881545985209211733,
            CH09M2BattleForAsgard = 17539112805425617594,
            CH09M3CityUnderSiege = 13914513569467865706,
            CH09M4ThroneOfDeceit = 4917040259561101914,

            CH10Main1TheresNoPlaceLikeHome = 15892710982401794316,
            CH10Main2MysteriesInMadripoor = 12674455325186466011,
            CH10Main3TowerOfIntrigue = 9031201734025551552,
            CH10Main4AGatherIntelligence = 15243774829094576163,
            CH10Main4BSkrullsAtSchool = 1418466953644678947,
            CH10Main4CSkrullsInStarkTower = 9232874567518200052,
            CH10Main4DSkrullsOnTheStreets = 14077014987781382384,
            CH10Main5CulminationOfWar = 14703490378628669207,
            CH10Main6HeroesTriumph = 61027896657714661,
        }

        [Command("missions", "Unlock main story missions.\nUsage: unlock missions")]
        public string Missions(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);

            var manager = playerConnection.Player?.MissionManager;
            if (manager ==  null) return "Mission manager is null.";

            foreach (PrototypeId missionRef in Enum.GetValues(typeof(StoryMissionsPrototypeId)))
            {
                var mission = manager.MissionByDataRef(missionRef);
                if (mission != null && mission.State != MissionState.Completed)
                    mission.SetState(MissionState.Completed, true);
            }

            return "Story missions unlocked";
        }

        [Command("chapters", "Unlock all chapters.\nUsage: unlock chapters")]
        public string Chapters(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            var player = playerConnection.Player;

            foreach (PrototypeId chapterRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<ChapterPrototype>(PrototypeIterateFlags.NoAbstract))
                player.UnlockChapter(chapterRef);

            return "Chapters unlocked";
        }
    }
}

