using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public class PhysicsManager
    {
        public int CurrentForceReadIndex => _currentForceReadWriteState ? 1 : 0;
        public int CurrentForceWriteIndex => _currentForceReadWriteState ? 0 : 1;

        private Game _game { get; }
        private List<ForceSystem> _pendingForceSystems { get; }
        private List<ForceSystem> _activeForceSystems { get; }
        private Queue<OverlapEvent> _overlapEvents { get; }
        private List<ulong> _entitiesPendingResolve { get; }
        private List<ulong> _entitiesResolving { get; }
        private int _physicsFrames;
        private bool _currentForceReadWriteState;

        public PhysicsManager(Game game)
        {
            _game = game;
            _pendingForceSystems = new();
            _activeForceSystems = new();
            _overlapEvents = new();
            _entitiesPendingResolve = new();
            _entitiesResolving = new();
            _currentForceReadWriteState = false;
            _physicsFrames = 1;
        }

        public void ResolveEntities()
        {
            if (_game == null || _entitiesResolving.Count > 0) return;

            _entitiesResolving.Clear();
            _entitiesResolving.AddRange(_entitiesPendingResolve);
            _entitiesPendingResolve.Clear();
            _physicsFrames++;

            SwapCurrentForceReadWriteIndices();
            ApplyForceSystems();
            PhysicsContext physicsContext = new();
            ResolveEntitiesAllowPenetration(physicsContext, _entitiesResolving);
            ResolveEntitiesOverlapState(physicsContext);

            _entitiesResolving.Clear();

            foreach (Region region in _game.RegionIterator())
                region.ClearCollidedEntities();
        }

        private void ResolveEntitiesOverlapState(PhysicsContext physicsContext)
        {
            throw new NotImplementedException();
        }

        private void ResolveEntitiesAllowPenetration(PhysicsContext physicsContext, List<ulong> entitiesResolving)
        {
            throw new NotImplementedException();
        }

        private void ApplyForceSystems()
        {
            _activeForceSystems.AddRange(_pendingForceSystems);
            _pendingForceSystems.Clear();

            for (int i = _activeForceSystems.Count - 1; i >= 0; i--)
                if (ApplyForceSystemCheckCompletion(_activeForceSystems[i]))
                    _activeForceSystems.RemoveAt(i);
        }

        private bool ApplyForceSystemCheckCompletion(ForceSystem forceSystem)
        {
            throw new NotImplementedException();
        }

        private void SwapCurrentForceReadWriteIndices()
        {
            _currentForceReadWriteState = !_currentForceReadWriteState;
        }
    }

    public class PhysicsContext 
    {

    }

    public class ForceSystem
    {

    }

    public class OverlapEvent
    {

    }
}
