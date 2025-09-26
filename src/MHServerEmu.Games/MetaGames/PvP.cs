using System.Text;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.MetaGames
{
    public class PvP : MetaGame
    {
        private RepVar_int _team1 = new();
        private RepVar_int _team2 = new();

        public PvPPrototype PvPPrototype { get => Prototype as PvPPrototype; }
        public ScoreTable PvPScore { get; private set; }
        public PvP(Game game) : base(game) { }

        public override bool Initialize(EntitySettings settings)
        {
            if (base.Initialize(settings) == false) return false;

            var pvpProto = PvPPrototype;
            CreateTeams(pvpProto.Teams);

            if (pvpProto.ScoreSchemaRegion != PrototypeId.Invalid || pvpProto.ScoreSchemaPlayer != PrototypeId.Invalid)
            {
                PvPScore = new(this);
                PvPScore.Initialize(pvpProto.ScoreSchemaRegion, pvpProto.ScoreSchemaPlayer);
            }

            CreateGameModes(pvpProto.GameModes);

            return true;
        }

        public override bool Serialize(Archive archive)
        {
            bool success = base.Serialize(archive);
            // if (archive.IsTransient)
            success &= Serializer.Transfer(archive, ref _team1);
            success &= Serializer.Transfer(archive, ref _team2);
            return success;
        }

        protected override void BindReplicatedFields()
        {
            base.BindReplicatedFields();

            _team1.Bind(this, AOINetworkPolicyValues.AOIChannelProximity);
            _team2.Bind(this, AOINetworkPolicyValues.AOIChannelProximity);
        }

        protected override void UnbindReplicatedFields()
        {
            base.UnbindReplicatedFields();

            _team1.Unbind();
            _team2.Unbind();
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            sb.AppendLine($"{nameof(_team1)}: {_team1}");
            sb.AppendLine($"{nameof(_team2)}: {_team2}");
        }

        public override void OnPostInit(EntitySettings settings)
        {
            base.OnPostInit(settings);
            if (GameModes.Count > 0) ActivateGameMode(0);
        }

        public override bool AddPlayer(Player player)
        {
            if (base.AddPlayer(player) == false) return false;

            var mode = CurrentMode;
            if (mode == null) return false;
            mode.OnAddPlayer(player);

            if (Debug) Logger.Warn($"AddPlayer {player.Id} {mode.PrototypeDataRef.GetNameFormatted()}");

            foreach (var state in MetaStates)
                state.OnAddPlayer(player);

            // TODO MiniMap update

            player.Properties[PropertyEnum.PvPMode] = mode.PrototypeDataRef;

            return true;
        }

        public override bool RemovePlayer(Player player)
        {
            if (base.RemovePlayer(player) == false) return false;

            var mode = CurrentMode;
            if (mode == null) return false;
            mode.OnRemovePlayer(player);
            player.Properties[PropertyEnum.PvPMode] = PrototypeId.Invalid;

            return true;
        }
    }
}
