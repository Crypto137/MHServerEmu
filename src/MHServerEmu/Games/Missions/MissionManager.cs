using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions
{
    public class MissionManager
    {
        public PrototypeId PrototypeId { get; set; }
        public Dictionary<PrototypeId, Mission> Missions { get; set; } = new();
        public SortedDictionary<PrototypeGuid, LegendaryMissionBlacklist> LegendaryMissionBlacklists { get; set; } = new();


        public Player Player { get; private set; }       
        public Game Game { get; private set; }
        public IMissionManagerOwner Owner { get; set; }

        private ulong _regionId; 
        private HashSet<ulong> _missionInterestEntities = new();

        public MissionManager() { }

        public MissionManager(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            PrototypeId = stream.ReadPrototypeEnum<Prototype>();

            Missions.Clear();
            int mlength = (int)stream.ReadRawVarint64();

            for (int i = 0; i < mlength; i++)
            {
                PrototypeGuid missionGuid = (PrototypeGuid)stream.ReadRawVarint64();
                var missionRef = GameDatabase.GetDataRefByPrototypeGuid(missionGuid);
                // Mission mission = CreateMission(missionRef);
                // mission.Decode(stream, boolDecoder) TODO
                Mission mission = new(stream, boolDecoder);
                InsertMission(mission);
            }

            LegendaryMissionBlacklists.Clear();
            mlength = stream.ReadRawInt32();

            for (int i = 0; i < mlength; i++)
            {                
                PrototypeGuid category = (PrototypeGuid)stream.ReadRawVarint64();
                LegendaryMissionBlacklist legendaryMission = new(stream);
                LegendaryMissionBlacklists.Add(category, legendaryMission);
            }
        }

        public void EncodeBools(BoolEncoder boolEncoder)
        {
            foreach (var mission in Missions)
                boolEncoder.EncodeBool(mission.Value.Suspended);
        }

        public void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            stream.WritePrototypeEnum<Prototype>(PrototypeId);

            stream.WriteRawVarint64((ulong)Missions.Count);
            foreach (var pair in Missions)
            {
                PrototypeGuid missionGuid = GameDatabase.GetPrototypeGuid(pair.Key);
                stream.WriteRawVarint64((ulong)missionGuid);
                pair.Value.Encode(stream, boolEncoder);
            }

            stream.WriteRawInt32(LegendaryMissionBlacklists.Count);
            foreach (var pair in LegendaryMissionBlacklists)
            {
                PrototypeGuid category = pair.Key;
                stream.WriteRawVarint64((ulong)category); 
                pair.Value.Encode(stream);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"PrototypeId: {GameDatabase.GetPrototypeName(PrototypeId)}");
            foreach (var pair in Missions) sb.AppendLine($"Mission[{pair.Key}]: {pair.Value}");
            foreach (var pair in LegendaryMissionBlacklists) sb.AppendLine($"LegendaryMissionBlacklist[{pair.Key}]: {pair.Value}");
            return sb.ToString();
        }

        public MissionManager(Game game, IMissionManagerOwner owner)
        {
            Game = game;
            Owner = owner;
        }

        public bool InitializeForPlayer(Player player, Region region)
        {
            if (player == null) return false;

            Player = player;
            SetRegion(region);

            return true;
        }

        public bool IsPlayerMissionManager()
        {
            return (Owner != null) && Owner is Player;
        }

        public bool IsRegionMissionManager()
        {
            return (Owner != null) && Owner is Region;
        }

        public bool InitializeForRegion(Region region)
        {
            if (region == null)  return false;

            Player = null;
            SetRegion(region);

            return true;
        }

        private void SetRegion(Region region)
        {
            _regionId = region != null ? region.Id : 0;
        }

        public Region GetRegion()
        {
            if (_regionId == 0 || Game == null) return null;
            return RegionManager.GetRegion(Game, _regionId);
        }

        public Mission CreateMission(PrototypeId missionRef)
        {
            return new(this, missionRef);
        }

        public Mission InsertMission(Mission mission)
        {
            if (mission == null) return null;
            Missions.Add(mission.PrototypeId, mission); 
            return mission;
        }

        internal void Shutdown(Region region)
        {
            throw new NotImplementedException();
        }

        public bool GenerateMissionPopulation()
        {
            Region region = GetRegion();
            // search all Missions with encounter
            foreach (var missionRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy(typeof(MissionPrototype), PrototypeIterateFlags.NoAbstract | PrototypeIterateFlags.ApprovedOnly))
            {
                MissionPrototype missionProto = GameDatabase.GetPrototype<MissionPrototype>(missionRef);
                if (missionProto == null) continue;
                if (missionProto.HasPopulationInRegion(region) == false) continue;
                // TODO check mission
                if (InvalidMissions.Contains((InvalidMission)missionRef)) continue;
                // IsMissionValidAndApprovedForUse
                region.SpawnPopulation.MissionRegisty(missionProto);
            }

            return true;
        }

        public static readonly InvalidMission[] InvalidMissions = new InvalidMission[]
        {
            InvalidMission.CH00TrainingPathingController,
            InvalidMission.CH00NPETrainingRoom,
            InvalidMission.CivilWarDailyCapOM01DefeatSpiderman,
            InvalidMission.CivilWarDailyCapOM02DestroyCrates,
            InvalidMission.CivilWarDailyCapOM04SaveDumDum,
            InvalidMission.CivilWarDailyCapOM05HydraZoo,
            InvalidMission.CivilWarDailyCapOM06TeamUpDefeatSHIELD,
            InvalidMission.CivilWarDailyCapOM07InteractDefeatTurrets,
            InvalidMission.CivilWarDailyIronmanOM01DefeatSpiderman,
            InvalidMission.CivilWarDailyIronmanOM02DefeatThor,
            InvalidMission.CivilWarDailyIronmanOM03SaveJocasta,
            InvalidMission.CivilWarDailyIronmanOM04DestroyCrates,
            InvalidMission.CivilWarDailyIronmanOM05HydraZoo,
            InvalidMission.CivilWarDailyIronmanOM06TeamUpDefeatAIM,
            InvalidMission.CivilWarDailyIronmanOM07InteractDefeatHand,
        };

        public enum InvalidMission : ulong
        {
            CH00TrainingPathingController = 3126128604301631533,
            CH00NPETrainingRoom = 17508547083537161214,

            CivilWarDailyCapOM01DefeatSpiderman = 422011357013684087,
            CivilWarDailyCapOM02DestroyCrates = 16726105122650140376,
            CivilWarDailyCapOM04SaveDumDum = 1605098401643834761,
            CivilWarDailyCapOM05HydraZoo = 16108444317179587775,
            CivilWarDailyCapOM06TeamUpDefeatSHIELD = 16147585525915463870,
            CivilWarDailyCapOM07InteractDefeatTurrets = 11425191689973609005,

            CivilWarDailyIronmanOM01DefeatSpiderman = 10006467310735077687,
            CivilWarDailyIronmanOM02DefeatThor = 10800373542996422450,
            CivilWarDailyIronmanOM03SaveJocasta = 1692932771743412129,
            CivilWarDailyIronmanOM04DestroyCrates = 2469191070689800346,
            CivilWarDailyIronmanOM05HydraZoo = 14812369129072701055,
            CivilWarDailyIronmanOM06TeamUpDefeatAIM = 6784016171053232444,
            CivilWarDailyIronmanOM07InteractDefeatHand = 8062690480896488047,
        }
    }
}
