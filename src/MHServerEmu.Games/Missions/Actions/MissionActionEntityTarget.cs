using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Dialog;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionEntityTarget : MissionAction
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private HashSet<ulong> _completedEntities;
        
        public MissionActionEntityTarget(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype) { }

        public override void Run()
        {
            var entityTracker = EntityTracker;
            if (entityTracker == null) return;
            var contextRef = Context;
            if (contextRef == PrototypeId.Invalid) return;
            foreach (var entity in entityTracker.Iterate(contextRef, EntityTrackingFlag.MissionAction | EntityTrackingFlag.SpawnedByMission))
                if (Evaluate(entity)) EvaluateAndRunEntity(entity);

        }

        public virtual void EvaluateAndRunEntity(WorldEntity entity)
        {
            if (entity == null) return;
            if (_completedEntities != null && _completedEntities.Contains(entity.Id)) return;

            if (Evaluate(entity) && RunEntity(entity))
            {
                _completedEntities ??= new(); 
                _completedEntities.Add(entity.Id);
            }
        }

        public virtual bool Evaluate(WorldEntity entity)
        {
            if (entity == null || entity.IsDestroyed) return false;
            if (Prototype is not MissionActionEntityTargetPrototype targetProto) return false;
            if (targetProto.AllowWhenDead == false && entity.IsDead) return false;
            if (targetProto.EntityFilter != null && targetProto.EntityFilter.Evaluate(entity, new(MissionRef)) == false) return false;
            return true;
        }

        public virtual bool RunEntity(WorldEntity entity) => true;

        public override bool RunOnStart() => true;
    }
}
