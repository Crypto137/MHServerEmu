﻿using System.Text;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.MetaGames.GameModes;
using MHServerEmu.Games.MetaGames.MetaStates;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.MetaGames
{
    public class MetaGame : Entity
    {
        public static readonly Logger Logger = LogManager.CreateLogger();

        protected RepString _name;
        protected ulong _regionId;

        public Region Region { get => GetRegion(); }
        public MetaGamePrototype MetaGamePrototype { get => Prototype as MetaGamePrototype; }
        public List<MetaState> MetaStates { get; }
        protected List<MetaGameTeam> Teams { get; }
        protected List<MetaGameMode> GameModes { get; }
        protected GRandom Random { get; }

        private HashSet<ulong> _discoveredEntities;

        private Dictionary<PrototypeId, MetaStateSpawnEvent> _metaStateSpawnMap;
        private Action<PlayerEnteredRegionGameEvent> _playerEnteredRegionAction;
        private Action<EntityEnteredWorldGameEvent> _entityEnteredWorldAction;
        private Action<EntityExitedWorldGameEvent> _entityExitedWorldAction;
        private Action<PlayerRegionChangeGameEvent> _playerRegionChangeAction;
        private Action<Entity> _destroyEntityAction;
        private int _modeIndex;

        public MetaGame(Game game) : base(game) 
        {
            MetaStates = new();
            GameModes = new();
            Teams = new();
            Random = new();
            _name = new();
            _discoveredEntities = new();
            _modeIndex = -1;
            _playerEnteredRegionAction = OnPlayerEnteredRegion;
            _entityEnteredWorldAction = OnEntityEnteredWorld;
            _entityExitedWorldAction = OnEntityExitedWorld;
            _playerRegionChangeAction = OnPlayerRegionChange;
            _destroyEntityAction = OnDestroyEntity;
        }

        public override bool Initialize(EntitySettings settings)
        {
            base.Initialize(settings);

            var game = Game;
            if (game == null) return false;

            var region = Game.RegionManager?.GetRegion(settings.RegionId);
            if (region != null)
            {
                _metaStateSpawnMap = new();
                _regionId = region.Id;
                region.RegisterMetaGame(this);
                region.PlayerEnteredRegionEvent.AddActionBack(_playerEnteredRegionAction);
                region.EntityEnteredWorldEvent.AddActionBack(_entityEnteredWorldAction);
                region.EntityExitedWorldEvent.AddActionBack(_entityExitedWorldAction);

                foreach (var kvp in region.Properties.IteratePropertyRange(PropertyEnum.MetaStateApplyOnInit))
                {
                    Property.FromParam(kvp.Key, 0, out PrototypeId stateRef);
                    if (stateRef != PrototypeId.Invalid)
                        ApplyMetaState(stateRef);
                }
            }
            else
            {
                Logger.Warn("Initialize(): region == null");
            }

            Game.EntityManager?.DestroyEntityEvent.AddActionBack(_destroyEntityAction);

            return true;
        }

        public override bool Serialize(Archive archive)
        {
            bool success = base.Serialize(archive);
            // if (archive.IsTransient)
            success &= Serializer.Transfer(archive, ref _name);
            return success;
        }

        public override void Destroy()
        {
            foreach (var mode in GameModes)
                mode.OnDestroy();

            var region = Region;
            if (region != null)
            {
                region.PlayerRegionChangeEvent.RemoveAction(_playerRegionChangeAction);
                region.PlayerEnteredRegionEvent.RemoveAction(_playerEnteredRegionAction);
                region.EntityEnteredWorldEvent.RemoveAction(_entityEnteredWorldAction);
                region.EntityExitedWorldEvent.RemoveAction(_entityExitedWorldAction);
                region.UnRegisterMetaGame(this);
            }
            Game.EntityManager?.DestroyEntityEvent.RemoveAction(_destroyEntityAction);

            foreach(var team in Teams)
            {
                team.ClearPlayers();
                DestroyTeam(team);
            }
            Teams.Clear();

            base.Destroy();
        }

        public void CreateTeams(PrototypeId[] teams)
        {
            if (teams.HasValue())
            {
                foreach(var teamRef in teams)
                {
                    var team = CreateTeam(teamRef);
                    Teams.Add(team);
                }
            }
            else
            {
                var globalsProto = GameDatabase.GlobalsPrototype;
                if (globalsProto == null) return;
                var team = CreateTeam(globalsProto.MetaGameTeamDefault);
                Teams.Add(team);
            }
        }

        public MetaGameTeam CreateTeam(PrototypeId teamRef)
        {
            var teamProto = GameDatabase.GetPrototype<MetaGameTeamPrototype>(teamRef);
            if (teamProto == null) return null;
            return new MetaGameTeam(this, teamRef, teamProto.MaxPlayers);
        }

        private void DestroyTeam(MetaGameTeam team)
        {
            team.Destroy();
        }

        public Region GetRegion()
        {
            if (_regionId == 0) return null;
            return Game.RegionManager.GetRegion(_regionId);
        }

        protected override void BindReplicatedFields()
        {
            base.BindReplicatedFields();

            _name.Bind(this, AOINetworkPolicyValues.AOIChannelProximity);
        }

        protected override void UnbindReplicatedFields()
        {
            base.UnbindReplicatedFields();

            _name.Unbind();
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            sb.AppendLine($"{nameof(_name)}: {_name}");
        }

        protected void CreateGameModes(PrototypeId[] gameModes)
        {
            if (gameModes.HasValue())
                foreach (var gameModeRef in gameModes)
                {
                    var gameMode = MetaGameMode.CreateGameMode(this, gameModeRef);
                    GameModes.Add(gameMode);
                }
        }

        public void ActivateGameMode(int index)
        {
            var proto = MetaGamePrototype;
            var region = Region;
            if (proto == null || region == null) return;

            if (index < 0 || index >= GameModes.Count) return;
            var mode = GameModes[index];
            var modeProto = mode?.Prototype;
            if (modeProto == null) return;

            // deactivate old mode
            if (_modeIndex != -1) GameModes[_modeIndex].OnDeactivate();

            // TODO modeProto.EventHandler
            // TODO lock for proto.SoftLockRegionMode

            _modeIndex = index;
            Random.Seed(region.RandomSeed + index);

            // activate new mode
            mode.OnActivate();

            foreach (var player in new PlayerIterator(region))
                player.Properties[PropertyEnum.PvPMode] = modeProto.DataRef;
        }

        public void ApplyStates(PrototypeId[] states)
        {
            if (states.IsNullOrEmpty()) return;
            foreach(var stateRef in states)
                if (CanApplyState(stateRef))
                    ApplyMetaState(stateRef);
        }

        private bool CanApplyState(PrototypeId stateRef, bool skipCooldown = false)
        {
            var stateProto = GameDatabase.GetPrototype<MetaStatePrototype>(stateRef);
            if (stateProto == null || stateProto.CanApplyState() == false) return false;

            if (skipCooldown == false && stateProto.CooldownMS > 0)
            {
                TimeSpan time = Game.CurrentTime - Properties[PropertyEnum.MetaGameTimeStateRemovedMS, stateRef];
                if (time < TimeSpan.FromMilliseconds(stateProto.CooldownMS)) return false;
            }

            bool hasPreventStates = stateProto.PreventStates.HasValue();
            bool hasPreventGroups = stateProto.PreventGroups.HasValue();
            foreach (var state in MetaStates)
            {
                var stateProtoRef = state.PrototypeDataRef;
                if (hasPreventStates) 
                    foreach(var preventState in stateProto.PreventStates)
                        if (preventState == stateProtoRef) return false;

                if (hasPreventGroups)
                    foreach(var group in stateProto.PreventGroups)
                        if (state.HasGroup(group)) return false;
            }

            if (stateProto.EvalCanActivate != null)
            {
                EvalContextData contextData = new(Game);
                contextData.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, Region.Properties);
                contextData.SetReadOnlyVar_EntityPtr(EvalContext.Default, this);
                if (Eval.RunBool(stateProto.EvalCanActivate, contextData) == false) return false;
            }

            return true;
        }

        public void ApplyMetaState(PrototypeId stateRef)
        {
            // TODO
        }

        public void RemoveStates(PrototypeId[] removeStates)
        {
            // TODO
        }

        public void RemoveGroups(AssetId[] removeGroups)
        {
            // TODO
        }

        public MetaStateSpawnEvent GetMetaStateEvent(PrototypeId state)
        {
            if (_metaStateSpawnMap.TryGetValue(state, out var spawnEvent))
            {
                spawnEvent = _metaStateSpawnMap[state];
            }
            else
            {
                spawnEvent = new MetaStateSpawnEvent(this, Region);
                _metaStateSpawnMap[state] = spawnEvent;
            }
            return spawnEvent;
        }

        #region OLD registry

        public void MetaStateRegisty(PrototypeId stateRef)
        {
            var metastate = GameDatabase.GetPrototype<MetaStatePrototype>(stateRef);

            if (metastate is MetaStateMissionProgressionPrototype missionProgression)
            {
                if (missionProgression.StatesProgression.HasValue())
                    MetaStateRegisty(missionProgression.StatesProgression.First());
            }
            else if (metastate is MetaStateMissionActivatePrototype missionActivate)
            {
                if (missionActivate.SubStates.HasValue())
                    foreach (var state in missionActivate.SubStates)
                        MetaStateRegisty(state);

                var metaStateRef = missionActivate.DataRef;
                Logger.Debug($"State [{GameDatabase.GetFormattedPrototypeName(metaStateRef)}][{missionActivate.PopulationObjects.Length}]");
                var metaStateEvent = GetMetaStateEvent(metaStateRef);
                var spawnLocation = new SpawnLocation(Region, missionActivate.PopulationAreaRestriction, null);
                metaStateEvent.AddRequiredObjects(missionActivate.PopulationObjects, spawnLocation);
                metaStateEvent.Schedule();
            }
            else if (metastate is MetaStateMissionSequencerPrototype missionSequencer)
            {
                if (missionSequencer.Sequence.HasValue())
                    foreach (var missionEntry in missionSequencer.Sequence)
                    {
                        var metaStateRef = metastate.DataRef;
                        Logger.Debug($"State [{GameDatabase.GetFormattedPrototypeName(metaStateRef)}][{missionEntry.PopulationObjects.Length}]");
                        var metaStateEvent = GetMetaStateEvent(metaStateRef);
                        var spawnLocation = new SpawnLocation(Region, missionEntry.PopulationAreaRestriction, null);
                        metaStateEvent.AddRequiredObjects(missionEntry.PopulationObjects, spawnLocation);
                        metaStateEvent.Schedule();
                    }
            }
            else if (metastate is MetaStateWaveInstancePrototype waveInstance)
            {
                if (waveInstance.States.HasValue())
                    foreach (var state in waveInstance.States)
                        MetaStateRegisty(state);
            }
            else if (metastate is MetaStatePopulationMaintainPrototype popProto && popProto.PopulationObjects.HasValue())
            {
                var metaStateRef = popProto.DataRef;
                Logger.Debug($"State [{GameDatabase.GetFormattedPrototypeName(metaStateRef)}][{popProto.PopulationObjects.Length}]");
                var areas = popProto.RestrictToAreas;
                if (popProto.DataRef == (PrototypeId)7730041682554854878 && _regionId == (ulong)RegionPrototypeId.CH0402UpperEastRegion) areas = null; // Hack for Moloids
                var metaStateEvent = GetMetaStateEvent(metaStateRef);
                var spawnLocation = new SpawnLocation(Region, areas, popProto.RestrictToCells);
                metaStateEvent.AddRequiredObjects(popProto.PopulationObjects, spawnLocation);
                metaStateEvent.Schedule();
            }
        }

        // TODO event registry States
        public void RegisterStates()
        {
            Region region = Region;           
            if (region == null) return;
            if (Prototype is not MetaGamePrototype metaGameProto) return;
            
            if (metaGameProto.GameModes.HasValue())
            {
                var gameMode = metaGameProto.GameModes.First().As<MetaGameModePrototype>();
                if (gameMode == null) return;

                if (gameMode.ApplyStates.HasValue())
                    foreach(var state in gameMode.ApplyStates)
                        MetaStateRegisty(state);

                if (region.PrototypeDataRef == (PrototypeId)RegionPrototypeId.HoloSimARegion1to60) // Hardcode for Holo-Sim
                {
                    MetaGameStateModePrototype stateMode = gameMode as MetaGameStateModePrototype;
                    int wave = Game.Random.Next(0, stateMode.States.Length);
                    MetaStateRegisty(stateMode.States[wave]);
                } 
                else if (region.PrototypeDataRef == (PrototypeId)RegionPrototypeId.LimboRegionL60) // Hardcode for Limbo
                {
                    MetaGameStateModePrototype stateMode = gameMode as MetaGameStateModePrototype;
                    MetaStateRegisty(stateMode.States[0]);
                }
                else if (region.PrototypeDataRef == (PrototypeId)RegionPrototypeId.CH0402UpperEastRegion) // Hack for Moloids
                {
                    MetaStateRegisty((PrototypeId)7730041682554854878); // CH04UpperMoloids
                }
                else if (region.PrototypeDataRef == (PrototypeId)RegionPrototypeId.SurturRaidRegionGreen) // Hardcode for Surtur
                {   
                    var stateRef = (PrototypeId)5463286934959496963; // SurturMissionProgressionStateFiveMan
                    var missionProgression = stateRef.As<MetaStateMissionProgressionPrototype>();
                    foreach(var state in missionProgression.StatesProgression)
                        MetaStateRegisty(state);
                }
            }
        }

        #endregion

        #region Player

        private void OnPlayerRegionChange(PlayerRegionChangeGameEvent evt)
        {
            var player = evt.Player;
            if (player == null) return;
            InitializePlayer(player);
        }

        public void OnRemovedPlayer(Player player)
        {
            RemovePlayer(player);

            var manager = Game.EntityManager;
            if (manager == null) return;

            foreach (ulong entityId in _discoveredEntities)
            {
                var discoveredEntity = manager.GetEntity<WorldEntity>(entityId);
                if (discoveredEntity != null)
                    player.UndiscoverEntity(discoveredEntity, true);
            }

            foreach (MetaState state in MetaStates)
                state.OnRemovedPlayer(player);
        }

        private void OnDestroyEntity(Entity entity)
        {
            if (entity is Player player)
                RemovePlayer(player);
        }

        private void OnPlayerEnteredRegion(PlayerEnteredRegionGameEvent evt)
        {
            var player = evt.Player;
            if (player == null) return;
            AddPlayer(player);
        }

        public bool InitializePlayer(Player player)
        {
            var team = GetTeamByPlayer(player);
            // TODO crate team?
            team?.AddPlayer(player);

            return true;
        }

        public bool UpdatePlayer(Player player, MetaGameTeam team)
        {
            if (player == null || team == null) return false;
            MetaGameTeam oldTeam = GetTeamByPlayer(player);
            if (oldTeam != team)
                return oldTeam.RemovePlayer(player) && team.AddPlayer(player);
            return false;
        }

        private MetaGameTeam GetTeamByPlayer(Player player)
        {
            foreach (var team in Teams)
                if (team.Contains(player)) return team;
            return null;
        }

        public bool AddPlayer(Player player)
        {
            // TODO add in chat

            return true;
        }

        public bool RemovePlayer(Player player)
        {
            if (player == null) return false;

            // TODO remove from chat

            // remove from teams
            foreach (var team in Teams)
                if (team.RemovePlayer(player))
                    return true;

            return false;
        }

        #endregion

        #region Discover

        private void OnEntityEnteredWorld(EntityEnteredWorldGameEvent evt)
        {
            if (MetaGamePrototype?.DiscoverAvatarsForPlayers == true)
            {
                var entity = evt.Entity;
                if (entity is Avatar) DiscoverEntity(entity);
            }
        }

        private void DiscoverEntity(WorldEntity entity)
        {
            if (entity.IsDiscoverable && _discoveredEntities.Contains(entity.Id) == false)
            {
                _discoveredEntities.Add(entity.Id);
                DiscoverEntityForPlayers(entity);
            }
        }

        private void DiscoverEntityForPlayers(WorldEntity entity)
        {
            foreach (var player in new PlayerIterator(Game))
                player.DiscoverEntity(entity, true);
        }

        private void OnEntityExitedWorld(EntityExitedWorldGameEvent evt)
        {
            var entity = evt.Entity;
            if (entity != null) UniscoverEntity(entity);
        }

        public void UniscoverEntity(WorldEntity entity)
        {
            if (entity.IsDiscoverable && _discoveredEntities.Contains(entity.Id))
            {
                _discoveredEntities.Remove(entity.Id);
                UndiscoverEntityForPlayers(entity);
            }
        }

        private void UndiscoverEntityForPlayers(WorldEntity entity)
        {
            foreach (var player in new PlayerIterator(Game))
                player.UndiscoverEntity(entity, true);
        }

        public void ConsiderInAOI(AreaOfInterest aoi)
        {
            aoi.ConsiderEntity(this);
            foreach (var player in new PlayerIterator(Region))
                aoi.ConsiderEntity(player);
        }

        #endregion

    }
}
