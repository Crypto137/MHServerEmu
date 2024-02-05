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
    }
}
