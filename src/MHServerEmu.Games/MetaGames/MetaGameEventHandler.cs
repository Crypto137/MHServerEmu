using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.MetaGames
{
    public class MetaGameEventHandler(MetaGame metaGame, MetaGameEventHandlerPrototype proto)
    {
        public PrototypeId PrototypeRef { get; private set; } = proto.DataRef;
        public MetaGame MetaGame { get; private set; } = metaGame;

        protected Event<EntityDeadGameEvent>.Action _entityDeadAction;
        protected Event<AdjustHealthGameEvent>.Action _adjustHealthAction;

        protected void RegisterEvents()
        {
            var region = MetaGame.Region;
            if (region == null) return;
            region.EntityDeadEvent.AddActionBack(_entityDeadAction);
            region.AdjustHealthEvent.AddActionBack(_adjustHealthAction);
        }

        public void UnRegisterEvents()
        {
            var region = MetaGame?.Region;
            if (region != null) return;
            region.EntityDeadEvent.RemoveAction(_entityDeadAction);
            region.AdjustHealthEvent.RemoveAction(_adjustHealthAction);
        }
    }

    public class PvPScoreEventHandler : MetaGameEventHandler
    {
        private PvPScoreEventHandlerPrototype _proto;
        public PvPScoreEventHandlerPrototype Prototype => _proto;
        public PvPScoreEventHandler(MetaGame metaGame, MetaGameEventHandlerPrototype proto) : base(metaGame, proto)
        {
            _proto = proto as PvPScoreEventHandlerPrototype;
            _entityDeadAction = OnEntityDead;
            _adjustHealthAction = OnAdjustHealth;
            RegisterEvents();
        }

        private void OnEntityDead(in EntityDeadGameEvent evt)
        {
            if (MetaGame is not PvP pvp) return;
            var score = pvp.PvPScore;
            var mode = pvp.CurrentMode;

            var killer = evt.Killer;
            if (evt.Defender is not Avatar avatar) return;

            int killingSpree;
            bool killingSpreeSend = false;

            // defender
            var defender = avatar.GetOwnerOfType<Player>();
            if (defender != null)
            {
                score.UpdatePlayerScoreValue(defender, 1, _proto.DeathsEntry);
                if (mode != null 
                    && pvp.PvPPrototype.VOKillSpreeShutdown != AssetId.Invalid
                    && score.TryGetPlayerScore(defender, _proto.KillingSpreeEntry, out killingSpree) 
                    && killingSpree >= 2)
                {
                    mode.SendPlayUISoundTheme(pvp.PvPPrototype.VOKillSpreeShutdown);
                    killingSpreeSend = true;
                }
                score.SetPlayerScoreValue(defender, 0, _proto.KillingSpreeEntry);
            }

            // assists
            if (_proto.AssistsEntry >= 0)
            {
                var tags = avatar.TagPlayers;
                foreach (var player in tags.GetPlayers(TimeSpan.FromMilliseconds(_proto.AssistsMS)))
                {
                    if (player == killer) continue;

                    score.UpdatePlayerScoreValue(player, 1, _proto.AssistsEntry);
                    avatar.Properties.AdjustProperty(1, PropertyEnum.PvPAssists);
                    EvalRunestoneAssistReward(player, score);
                }
            }

            // killer
            if (killer != null)
            {
                score.UpdatePlayerScoreValue(killer, 1, _proto.KillsEntry);
                score.UpdatePlayerScoreValue(killer, 1, _proto.KillingSpreeEntry);
                if (killingSpreeSend == false
                    && mode != null
                    && pvp.PvPPrototype.VOKillSpreeList.HasValue()
                    && score.TryGetPlayerScore(killer, _proto.KillingSpreeEntry, out killingSpree)
                    && killingSpree >= pvp.PvPPrototype.VOKillSpreeList.Length)
                {
                    var spreeVO = pvp.PvPPrototype.VOKillSpreeList[killingSpree - 1];
                    mode.SendPlayUISoundTheme(spreeVO);
                }
            }
        }

        private void EvalRunestoneAssistReward(Player player, ScoreTable score)
        {
            if (_proto.EvalRunestoneAssistReward == null) return;

            if (score.TryGetPlayerScore(player, _proto.AssistsEntry, out int assists)
                && score.TryGetPlayerScore(player, _proto.KillsEntry, out int kills))
            {
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.Game = MetaGame.Game;
                evalContext.SetVar_Int(EvalContext.Var1, assists);
                evalContext.SetVar_Int(EvalContext.Var2, kills);
                int runes = Eval.RunInt(_proto.EvalRunestoneAssistReward, evalContext);
                if (runes > 0)
                {
                    var avatar = player.CurrentAvatar;
                    avatar.Properties.AdjustProperty(runes, PropertyEnum.RunestonesAmount);
                }
            }
        }

        private void OnAdjustHealth(in AdjustHealthGameEvent evt)
        {
            if (MetaGame is not PvP pvp) return;
            var score = pvp.PvPScore;

            bool isAvatar = evt.Entity is Avatar;
            var attacker = evt.Player;
            int damage = -(int)evt.Damage;

            if (attacker != null && damage > 0)
            {
                score.UpdatePlayerScoreValue(attacker, damage, _proto.DamageVsTotalEntry);
                if (isAvatar)
                    score.UpdatePlayerScoreValue(attacker, damage, _proto.DamageVsPlayersEntry);
                else
                    score.UpdatePlayerScoreValue(attacker, damage, _proto.DamageVsMinionsEntry);
            }

            if (isAvatar)
            {
                var defender = evt.Entity.GetOwnerOfType<Player>();
                if (defender != null && damage > 0)
                    score.UpdatePlayerScoreValue(defender, damage, _proto.DamageTakenEntry);
            }
        }
    }

    public class PvEScoreEventHandler : MetaGameEventHandler
    {
        private PvEScoreEventHandlerPrototype _proto;
        public PvEScoreEventHandler(MetaGame metaGame, MetaGameEventHandlerPrototype proto) : base(metaGame, proto)
        {
            _proto = proto as PvEScoreEventHandlerPrototype;
            _entityDeadAction = OnEntityDead;
            _adjustHealthAction = OnAdjustHealth;
            RegisterEvents();
        }

        private void OnEntityDead(in EntityDeadGameEvent evt)
        {
            // Not used
        }

        private void OnAdjustHealth(in AdjustHealthGameEvent evt)
        {
            // Not used
        }

    }
}
