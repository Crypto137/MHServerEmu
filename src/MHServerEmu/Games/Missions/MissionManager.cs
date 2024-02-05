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
        public List<Mission> Missions { get; set; } = new();
        public List<LegendaryMissionBlacklist> LegendaryMissionBlacklists { get; set; } = new();


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
                Missions.Add(new(stream, boolDecoder));

            LegendaryMissionBlacklists.Clear();
            mlength = stream.ReadRawInt32();

            for (int i = 0; i < mlength; i++)
                LegendaryMissionBlacklists.Add(new(stream));
        }

        public MissionManager(PrototypeId prototypeId, List<Mission> missions, List<LegendaryMissionBlacklist> legendaryMissionBlacklists)
        {
            PrototypeId = prototypeId;
            Missions = missions;
            LegendaryMissionBlacklists = legendaryMissionBlacklists;
        }

        public void EncodeBools(BoolEncoder boolEncoder)
        {
            foreach (Mission mission in Missions)
                boolEncoder.EncodeBool(mission.Suspended);
        }

        public void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            stream.WritePrototypeEnum<Prototype>(PrototypeId);

            stream.WriteRawVarint64((ulong)Missions.Count);
            foreach (Mission mission in Missions) mission.Encode(stream, boolEncoder);

            stream.WriteRawInt32(LegendaryMissionBlacklists.Count);
            foreach (LegendaryMissionBlacklist blacklist in LegendaryMissionBlacklists) blacklist.Encode(stream);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"PrototypeId: {GameDatabase.GetPrototypeName(PrototypeId)}");
            for (int i = 0; i < Missions.Count; i++) sb.AppendLine($"Mission{i}: {Missions[i]}");
            for (int i = 0; i < LegendaryMissionBlacklists.Count; i++) sb.AppendLine($"LegendaryMissionBlacklist{i}: {LegendaryMissionBlacklists[i]}");
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

        internal void Shutdown(Region region)
        {
            throw new NotImplementedException();
        }
    }
}
