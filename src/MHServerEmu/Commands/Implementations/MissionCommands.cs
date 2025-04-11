using MHServerEmu.Commands.Attributes;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Regions;
using MHServerEmu.Grouping;
using static MHServerEmu.Commands.Implementations.DebugCommands;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("mission", "commands about missions", AccountUserLevel.User)]
    public class MissionCommands : CommandGroup
    {
        [Command("debug", "Usage: mission debug [on|off].", AccountUserLevel.Admin)]
        public string Debug(string[] @params, FrontendClient client)
        {
            if ((@params.Length > 0 && Enum.TryParse(@params[0], true, out Switch flags)) == false)
                flags = Switch.Off;   // Default Off

            MissionManager.Debug = (flags == Switch.On) ? true : false;

            return $"Mission Log [{flags}]";
        }

        [Command("resetstory", "Reset all main story missions.\nUsage: mission resetstory.")]
        public string ResetStory(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);

            var avatar = playerConnection.Player.CurrentAvatar;
            if (avatar == null) return "Current Avatar not found.";

            avatar.ResetMissions();

            return "Story missions reset";
        }

        [Command("completestory", "Set all main story missions to completed.\nUsage: mission completestory", AccountUserLevel.Admin)]
        public string CompleteStory(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);

            var manager = playerConnection.Player?.MissionManager;
            if (manager == null) return "Mission manager is null.";

            foreach (PrototypeId missionRef in Enum.GetValues(typeof(StoryMissionsPrototypeId)))
            {
                var mission = manager.MissionByDataRef(missionRef);
                if (mission != null && mission.State != MissionState.Completed)
                    mission.SetState(MissionState.Completed, true);
            }

            return "Story missions set to completed";
        }

        [Command("region", "List all the mission prototypes in the current region.\nUsage: mission region")]
        public string Region(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            if (playerConnection == null) return "PlayerConnection not found";

            Region region = playerConnection.Player.GetRegion();
            if (region == null) return "No region found.";

            ChatHelper.SendMetagameMessage(client, $"Missions in {region.PrototypeName} :", true);
            ChatHelper.SendMetagameMessageSplit(client, string.Join("\r\n", region.MissionManager.ActiveMissions.Select(GameDatabase.GetFormattedPrototypeName)), false);
            return string.Empty;
        }

        [Command("info", "Display information about the given mission.\nUsage: mission info [pattern].")]
        public string Info(string[] @params, FrontendClient client)
        {
            if (client == null)
                return "You can only invoke this command from the game.";

            if (@params.Length == 0)
                return "Invalid arguments. Type 'help mission info' to get help.";

            string errorMessage = GetMissionFromPattern(client, @params[0], out List<Mission> missionsFound);
            if (errorMessage != null) return errorMessage;

            if (missionsFound.Count == 1)
            {
                var text = missionsFound[0].ToString();
                ChatHelper.SendMetagameMessage(client, $"Mission info:", true);
                ChatHelper.SendMetagameMessageSplit(client, string.Join("\r\n", text), false);
                return string.Empty;
            }

            ChatHelper.SendMetagameMessage(client, $"Multiple matches found :", true);
            ChatHelper.SendMetagameMessageSplit(client, string.Join("\r\n", missionsFound.Select(k => k.PrototypeName)), false);

            return string.Empty;
        }

        [Command("complete", "Complete the given mission.\nUsage: mission complete [pattern].", AccountUserLevel.Admin)]
        public string Complete(string[] @params, FrontendClient client)
        {
            if (client == null)
                return "You can only invoke this command from the game.";

            if (@params.Length == 0)
                return "Invalid arguments. Type 'help mission complete' to get help.";

            string errorMessage = GetMissionFromPattern(client, @params[0], out List<Mission> missionsFound);
            if (errorMessage != null) return errorMessage;

            if (missionsFound.Count == 1)
            {
                var mission = missionsFound[0];
                if (mission != null && mission.State != MissionState.Completed)
                {
                    mission.SetState(MissionState.Completed, true);
                    return $"{mission.PrototypeName} completed";
                }
                else
                {
                    return $"{mission.PrototypeName} already completed";
                }
            }

            ChatHelper.SendMetagameMessage(client, $"Multiple matches found :", true);
            ChatHelper.SendMetagameMessageSplit(client, string.Join("\r\n", missionsFound.Select(k => k.PrototypeName)), false);

            return string.Empty;
        }

        [Command("reset", "Restart the given mission.\nUsage: mission reset [pattern].", AccountUserLevel.Admin)]
        public string Reset(string[] @params, FrontendClient client)
        {
            if (client == null)
                return "You can only invoke this command from the game.";

            if (@params.Length == 0)
                return "Invalid arguments. Type 'help mission reset' to get help.";

            string errorMessage = GetMissionFromPattern(client, @params[0], out List<Mission> missionsFound);
            if (errorMessage != null) return errorMessage;

            if (missionsFound.Count == 1)
            {
                missionsFound[0].RestartMission();
                return $"{missionsFound[0].PrototypeName} restarted";
            }

            ChatHelper.SendMetagameMessage(client, $"Multiple matches found :", true);
            ChatHelper.SendMetagameMessageSplit(client, string.Join("\r\n", missionsFound.Select(k => k.PrototypeName)), false);

            return string.Empty;
        }

        private string GetMissionFromPattern(FrontendClient client, string pattern, out List<Mission> missionsFound)
        {
            missionsFound = new();

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection, out Game game);
            if (game == null) return "Game not found";
            if (playerConnection == null) return "PlayerConnection not found";

            missionsFound.AddRange(playerConnection.Player?.MissionManager?.FindMissionsByPattern(pattern));

            Region region = playerConnection.Player.GetRegion();
            if (region != null)
                missionsFound.AddRange(region.MissionManager?.FindMissionsByPattern(pattern));

            if (missionsFound.Count == 0) return $"No mission found";

            return null;
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
    }
}
