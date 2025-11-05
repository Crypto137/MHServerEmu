using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;
using System.Text;

namespace MHServerEmu.Games.MetaGames
{
    public class PvP : MetaGame
    {

        private Dictionary<ulong, PropertyCollection> _playersCollection = [];

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

            // foreach (var player in new PlayerIterator(Region))
            //    player.RevealDiscoveryMap();

            return true;
        }

        public override MetaGameTeam CreateTeam(PrototypeId teamRef)
        {
            var teamProto = GameDatabase.GetPrototype<PvPTeamPrototype>(teamRef);
            if (teamProto == null) return null;
            return new PvPTeam(this, teamProto);
        }

        public PvPTeam GetOtherTeamByRef(PrototypeId teamRef)
        {
            foreach (var team in Teams)
                if (team.ProtoRef != teamRef) 
                    return team as PvPTeam;

            return null;
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

            UpdatePlayerCollection(player);

            if (PvPPrototype.EvalOnPlayerAdded != null)
            {
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.SetVar_PropertyCollectionPtr(EvalContext.Default, player.Properties);
                evalContext.SetVar_PropertyCollectionPtr(EvalContext.Entity, player.CurrentAvatar.Properties);
                Eval.RunBool(PvPPrototype.EvalOnPlayerAdded, evalContext);
            }

            if (PvPPrototype.RefreshVendorTypes.HasValue())
                foreach (var vendorRef in PvPPrototype.RefreshVendorTypes)
                    player.RefreshVendorInventory(vendorRef);

            PvPScore?.AddNewPlayer(player);

            var mode = CurrentMode;
            if (mode == null) return false;
            mode.OnAddPlayer(player);

            if (Debug) Logger.Warn($"AddPlayer {player.Id} {mode.PrototypeDataRef.GetNameFormatted()}");

            foreach (var state in MetaStates)
                state.OnAddPlayer(player);

            // player.RevealDiscoveryMap();

            player.Properties[PropertyEnum.PvPMode] = mode.PrototypeDataRef;

            return true;
        }

        private void UpdatePlayerCollection(Player player)
        {
            if (!_playersCollection.TryGetValue(player.DatabaseUniqueId, out PropertyCollection collection))
            {
                collection = new();
                _playersCollection[player.DatabaseUniqueId] = collection;
            }

            if (collection.IsEmpty)
            {
                var avatarProperties = player.CurrentAvatar?.Properties;
                if (avatarProperties == null) return;

                avatarProperties.AdjustProperty(1, PropertyEnum.PvPMatchCount);

                // not used
                // PvPDamageBoostForKDPct.curve table set to 1.0
                // PvPDamageReductionForKDPct.curve table set to 0.0
                /*
                avatarProperties[PropertyEnum.PvPRecentKDRatio] = 0.0f;
                avatarProperties[PropertyEnum.PvPLastMatchIndex] = newMatch;
                avatarProperties[PropertyEnum.PvPKillsDuringMatch, newMatch] = 0;
                avatarProperties[PropertyEnum.PvPDeathsDuringMatch, newMatch] = 0;
                */
            }
            else
            {
                player.AvatarProperties.FlattenCopyFrom(collection, false);
            }
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

        public bool OnResurrect(Player player)
        {
            bool result = false;

            var mode = CurrentMode;
            if (mode != null) result = mode.OnResurrect(player);

            if (result) return true;
            
            if (GetTeamByPlayer(player) is not PvPTeam pvpTeam) return false;

            PrototypeId targetRef = PrototypeId.Invalid;
            if (mode != null) targetRef = mode.GetStartTargetOverride(player);

            if (targetRef == PrototypeId.Invalid) targetRef = pvpTeam.StartTarget;

            if (targetRef != PrototypeId.Invalid)
            {
                using Teleporter teleporter = ObjectPoolManager.Instance.Get<Teleporter>();
                teleporter.Initialize(player, TeleportContextEnum.TeleportContext_Resurrect);
                teleporter.TeleportToTarget(targetRef);
                result = true;
            }            

            return result;
        }

        public void UpdateRunestonesScore(Player player, int runestones)
        {            
            if (EventHandler is not PvPScoreEventHandler handler || PvPScore == null) return;
            PvPScore.SetPlayerScoreValue(player, runestones, handler.Prototype.Runestones);
        }
    }
}
