using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames
{
    public class PvPTeam(MetaGame metaGame, PvPTeamPrototype proto) : MetaGameTeam(metaGame, proto.DataRef, proto.MaxPlayers)
    {
        public PvPTeamPrototype Prototype { get; private set; } = proto;
        public PrototypeId StartTarget { get => Prototype.StartTarget; }
        public AlliancePrototype Alliance { get; private set; } = proto.Alliance.As<AlliancePrototype>();

        public override bool AddPlayer(Player player)
        {
            if (base.AddPlayer(player) == false) return false;

            if (Alliance != null) player.SetAllianceOverride(Alliance);
            return true;
        }

        public override bool RemovePlayer(Player player)
        {
            if (base.RemovePlayer(player) == false) return false;

            if (Alliance != null) player.SetAllianceOverride(null);
            return true;
        }

        public Player GetTeammateByPlayer(Player player)
        {
            foreach (var teammate in _players)
                if (teammate != player) return teammate;
            return null;
        }
    }
}
