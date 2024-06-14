using MHServerEmu.Games.Entities.Avatars;

namespace MHServerEmu.Games.Entities
{
    public class EntityDesc
    {
        public ulong EntityId { get; private set; }
        public string PlayerName { get; private set; }
        public bool IsValid  { get => IsLocalValid || IsRemoteValid; }
        public bool IsLocalValid { get => EntityId != Entity.InvalidId; }
        public bool IsRemoteValid { get => string.IsNullOrEmpty(PlayerName) == false; }

        public EntityDesc(Game game, ulong entityId, string playerName)
        {
            EntityId = entityId;
            PlayerName = playerName;
            if (PlayerName == string.Empty)
                PlayerName = GetNameFromEntity(game);
        }

        public EntityDesc(Entity entity)
        {
            EntityId = entity.Id;
            PlayerName = GetNameFromEntity(entity);
        }

        public void Clear()
        {
            EntityId = Entity.InvalidId;
            PlayerName = string.Empty;
        }

        private string GetNameFromEntity(Game game)
        {
            Entity entity = GetEntity<Entity>(game);
            if (entity == null)
                return string.Empty;

            return GetNameFromEntity(entity);
        }

        public bool Equals(string other)
        {
            return other == PlayerName;
        }

        public T GetEntity<T>(Game game) where T: Entity
        {
            if (game == null || EntityId == Entity.InvalidId) return default;
            return game.EntityManager.GetEntity<T>(EntityId);
        }

        private static string GetNameFromEntity(Entity entity)
        {
            if (entity is Avatar avatar)
                return avatar.PlayerName;

            if (entity is Player player)
                return player.GetName();

            return string.Empty;
        }
    }
}
