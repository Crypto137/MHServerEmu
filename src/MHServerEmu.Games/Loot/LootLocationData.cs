using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot
{
    public class LootLocationData : IPoolable, IDisposable
    {
        // NOTE: This doesn't work properly with random locations, we need to implement some kind
        // of distribution system that gradually fills space from min radius to max to make use of this.

        private const float DefaultBoundsRadius = 10f;

        public Bounds Bounds { get; } = new();
        public Game Game { get; private set; }
        public Vector3 Position { get; private set; }
        public WorldEntity SourceEntity { get; set; }
        public float MinRadius { get; set; }
        public float MaxRadius { get; set; }
        public bool DropInPlace { get; set; }
        public Vector3 Offset { get; set; }

        public void Initialize(Game game, Vector3 position, WorldEntity sourceEntity = null)
        {
            Game = game;
            Position = position;
            SourceEntity = sourceEntity;

            Bounds.InitializeSphere(DefaultBoundsRadius, BoundsCollisionType.Overlapping);
            Bounds.Center = Position;
        }

        public void ResetForPool()
        {
            // Bounds are reset during initialization
            Position = default;
            SourceEntity = default;
            MinRadius = default;
            MaxRadius = default;
            DropInPlace = default;
            Offset = default;
        }

        public void Dispose()
        {
            ObjectPoolManager.Instance.Return(this);
        }
    }
}
