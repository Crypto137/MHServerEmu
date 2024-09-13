using System.Text;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
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
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.MetaGames
{
    public class MetaGame : Entity
    {
        public static readonly Logger Logger = LogManager.CreateLogger();

        protected RepString _name = new();
        protected ulong _regionId;

        public Region Region { get => GetRegion(); }
        public MetaGamePrototype MetaGamePrototype { get => Prototype as MetaGamePrototype; }
        public List<MetaState> MetaStates { get; } = new();
        protected List<MetaGameTeam> Teams { get; } = new();
        protected List<MetaGameMode> GameModes { get; } = new();

        private HashSet<ulong> _discoveredEntities = new();

        private Dictionary<PrototypeId, MetaStateSpawnEvent> _metaStateSpawnMap;
        private Action<PlayerEnteredRegionGameEvent> _playerEnteredRegionAction;
        private Action<EntityEnteredWorldGameEvent> _entityEnteredWorldAction;
        private Action<EntityExitedWorldGameEvent> _entityExitedWorldAction;
        private Action<PlayerRegionChangeGameEvent> _playerRegionChangeAction;
        private Action<Entity> _destroyEntityAction;

        public MetaGame(Game game) : base(game) 
        {
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
            // TODO
        }

        public void ApplyMetaState(PrototypeId stateRef)
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
