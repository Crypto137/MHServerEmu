using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot
{
    public class LootLocationData : IPoolable, IDisposable
    {
        private const float DefaultBoundsRadius = 10f;

        public Bounds Bounds { get; } = new();
        public Game Game { get; private set; }
        public Vector3 Position { get; private set; }
        public WorldEntity Recipient { get; set; }
        public float MinRadius { get; set; }
        public float MaxRadius { get; set; }
        public bool DropInPlace { get; set; }
        public Vector3 Offset { get; set; }

        public bool IsInPool { get; set; }

        public void Initialize(Game game, Vector3 position, WorldEntity recipient = null)
        {
            Game = game;
            Position = position;
            Recipient = recipient;

            Bounds.InitializeSphere(DefaultBoundsRadius, BoundsCollisionType.Overlapping);
            Bounds.Center = Position;
        }

        public void ResetForPool()
        {
            // Bounds are reset during initialization
            Position = default;
            Recipient = default;
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
