using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.UI.Widgets;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateTrackRegionScore : MetaState, IPropertyChangeWatcher
    {
        private MetaStateTrackRegionScorePrototype _proto;
        private Event<EntityDeadGameEvent>.Action _entityDeadAction;
        private Event<OrbPickUpEvent>.Action _orbPickUpAction;
        private PropertyCollection _properties;
        private Curve _scoreLevelCurve;
        private Curve _scoreRankCurve;
        private int _scoreBoost;

        public MetaStateTrackRegionScore(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateTrackRegionScorePrototype;

            _orbPickUpAction = OnOrbPickUp;
            _entityDeadAction = OnEntityDead;

            _scoreLevelCurve = GameDatabase.GetCurve(_proto.ScoreCurveForMobLevel);
            _scoreRankCurve = GameDatabase.GetCurve(_proto.ScoreCurveForMobRank);

            _scoreBoost = 1;
            if (_proto.ScoreThreshold == 30000) _scoreBoost = 3; // BoostFix for AgeOfUltronTrackRegionScore
        }

        public override void OnApply()
        {
            var region = Region;
            if (region == null || _proto.ScoreThreshold < 0) return;

            region.EntityDeadEvent.AddActionBack(_entityDeadAction);
            region.OrbPickUpEvent.AddActionBack(_orbPickUpAction);

            region.Properties[PropertyEnum.TrackedRegionScore] = 0.0f;
            Attach(region.Properties);

            UpdateWidget(null);
        }

        public override void OnReset()
        {
            var region = Region;
            if (region == null) return;

            region.Properties[PropertyEnum.TrackedRegionScore] = 0.0f;
            UpdateWidget(null);
        }

        public override void OnAddPlayer(Player player)
        {
            if (player != null) OnUpdatePlayerNotification(player);
        }

        public override void OnUpdatePlayerNotification(Player player)
        {
            var region = Region;
            if (region == null) return;

            var mode = MetaGame.CurrentMode;
            if (mode == null) return;

            if (_proto.ScoreThreshold > 0)
                UpdateWidget(player);
        }

        private void UpdateWidget(Player player)
        {
            var region = Region;
            if (region == null) return;

            var mode = MetaGame.CurrentMode;
            if (mode == null) return;

            int score = (int)(float)region.Properties[PropertyEnum.TrackedRegionScore];
            mode.SendPvEInstanceRegionScoreUpdate(score, player);

            if (score > _proto.ScoreThreshold) 
                score = _proto.ScoreThreshold;

            var widget = MetaGame.GetWidget<UIWidgetGenericFraction>(_proto.UIWidget);
            widget?.SetCount(score, _proto.ScoreThreshold);
        }

        public override void OnRemove()
        {
            var region = Region;
            if (region == null) return;

            region.EntityDeadEvent.RemoveAction(_entityDeadAction);
            region.OrbPickUpEvent.RemoveAction(_orbPickUpAction);

            MetaGame.DeleteWidget(_proto.UIWidget);

            Detach(true);

            base.OnRemove();
        }

        private void OnOrbPickUp(in OrbPickUpEvent evt)
        {
            var player = evt.Player;
            var orb = evt.Orb;
            if (orb == null) return;

            var region = Region;
            if (region == null) return;

            var mode = MetaGame.CurrentMode;
            if (mode == null) return;

            int scoreRangeLow = orb.Properties[PropertyEnum.TrackedRegionScoreRangeLow];
            int scoreRangeHigh = orb.Properties[PropertyEnum.TrackedRegionScoreRangeHigh];

            scoreRangeLow = Math.Min(scoreRangeLow, scoreRangeHigh);
            scoreRangeHigh = Math.Max(scoreRangeLow, scoreRangeHigh);

            float score = scoreRangeHigh;
            if (scoreRangeLow > -1)
                score = Game.Random.Next(scoreRangeLow, scoreRangeHigh + 1);

            if (score > -1.0f)
            {
                if (orb.Properties.HasProperty(PropertyEnum.TrackedRegionScoreDivByParty))
                {
                    var party = player?.GetParty();
                    if (party != null)
                    {
                        int numMembers = party.NumMembers;
                        if (numMembers > 0)
                            score /= numMembers;
                    }
                }

                score *= _scoreBoost;
                region.Properties.AdjustProperty(score, PropertyEnum.TrackedRegionScore);
            }
        }

        private void OnEntityDead(in EntityDeadGameEvent evt)
        {
            var entity = evt.Defender;
            if (entity == null) return;

            var region = Region;
            if (region == null) return;

            float score = 0.0f;

            if (entity.IsHostileToPlayers() && entity.TagPlayers.HasTags)
            {
                if (_scoreLevelCurve != null)
                {
                    var level = entity.CharacterLevel;
                    if (_scoreLevelCurve.IndexInRange(level))
                        score += _scoreLevelCurve.GetAt(level);                    
                }

                if (_scoreRankCurve != null)
                {
                    var rankProto = entity.GetRankPrototype();
                    if (rankProto != null)
                    {
                        int rank = (int)rankProto.Rank;
                        if (_scoreRankCurve.IndexInRange(rank))
                            score += _scoreRankCurve.GetAt(rank);
                    }
                }

                var spawnGroup = entity.SpawnGroup;
                if (spawnGroup != null)
                {
                    var filter = SpawnGroupEntityQueryFilterFlags.Hostiles | SpawnGroupEntityQueryFilterFlags.NotDeadDestroyedControlled;
                    if (spawnGroup.FilterEntity(filter, entity, null, default) == false)
                    {
                        if (spawnGroup.SpawnerId != Entity.InvalidId && entity.Properties.HasProperty(PropertyEnum.ParentSpawnerGroupId))
                        {
                            var popManager = region.PopulationManager;
                            if (popManager == null) return;

                            var spawnerGroup = popManager.GetSpawnGroup(entity.Properties[PropertyEnum.ParentSpawnerGroupId]);
                            bool canScore = spawnerGroup != null && spawnerGroup.RegionScored == false;
                            if (canScore && spawnGroup.FilterEntity(filter, null, null, default) == false)
                            {
                                spawnerGroup.RegionScored = true;
                                var spawnerObjectProto = spawnerGroup.ObjectProto;
                                if (spawnerObjectProto != null && spawnerObjectProto.GameModeScoreValue > 0)
                                    score += spawnerObjectProto.GameModeScoreValue;
                            }
                        }

                        var objectProto = spawnGroup.ObjectProto;
                        if (objectProto != null && objectProto.GameModeScoreValue > 0)
                            score += objectProto.GameModeScoreValue;
                    }
                }
            }

            score *= _scoreBoost;
            if (score > 0.0f) 
                region.Properties.AdjustProperty(score, PropertyEnum.TrackedRegionScore);
        }

        public void Attach(PropertyCollection propertyCollection)
        {
            if (_properties != null && _properties == propertyCollection) return;
            _properties = propertyCollection;
            _properties.AttachWatcher(this);
        }

        public void Detach(bool removeFromAttachedCollection)
        {
            if (removeFromAttachedCollection)
                _properties?.DetachWatcher(this);
        }

        public void OnPropertyChange(PropertyId id, PropertyValue newValue, PropertyValue oldValue, SetPropertyFlags flags)
        {
            if (id.Enum != PropertyEnum.TrackedRegionScore) return;

            var region = Region;
            if (region == null) return;

            var mode = MetaGame.CurrentMode;
            if (mode == null) return;

            int score = (int)(float)region.Properties[PropertyEnum.TrackedRegionScore];

            if (score >= _proto.ScoreThreshold)
                OnScoreThreshold(region.MissionManager);
            else
                UpdateWidget(null);
        }

        private void OnScoreThreshold(MissionManager missionManager)
        {
            if (missionManager == null) return;

            if (_proto.MissionsToComplete.HasValue())
                foreach(var missionRef in _proto.MissionsToComplete)
                {
                    if (missionRef == PrototypeId.Invalid) continue;
                    var mission = missionManager.MissionByDataRef(missionRef);
                    if (mission == null || mission.IsOpenMission == false) continue;
                    mission.SetState(MissionState.Completed);
                }

            if (_proto.MissionsToFail.HasValue())
                foreach (var missionRef in _proto.MissionsToFail)
                {
                    if (missionRef == PrototypeId.Invalid) continue;
                    var mission = missionManager.MissionByDataRef(missionRef);
                    if (mission == null || mission.IsOpenMission == false) continue;
                    mission.SetState(MissionState.Failed);
                }

            if (_proto.MissionsToDeactivate.HasValue())
                foreach (var missionRef in _proto.MissionsToDeactivate)
                {
                    if (missionRef == PrototypeId.Invalid) continue;
                    var mission = missionManager.MissionByDataRef(missionRef);
                    if (mission == null || mission.IsOpenMission == false) continue;
                    mission.SetState(MissionState.Inactive);
                }

            if (_proto.OnScoreThresholdApplyStates.HasValue())
                MetaGame.ApplyStates(_proto.OnScoreThresholdApplyStates);

            if (_proto.OnScoreThresholdRemoveStates.HasValue())
                MetaGame.RemoveStates(_proto.OnScoreThresholdRemoveStates);

            if (_proto.NextMode >= 0)
                MetaGame.ScheduleActivateGameMode(_proto.NextMode);

            if (_proto.RemoveStateOnScoreThreshold)
                MetaGame.RemoveState(PrototypeDataRef);
        }
    }
}
