using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.MetaGames.GameModes
{
    public class PvPDefenderGameMode : MetaGameMode
    {
        private readonly PvPDefenderGameModePrototype _proto;
        private readonly HashSet<ulong> _defenders;
        private readonly Dictionary<ulong, PrototypeId> _attackers;
        private readonly List<ulong> _turrets;
        private readonly Dictionary<ulong, TimeSpan> _respawnPlayers;

        private readonly Event<EntityAggroedGameEvent>.Action _entityAggroedAction;
        private readonly Event<EntityDeadGameEvent>.Action _entityDeadAction;
        private readonly Event<EntityEnteredWorldGameEvent>.Action _entityEnteredWorldAction;
        private readonly Event<EntityExitedWorldGameEvent>.Action _entityExitedWorldAction;

        private readonly EventPointer<SoftLockRegionEvent> _softLockRegionEvent = new();
        private readonly EventPointer<AttakerWaveEvent> _attackerWaveEvent = new();
        private readonly EventPointer<TurretVulnerabilityEvent> _turretVulnerabilityEvent = new();
        private readonly EventPointer<DefenderVulnerabilityEvent> _defenderVulnerabilityEvent = new();
        private readonly EventPointer<RespawnPlayersEvent> _respawnPlayersEvent = new();
        private readonly EventGroup _timedBanners = new();

        private int _totalKills;

        public const int MaxAttackers = 7; // optimal 6-8

        public PvPDefenderGameMode(MetaGame metaGame, MetaGameModePrototype proto) : base(metaGame, proto)
        {
            _proto = proto as PvPDefenderGameModePrototype;
            _defenders = [];
            _attackers = [];
            _turrets = [];
            _respawnPlayers = [];

            _entityAggroedAction = OnEntityAggroed;
            _entityDeadAction = OnEntityDead;
            _entityEnteredWorldAction = OnDefenderEntityEnteredWorld;
            _entityExitedWorldAction = OnEntityExitedWorld;
        }

        #region Override

        public override void OnDestroy()
        {
            Game?.GameEventScheduler?.CancelAllEvents(_timedBanners);
            base.OnDestroy();
        }

        public override void OnActivate()
        {
            if (Region == null) return;

            base.OnActivate();
            SetModeText(_proto.Name);

            if (_proto.StartTargetOverrides.HasValue())
                foreach (var target in _proto.StartTargetOverrides) 
                {
                    if (target.StartTarget == PrototypeId.Invalid || target.Team == PrototypeId.Invalid) continue;
                    Region.Properties[PropertyEnum.RegionBodysliderTargetOverride, target.Team] = target.StartTarget;
                }

            GetDefenders();
            SpawnTurrets();
            ApplyInvinciblePowers();
            RegisterEvents();

            _totalKills = 0;
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();

            UnRegisterEvents();
            UnassignPowerLockForPlayers();
            if (_proto.ShowTimer) SendStopPvPTimer();
            DestroyEntities();

            foreach (var player in new PlayerIterator(Region))
                player.SetAllianceOverride(null);
        }

        public override void OnAddPlayer(Player player)
        {
            base.OnAddPlayer(player);
            SendSetModeText(player);
            TeleportPlayerToStartTarget(player);
            SendTrackedEntitiesToPlayer(player);
        }

        public override void OnRemovePlayer(Player player)
        {
            UnassignPowerLockForPlayer(player);
        }

        public override bool OnResurrect(Player player)
        {
            TeleportPlayerToStartTarget(player);
            RespawnPlayer(player);
            return true;
        }

        private void RespawnPlayer(Player player)
        {
            var avatar = player.CurrentAvatar;
            if (avatar == null) return;
            AssignPowerLock(avatar);

            var currentTime = Game.CurrentTime;
            var time = currentTime - _startTime;

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetVar_Int(EvalContext.Var1, (int)time.TotalSeconds);
            float respawnSeconds = Math.Max(1f, Eval.RunFloat(_proto.TimeToRespawn, evalContext));
            var timeToRespawn = TimeSpan.FromSeconds(respawnSeconds);

            ulong guid = player.DatabaseUniqueId;
            _respawnPlayers[guid] = currentTime + timeToRespawn;

            ScheduleRespawnPlayer(guid, timeToRespawn);

            if (_proto.ShowTimer)
                SendStartPvPTimer(timeToRespawn, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, player, _proto.DeathTimerText);

            ScheduleTimedBanners(player);

            if (_proto.BannerMsgPlayerDefeatLock != LocaleStringId.Blank)
            {
                var interestedClients = ListPool<PlayerConnection>.Instance.Get();
                GetInterestedClients(interestedClients, player);
                var intArgs = ListPool<long>.Instance.Get();
                intArgs.Add((long)respawnSeconds);
                SendMetaGameBanner(interestedClients, _proto.BannerMsgPlayerDefeatLock, intArgs);
                ListPool<PlayerConnection>.Instance.Return(interestedClients);
                ListPool<long>.Instance.Return(intArgs);
            }
        }

        public override PrototypeId GetStartTargetOverride(Player player)
        {
            var team = MetaGame.GetTeamByPlayer(player);
            if (team != null && _proto.StartTargetOverrides.HasValue())
                foreach (var target in _proto.StartTargetOverrides)
                    if (team.ProtoRef == target.Team)
                        return target.StartTarget;

            return PrototypeId.Invalid;
        }

        #endregion

        #region Turrets Defenders Attackers

        private void ApplyInvinciblePowers()
        {
            var manager = Game.EntityManager;

            if (_proto.TurretInvinciblePower != PrototypeId.Invalid)
                foreach (var turretId in _turrets)
                {
                    var turret = manager.GetEntity<WorldEntity>(turretId);
                    SetInvisible(turret, _proto.TurretInvinciblePower, true);
                }

            if (_proto.DefenderInvinciblePower != PrototypeId.Invalid)
                foreach (var defenderId in _defenders)
                {
                    var defender = manager.GetEntity<WorldEntity>(defenderId);
                    SetInvisible(defender, _proto.DefenderInvinciblePower, true);
                }

            foreach (var team in MetaGame.Teams)
            {
                bool turretFound = false;
                int turretGroupId = -1;
                foreach (var turretId in _turrets)
                {
                    var turret = manager.GetEntity<WorldEntity>(turretId);
                    if (turret.Properties[PropertyEnum.MetaGameTeam] == team.ProtoRef)
                    {
                        turretFound = true;

                        var turretData = GetTurretData(turret.PrototypeDataRef);
                        if (turretData == null) continue;

                        if (turretGroupId == -1)
                        {
                            turretGroupId = turretData.TurretGroupId;
                            SendTrackedEntityToPlayers(turret);
                            SetInvisible(turret, _proto.TurretInvinciblePower, false);
                        }
                        else if (turretGroupId == turretData.TurretGroupId)
                        {
                            SetInvisible(turret, _proto.TurretInvinciblePower, false);
                        }
                    }
                }

                if (turretFound) continue;             
                
                foreach (var defenderId in _defenders)
                {
                    var defender = manager.GetEntity<WorldEntity>(defenderId);
                    if (defender.Properties[PropertyEnum.MetaGameTeam] == team.ProtoRef)
                    {
                        SendTrackedEntityToPlayers(defender);
                        SetInvisible(defender, _proto.DefenderInvinciblePower, false);
                    }
                }
            }
        }

        private int AttackerAllianceCount(PopulationObjectPrototype popObject)
        {
            int count = 0;
            if (_attackers.Count == 0) return count;

            var entities = HashSetPool<PrototypeId>.Instance.Get();
            try
            {
                popObject.GetContainedEntities(entities);
                foreach (var entityRef in entities)
                {
                    var attackerProto = GameDatabase.GetPrototype<WorldEntityPrototype>(entityRef);
                    if (attackerProto == null) continue;

                    var allianceRef = attackerProto.Alliance;
                    foreach (var alliance in _attackers.Values)
                        if (alliance == allianceRef) count++;

                    break;
                }
                return count;
            }
            finally
            {
                HashSetPool<PrototypeId>.Instance.Return(entities);
            }
        }

        private PvPTurretDataPrototype GetTurretData(PrototypeId prototypeDataRef)
        {
            var entities = HashSetPool<PrototypeId>.Instance.Get();
            try
            {
                foreach (var turretData in _proto.Turrets)
                {
                    entities.Clear();
                    turretData.TurretPopulation.GetContainedEntities(entities);
                    if (entities.Contains(prototypeDataRef))
                        return turretData;
                }
                return null;
            }
            finally
            {
                HashSetPool<PrototypeId>.Instance.Return(entities);
            }
        }

        private PvPDefenderDataPrototype GetDefenderData(PrototypeId defenderRef)
        {
            if (defenderRef == PrototypeId.Invalid) return null;
            foreach (var defenderData in _proto.Defenders)
                if (defenderData.Defender == defenderRef)
                    return defenderData;

            return null;
        }

        private PvPDefenderDataPrototype GetDefenderDataPrototype(PrototypeId teamRef, PrototypeId allianceRef)
        {
            foreach (var defenderData in _proto.Defenders)
            {
                var defenderProto = GameDatabase.GetPrototype<WorldEntityPrototype>(defenderData.Defender);
                if (defenderProto == null) continue;
                if (teamRef != PrototypeId.Invalid)
                {
                    if (defenderData.Team == teamRef)
                        return defenderData;
                }
                else if (defenderProto.Alliance == allianceRef)
                    return defenderData;
            }
            return null;
        }

        private void GetDefenders()
        {
            _defenders.Clear();
            var active = new EntityRegionSPContext(EntityRegionSPContextFlags.ActivePartition);
            foreach (var defenderData in _proto.Defenders)
                foreach (var defender in Region.IterateEntitiesInRegion(active))
                    if (defender.PrototypeDataRef == defenderData.Defender)
                    {
                        _defenders.Add(defender.Id);
                        MetaGame.DiscoverEntity(defender);
                        defender.Properties[PropertyEnum.MissionXEncounterHostilityOk] = true;

                        if (defenderData.Team != PrototypeId.Invalid)
                            defender.Properties[PropertyEnum.MetaGameTeam] = defenderData.Team;
                    }
        }

        private void SpawnTurrets()
        {
            _turrets.Clear();
            var populationManager = Region.PopulationManager;

            var spawnedTurrets = ListPool<WorldEntity>.Instance.Get();
            foreach (var turretData in _proto.Turrets)
            {
                spawnedTurrets.Clear();
                populationManager.SpawnObjectUsePopulationMarker(turretData.TurretPopulation, spawnedTurrets);

                foreach (var turret in spawnedTurrets)
                {
                    _turrets.Add(turret.Id);
                    MetaGame.DiscoverEntity(turret);
                    if (turretData.Team != PrototypeId.Invalid)
                        turret.Properties[PropertyEnum.MetaGameTeam] = turretData.Team;
                }
            }
            ListPool<WorldEntity>.Instance.Return(spawnedTurrets);
        }

        private void DestroyEntities()
        {
            var manager = Game.EntityManager;
            foreach (var defenderId in _defenders)
            {
                var defender = manager.GetEntity<WorldEntity>(defenderId);
                defender?.Destroy();
            }
            _defenders.Clear();

            foreach (var turretId in _turrets)
            {
                var turret = manager.GetEntity<WorldEntity>(turretId);
                turret?.Destroy();
            }
            _turrets.Clear();

            foreach (var attackerId in _attackers.Keys)
            {
                var attacker = manager.GetEntity<WorldEntity>(attackerId);
                if (attacker != null && attacker is not Avatar)
                    attacker.Destroy();
            }
            _attackers.Clear();
        }

        #endregion

        #region SendMessage

        private void SendTrackedEntityToPlayers(WorldEntity entity)
        {
            var teamRef = entity.Properties[PropertyEnum.MetaGameTeam];
            foreach (var player in new PlayerIterator(Region))
            {
                var team = MetaGame.GetTeamByPlayer(player);
                if (team.ProtoRef == teamRef)
                    SetUITrackedEntityId(entity.Id, player);
            }
        }

        private void SendTrackedEntitiesToPlayer(Player player)
        {
            var manager = Game.EntityManager;
            var team = MetaGame.GetTeamByPlayer(player);

            foreach (var turretId in _turrets)
            {
                var turret = manager.GetEntity<WorldEntity>(turretId);
                if (turret == null) continue;
                if (turret.Properties[PropertyEnum.MetaGameTeam] == team.ProtoRef)
                {
                    SetUITrackedEntityId(turret.Id, player);
                    return;
                }
            }

            foreach (var defenderId in _defenders)
            {
                var defender = manager.GetEntity<WorldEntity>(defenderId);
                if (defender == null) continue;
                if (defender.Properties[PropertyEnum.MetaGameTeam] == team.ProtoRef)
                    SetUITrackedEntityId(defender.Id, player);
            }
        }

        private void SendPlayerDefeatedPlayer(Player attacker, Player defender)
        {
            if (Game == null || Region == null) return;

            var interestedClients = ListPool<PlayerConnection>.Instance.Get();

            GetInterestedClients(interestedClients);
            Game.ChatManager.SendChatFromMetaGame(_proto.ChatMessagePlayerDefeatedPlayer, interestedClients, attacker, defender);

            interestedClients.Clear();
            GetInterestedClients(interestedClients, attacker);
            SendMetaGameBanner(interestedClients, _proto.BannerMsgPlayerDefeatAttacker, null, attacker.GetName(), defender.GetName());

            interestedClients.Clear();
            GetInterestedClients(interestedClients, defender);
            SendMetaGameBanner(interestedClients, _proto.BannerMsgPlayerDefeatDefender, null, attacker.GetName(), defender.GetName());

            interestedClients.Clear();
            foreach (var regionPlayer in new PlayerIterator(Region))
                if (regionPlayer != attacker && regionPlayer != defender)
                    interestedClients.Add(regionPlayer.PlayerConnection);
            
            SendMetaGameBanner(interestedClients, _proto.BannerMsgPlayerDefeatOther, null, attacker.GetName(), defender.GetName());

            ListPool<PlayerConnection>.Instance.Return(interestedClients);
        }

        private void SendNPDefeatPlayer(WorldEntity attacker, Player defender)
        {
            var attackerName = attacker.Prototype.DisplayName;
            if (attackerName == LocaleStringId.Blank) return;

            var interestedClients = ListPool<PlayerConnection>.Instance.Get();

            GetInterestedClients(interestedClients);
            Game.ChatManager.SendChatFromMetaGame(_proto.ChatMessageNPDefeatedPlayer, interestedClients, defender, null, attackerName);

            interestedClients.Clear();
            GetInterestedClients(interestedClients, defender);
            SendMetaGameBanner(interestedClients, _proto.BannerMsgNPDefeatPlayerDefender, null, "", defender.GetName(), attackerName);

            interestedClients.Clear();
            foreach (var regionPlayer in new PlayerIterator(Region))
                if (regionPlayer != defender)
                    interestedClients.Add(regionPlayer.PlayerConnection);

            SendMetaGameBanner(interestedClients, _proto.BannerMsgNPDefeatPlayerOther, null, "", defender.GetName(), attackerName);

            ListPool<PlayerConnection>.Instance.Return(interestedClients);
        }

        private void SendPlayerMetaGameComplete(PrototypeId teamRef)
        {
            var region = Region;
            if (region == null || MetaGame is not PvP pvp) return;
            var score = pvp.PvPScore;
            int maxMatches = 10;

            PvPScoreEventHandlerPrototype scoreHandlerProto = null;
            int didNotPartipateDamage = 0;
            if (score != null && pvp.EventHandler is PvPScoreEventHandler scoreHandler) 
            {
                scoreHandlerProto = scoreHandler.Prototype;
                var runTime = Game.CurrentTime - _startTime;
                didNotPartipateDamage = (int)(runTime.TotalSeconds * _proto.DidNotParticipateDmgPerMinuteMin) / 60;
            }

            foreach (var player in new PlayerIterator(region))
            {
                var avatar = player.CurrentAvatar;

                int recentMatches = avatar.Properties[PropertyEnum.PvPMatchCount] - 1;                
                if (recentMatches < 0) recentMatches = 0;
                else if (recentMatches > maxMatches) recentMatches = maxMatches;

                float recentRatio = avatar.Properties[PropertyEnum.PvPRecentWinLossRatio];
                float recentWins = recentRatio * recentMatches;

                bool playerDidNotPartipate = false;
                if (scoreHandlerProto != null)
                {
                    if (score.TryGetPlayerScore(player, scoreHandlerProto.DamageVsTotalEntry, out int damage))
                        playerDidNotPartipate = damage < didNotPartipateDamage;
                }

                MetaGameCompleteType complete;

                if (playerDidNotPartipate)
                {
                    complete = MetaGameCompleteType.DidNotParticipate;
                }
                else
                {
                    var team = MetaGame.GetTeamByPlayer(player);
                    if (team.ProtoRef == teamRef)
                    {
                        complete = MetaGameCompleteType.Success;
                        avatar.Properties.AdjustProperty(1, PropertyEnum.PvPWins);
                        player.Properties.AdjustProperty(1, PropertyEnum.PvPWins);
                        player.OnScoringEvent(new(ScoringEventType.PvPMatchWon));

                        recentWins += 1;
                        if (recentWins > maxMatches) recentWins = maxMatches;
                    }
                    else
                    {
                        complete = MetaGameCompleteType.Failure; 
                        avatar.Properties.AdjustProperty(1, PropertyEnum.PvPLosses);
                        player.Properties.AdjustProperty(1, PropertyEnum.PvPLosses);
                        player.OnScoringEvent(new(ScoringEventType.PvPMatchLost));

                        recentWins -= 1;
                        if (recentWins < 0) recentWins = 0;
                    }
                }

                if (recentMatches < maxMatches) recentMatches++;

                // calculate new Ratio
                recentRatio = 0f;
                if (recentMatches > 0) recentRatio = Math.Max(1f, recentWins / recentMatches);
                avatar.Properties[PropertyEnum.PvPRecentWinLossRatio] = recentRatio;

                // send event
                region.PlayerMetaGameCompleteEvent.Invoke(new(player, MetaGame.PrototypeDataRef, complete));
            }
        }

        #endregion

        #region Powers

        private static PowerUseResult AcivatePowerForEntity(PrototypeId powerRef, WorldEntity entity)
        {
            PowerIndexProperties indexProps = new(0, entity.CharacterLevel, entity.CombatLevel);
            entity.AssignPower(powerRef, indexProps);
            var position = entity.RegionLocation.Position;
            var powerSettings = new PowerActivationSettings(entity.Id, Vector3.Zero, position)
            { Flags = PowerActivationSettingsFlags.NotifyOwner };
            return entity.ActivatePower(powerRef, ref powerSettings);
        }

        private static void SetInvisible(WorldEntity entity, PrototypeId powerRef, bool invisible)
        {
            if (entity == null || entity.IsInWorld == false) return;

            entity.Properties[PropertyEnum.Invulnerable] = invisible;
            entity.Properties[PropertyEnum.Untargetable] = invisible;

            if (invisible)
            {
                if (entity.HasPowerInPowerCollection(powerRef)) return;
                AcivatePowerForEntity(powerRef, entity);
            }
            else
                entity.UnassignPower(powerRef);
        }

        private void AssignPowerLock(Avatar avatar)
        {
            avatar.Properties[PropertyEnum.SystemImmobilized] = true;
            avatar.Properties[PropertyEnum.Invulnerable] = true;
            avatar.Properties[PropertyEnum.PowerLock] = true;

            if (avatar.IsInWorld && _proto.PlayerLockVisualsPower != PrototypeId.Invalid)
                AcivatePowerForEntity(_proto.PlayerLockVisualsPower, avatar);
        }

        private void UnassignPowerLockForPlayer(Player player)
        {                
            var avatar = player?.CurrentAvatar;
            if (avatar == null) return;

            avatar.Properties.RemoveProperty(PropertyEnum.SystemImmobilized);
            avatar.Properties.RemoveProperty(PropertyEnum.Invulnerable);
            avatar.Properties.RemoveProperty(PropertyEnum.PowerLock);

            if (_proto.PlayerLockVisualsPower != PrototypeId.Invalid)
                avatar.UnassignPower(_proto.PlayerLockVisualsPower);
        }

        private void UnassignPowerLockForPlayers()
        {
            var manager = Game.EntityManager;
            foreach (var guid in _respawnPlayers.Keys)
            {
                var player = manager.GetEntityByDbGuid<Player>(guid);
                UnassignPowerLockForPlayer(player);
            }
            _respawnPlayers.Clear();
        }

        #endregion

        #region Teleport

        private void TeleportPlayerToStartTarget(Player player)
        {
            var avatar = player.CurrentAvatar;
            if (avatar == null || avatar.IsInWorld == false) return;

            var targetRef = GetStartTargetOverride(player);
            if (targetRef == PrototypeId.Invalid) return;

            var targetEntity = GetTargetEntity(targetRef);
            if (targetEntity != null)
                avatar.ChangeRegionPosition(targetEntity.RegionLocation.Position, null);
        }

        private WorldEntity GetTargetEntity(PrototypeId startTargetRef)
        {
            var startTarget = GameDatabase.GetPrototype<RegionConnectionTargetPrototype>(startTargetRef);
            if (startTarget == null) return null;
            foreach (var entity in Region.Entities) 
            {
                var target = entity as WorldEntity;
                if (target.PrototypeDataRef != startTarget.Entity) continue;

                var targetCell = GameDatabase.GetDataRefByAsset(startTarget.Cell);
                if (target.Cell.PrototypeDataRef != targetCell) continue;

                if (target.Area.PrototypeDataRef != startTarget.Area) continue;

                return target;
            }
            return null;
        }

        #endregion

        #region GameEvents

        private void RegisterEvents()
        {
            Region.EntityAggroedEvent.AddActionBack(_entityAggroedAction);
            Region.EntityDeadEvent.AddActionBack(_entityDeadAction);
            Region.EntityEnteredWorldEvent.AddActionBack(_entityEnteredWorldAction);
            Region.EntityExitedWorldEvent.AddActionBack(_entityExitedWorldAction);

            ScheduleEvent(_softLockRegionEvent, _proto.SoftLockRegionMS);

            if (_proto.AttackerWaveInitialDelayMS > 0)
                ScheduleEvent(_attackerWaveEvent, _proto.AttackerWaveInitialDelayMS);
            else
                ScheduledAttakerWave();

            ScheduleEvent(_defenderVulnerabilityEvent, _proto.DefenderVulnerabilityIntervalMS);
            ScheduleEvent(_turretVulnerabilityEvent, _proto.TurretVulnerabilityIntervalMS);

            ScheduledRespawnPlayers();
        }

        private void UnRegisterEvents()
        {
            if (Region == null) return;

            Region.EntityAggroedEvent.RemoveAction(_entityAggroedAction);
            Region.EntityDeadEvent.RemoveAction(_entityDeadAction);
            Region.EntityEnteredWorldEvent.RemoveAction(_entityEnteredWorldAction);
            Region.EntityExitedWorldEvent.RemoveAction(_entityExitedWorldAction);

            var scheduler = Game.GameEventScheduler;
            if (scheduler == null) return;

            scheduler.CancelEvent(_softLockRegionEvent);
            scheduler.CancelEvent(_attackerWaveEvent);
            scheduler.CancelEvent(_defenderVulnerabilityEvent);
            scheduler.CancelEvent(_turretVulnerabilityEvent);
            scheduler.CancelEvent(_respawnPlayersEvent);
            scheduler.CancelAllEvents(_timedBanners);
        }

        private void OnEntityDead(in EntityDeadGameEvent evt)
        {
            var defender = evt.Defender;
            if (defender == null || Game == null || MetaGame is not PvP pvp) return;
            var pvpPrototype = pvp.PvPPrototype;

            var manager = Game.EntityManager;

            bool isTurret = false;
            foreach (var turretId in _turrets)
            {
                var turret = manager.GetEntity<WorldEntity>(turretId);
                if (turret == defender)
                {
                    isTurret = true;
                    break;
                }
            }

            if (isTurret)
            {
                var turredData = GetTurretData(defender.PrototypeDataRef);
                if (turredData != null)
                {
                    SendUINotification(turredData.DeathUINotification);
                    SendPlayUISoundTheme(turredData.DeathAudioTheme);
                }

                MetaGame.UniscoverEntity(defender);
                _turrets.Remove(defender.Id);
                ApplyInvinciblePowers();
            }

            bool sendAudio = false;

            if (_defenders.Contains(defender.Id))
            { 
                var defenderData = GetDefenderData(defender.PrototypeDataRef);
                if (defenderData != null)
                {
                    SendUINotification(defenderData.DeathUINotification);
                    if (defenderData.DeathAudioTheme != AssetId.Invalid)
                    {
                        SendPlayUISoundTheme(defenderData.DeathAudioTheme);
                        sendAudio = true;
                    }

                    var team = pvp.GetOtherTeamByRef(defender.Properties[PropertyEnum.MetaGameTeam]);
                    if (team != null)
                        SendPlayerMetaGameComplete(team.ProtoRef);

                    MetaGame.ScheduleActivateGameMode(_proto.NextMode);
                }

                MetaGame.UniscoverEntity(defender);
                _defenders.Remove(defender.Id);
            }

            if (_attackers.ContainsKey(defender.Id))
                _attackers.Remove(defender.Id);
            
            if (defender is not Avatar avatarDefender) return;
            var playerDefender = avatarDefender.GetOwnerOfType<Player>();
            if (playerDefender == null) return;

            var playerAttacker = evt.Killer;
            if (playerAttacker != null)
            {
                var avatarAttacker = playerAttacker.CurrentAvatar;
                if (avatarAttacker != null)
                {
                    SendPlayerDefeatedPlayer(playerAttacker, playerDefender);

                    if (avatarAttacker.Properties[PropertyEnum.PvPLastKilledByEntityId] == playerDefender.Id)
                    {
                        if (sendAudio == false && pvpPrototype.VORevenge != AssetId.Invalid)
                        {
                            SendPlayUISoundTheme(pvpPrototype.VORevenge);
                            sendAudio = true;
                        }
                        avatarAttacker.Properties.RemoveProperty(PropertyEnum.PvPLastKilledByEntityId);
                    }
                    avatarDefender.Properties[PropertyEnum.PvPLastKilledByEntityId] = playerAttacker.Id;

                    if (sendAudio == false && pvpPrototype.VOTeammateKilled != AssetId.Invalid)
                    {
                        if (pvp.GetTeamByPlayer(playerDefender) is PvPTeam pvpTeam)
                        {
                            var teammate = pvpTeam.GetTeammateByPlayer(playerDefender);
                            if (teammate != null)
                            {
                                SendPlayUISoundTheme(pvpPrototype.VOTeammateKilled);
                                sendAudio = true;
                            }
                        }
                    }
                }
            }
            else
            {
                avatarDefender.Properties.RemoveProperty(PropertyEnum.PvPLastKilledByEntityId);
                var attacker = evt.Attacker;
                if (attacker != null) SendNPDefeatPlayer(attacker, playerDefender);
            }

            if (++_totalKills == 1 && sendAudio == false)
                SendPlayUISoundTheme(pvpPrototype.VOFirstKill);
        }

        private void OnDefenderEntityEnteredWorld(in EntityEnteredWorldGameEvent evt)
        {
            if (evt.Entity is not Avatar avatar) return;

            var allianceProto = avatar.Alliance;
            if (allianceProto == null) return;

            var teamRef = PrototypeId.Invalid;

            var player = avatar.GetOwnerOfType<Player>();
            if (player == null) return;

            if (MetaGame.GetTeamByPlayer(player) is PvPTeam team)
            {
                teamRef = team.ProtoRef;
                allianceProto = team.Alliance;
            }

            // Assign Boost Power FactionMode0Main only!!!
            var defenderProto = GetDefenderDataPrototype(teamRef, allianceProto.DataRef);
            if (defenderProto != null && defenderProto.Boost != PrototypeId.Invalid)
            {
                PowerIndexProperties indexProps = new(0, avatar.CharacterLevel, avatar.CombatLevel);
                avatar.AssignPower(defenderProto.Boost, indexProps);
            }

            // PlayerLockVisualsPower
            if (_proto.PlayerLockVisualsPower != PrototypeId.Invalid && _respawnPlayers.ContainsKey(player.DatabaseUniqueId))
            {
                AcivatePowerForEntity(_proto.PlayerLockVisualsPower, avatar);
            }
        }

        private void OnEntityExitedWorld(in EntityExitedWorldGameEvent evt)
        {
            if (evt.Entity is not Avatar avatar) return;

            // Unassign Boost Power FactionMode0Main only!!!
            var allianceProto = avatar.Alliance;
            if (allianceProto == null) return;

            var player = avatar.GetOwnerOfType<Player>();
            if (player == null) return;

            var teamRef = PrototypeId.Invalid;
            if (MetaGame.GetTeamByPlayer(player) is PvPTeam team)
                teamRef = team.ProtoRef;

            var defenderProto = GetDefenderDataPrototype(teamRef, allianceProto.DataRef);
            if (defenderProto != null && defenderProto.Boost != PrototypeId.Invalid)
                avatar.UnassignPower(defenderProto.Boost);
        }

        private void OnEntityAggroed(in EntityAggroedGameEvent evt)
        {
            var defender = evt.AggroEntity;
            if (defender == null || _defenders.Contains(defender.Id) == false) return;
            
            var defenderData = GetDefenderData(defender.PrototypeDataRef);
            if (defenderData == null) return;
            SendUINotification(defenderData.UnderAttackUINotification);
            SendPlayUISoundTheme(defenderData.UnderAttackAudioTheme);            
        }

        #endregion

        #region Schedule

        private void ScheduleEvent<T>(EventPointer<T> eventPointer, int timeMs) where T : CallMethodEvent<PvPDefenderGameMode>, new()
        {
            if (timeMs == 0) return;
            var scheduler = Game.GameEventScheduler;
            if (scheduler == null) return;

            TimeSpan timeOffset = TimeSpan.FromMilliseconds(timeMs);
            if (eventPointer.IsValid) return;

            scheduler.ScheduleEvent(eventPointer, timeOffset, _pendingEvents);
            eventPointer.Get().Initialize(this);
        }

        private void ScheduleRespawnPlayer(ulong guid, TimeSpan timeOffset)
        {
            if (_respawnPlayers.ContainsKey(guid) == false) return;
            if (timeOffset == TimeSpan.Zero) return;
            var scheduler = Game.GameEventScheduler;
            if (scheduler == null) return;

            var eventPointer = new EventPointer<RespawnPlayerEvent>();
            scheduler.ScheduleEvent(eventPointer, timeOffset, _pendingEvents);
            eventPointer.Get().Initialize(this, guid);
        }

        private void ScheduledRespawnPlayer(ulong guid)
        {
            if (guid == 0 || _respawnPlayers.ContainsKey(guid) == false) return;
            _respawnPlayers.Remove(guid);

            ScheduledRespawnPlayers();

            var player = Game.EntityManager.GetEntityByDbGuid<Player>(guid);
            if (player == null) return;

            if (_proto.ShowTimer) SendStopPvPTimer(player);

            if (_proto.BannerMsgPlayerDefeatUnlock != LocaleStringId.Blank)
            {
                var interestedClients = ListPool<PlayerConnection>.Instance.Get();
                GetInterestedClients(interestedClients, player);
                SendMetaGameBanner(interestedClients, _proto.BannerMsgPlayerDefeatUnlock);
                ListPool<PlayerConnection>.Instance.Return(interestedClients);
            }

            CancelScheduledTimedBannersEvents(guid);

            UnassignPowerLockForPlayer(player);
        }

        private void CancelScheduledTimedBannersEvents(ulong guid)
        {
            TimedBannersEvent.GuidFilter filter = new(guid);
            Game.GameEventScheduler.CancelEventsFiltered(_timedBanners, filter);
        }

        private void ScheduledTimedBanners(ulong guid, LocaleStringId banner, int timer)
        {
            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;

            var player = Game.EntityManager.GetEntityByDbGuid<Player>(guid);
            if (player == null) return;

            if (_respawnPlayers.TryGetValue(guid, out var respawnTime) == false) return;
            respawnTime -= Game.CurrentTime;

            var interestedClients = ListPool<PlayerConnection>.Instance.Get();
            GetInterestedClients(interestedClients, player);
            var intArgs = ListPool<long>.Instance.Get();
            intArgs.Add((long)respawnTime.TotalSeconds);
            SendMetaGameBanner(interestedClients, banner, intArgs);
            ListPool<PlayerConnection>.Instance.Return(interestedClients);
            ListPool<long>.Instance.Return(intArgs);

            if (timer > 0)
            {
                EventPointer<TimedBannersEvent> timedBannerEvent = new();
                scheduler.ScheduleEvent(timedBannerEvent, TimeSpan.FromMilliseconds(timer), _timedBanners);
                timedBannerEvent.Get().Initialize(this, player.DatabaseUniqueId, banner, timer);
            }
        }

        private void ScheduleTimedBanners(Player player)
        {
            if (_proto.UITimedBannersOnDefeatLock.IsNullOrEmpty()) return;
            var scheduler = Game.GameEventScheduler;
            if (scheduler == null) return;

            foreach (var bannerTime in _proto.UITimedBannersOnDefeatLock)
            {
                if (bannerTime.TimerModeType == MetaGameModeTimerBannerType.Interval)
                {
                    EventPointer<TimedBannersEvent> timedBannerEvent = new();
                    scheduler.ScheduleEvent(timedBannerEvent, TimeSpan.FromMilliseconds(bannerTime.TimerValueMS), _timedBanners);
                    timedBannerEvent.Get().Initialize(this, player.DatabaseUniqueId, bannerTime.BannerText, bannerTime.TimerValueMS);
                }
                else if (bannerTime.TimerModeType == MetaGameModeTimerBannerType.Once)
                {
                    EventPointer<TimedBannersEvent> timedBannerEvent = new();
                    scheduler.ScheduleEvent(timedBannerEvent, TimeSpan.FromMilliseconds(bannerTime.TimerValueMS), _timedBanners);
                    timedBannerEvent.Get().Initialize(this, player.DatabaseUniqueId, bannerTime.BannerText, 0);
                }
            }
        }

        private void ScheduledRespawnPlayers()
        {
            if (Game == null) return;

            var time = Game.CurrentTime;
            foreach(var kvp in _respawnPlayers)
                if (kvp.Value <= time)
                    ScheduleRespawnPlayer(kvp.Key, TimeSpan.Zero);

            ScheduleEvent(_respawnPlayersEvent, 5000);
        }

        private void ScheduledAttakerWave()
        {
            if (_proto.Attackers.IsNullOrEmpty() || Region == null) return;
            var populationManager = Region.PopulationManager;
            var registry = Region.SpawnMarkerRegistry;
            var random = Game.Random;

            var spawnPositions = ListPool<Vector3>.Instance.Get();
            var spawnedAttackers = ListPool<WorldEntity>.Instance.Get();
            foreach (var attackerData in _proto.Attackers)
            {
                if (attackerData.WaveSpawnPosition == PrototypeId.Invalid) continue;
                var popObj = GameDatabase.GetPrototype<PopulationObjectPrototype>(attackerData.Wave);
                if (popObj == null) continue;

                if (AttackerAllianceCount(popObj) >= MaxAttackers) continue;

                spawnPositions.Clear();
                registry.GetPositionsByMarker(attackerData.WaveSpawnPosition, spawnPositions);
                if (spawnPositions.Count > 0)
                {
                    int randIndex = random.Next(0, spawnPositions.Count);
                    var position = spawnPositions[randIndex];
                    populationManager.SpawnObjectUsePosition(popObj, position, spawnedAttackers);
                }
            }

            foreach (var attacker in spawnedAttackers)
                if (attacker != null) _attackers.Add(attacker.Id, attacker.Alliance.DataRef);

            ListPool<WorldEntity>.Instance.Return(spawnedAttackers);
            ListPool<Vector3>.Instance.Return(spawnPositions);

            ScheduleEvent(_attackerWaveEvent, _proto.AttackerWaveCycleMS);
        }

        private void ScheduledSoftLockRegion()
        {
            MetaGame.SetSoftLockRegion(RegionPlayerAccess.InviteOnly);
        }

        private void ScheduledDefenderVulnerability()
        {
            if (Game == null || _proto.DefenderVulnerabilityEval == null || _defenders.Count == 0) return;
            var manager = Game.EntityManager;

            var time = Game.CurrentTime - _startTime;
            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetVar_Int(EvalContext.Var1, (int)time.TotalSeconds);
            float vulnerability = Eval.RunFloat(_proto.DefenderVulnerabilityEval, evalContext);

            foreach (var defenderId in _defenders)
            {
                var defender = manager.GetEntity<WorldEntity>(defenderId);
                if (defender == null) continue;
                defender.Properties[PropertyEnum.DamagePctVulnerabilityPvP] = vulnerability;
            }

            ScheduleEvent(_defenderVulnerabilityEvent, _proto.DefenderVulnerabilityIntervalMS);
        }

        private void ScheduledTurretVulnerability()
        {
            if (Game == null || _proto.TurretVulnerabilityEval == null || _turrets.Count == 0) return;
            var manager = Game.EntityManager;

            var time = Game.CurrentTime - _startTime;
            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetVar_Int(EvalContext.Var1, (int)time.TotalSeconds);
            float vulnerability = Eval.RunFloat(_proto.TurretVulnerabilityEval, evalContext);
            
            foreach (var turretId in _turrets)
            {
                var turret = manager.GetEntity<WorldEntity>(turretId);
                if (turret == null) continue; 
                turret.Properties[PropertyEnum.DamagePctVulnerabilityPvP] = vulnerability;
            }

            ScheduleEvent(_turretVulnerabilityEvent, _proto.TurretVulnerabilityIntervalMS);
        }

        #endregion

        #region Events

        public class TurretVulnerabilityEvent : CallMethodEvent<PvPDefenderGameMode>
        {
            protected override CallbackDelegate GetCallback() => gameMode => gameMode.ScheduledTurretVulnerability();
        }

        public class DefenderVulnerabilityEvent : CallMethodEvent<PvPDefenderGameMode>
        {
            protected override CallbackDelegate GetCallback() => gameMode => gameMode.ScheduledDefenderVulnerability();
        }

        public class SoftLockRegionEvent : CallMethodEvent<PvPDefenderGameMode>
        {
            protected override CallbackDelegate GetCallback() => gameMode => gameMode.ScheduledSoftLockRegion();
        }

        public class AttakerWaveEvent : CallMethodEvent<PvPDefenderGameMode>
        {
            protected override CallbackDelegate GetCallback() => gameMode => gameMode.ScheduledAttakerWave();
        }

        public class RespawnPlayersEvent : CallMethodEvent<PvPDefenderGameMode>
        {
            protected override CallbackDelegate GetCallback() => gameMode => gameMode.ScheduledRespawnPlayers();
        }

        public class RespawnPlayerEvent : CallMethodEventParam1<PvPDefenderGameMode, ulong>
        {
            protected override CallbackDelegate GetCallback() => (gameMode, guid) => gameMode.ScheduledRespawnPlayer(guid);
        }

        public class TimedBannersEvent : CallMethodEventParam3<PvPDefenderGameMode, ulong, LocaleStringId, int>
        {
            protected override CallbackDelegate GetCallback() => (gameMode, guid, banner, timer) 
                => gameMode.ScheduledTimedBanners(guid, banner, timer);

            public readonly struct GuidFilter : IScheduledEventFilter
            {
                private readonly ulong _guid;

                public GuidFilter(ulong guid)
                {
                    _guid = guid;
                }

                public bool Filter(ScheduledEvent @event)
                {
                    if (@event is not TimedBannersEvent timedBannersEvent)
                        return false;

                    var guid = timedBannersEvent._param1;
                    if (guid == 0) return false;

                    return guid == _guid;
                }
            }
        }

        #endregion
    }
}
