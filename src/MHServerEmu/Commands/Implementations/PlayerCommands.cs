using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Powers.Conditions;
using MHServerEmu.Games.Properties;
using MHServerEmu.Grouping;
using MHServerEmu.PlayerManagement;
using MHServerEmu.Core.VectorMath;
using System.Linq;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("player")]
    [CommandGroupDescription("Commands for managing player data for the invoker's account.")]
    public class PlayerCommands : CommandGroup
    {
        [Command("costume")]
        [CommandDescription("Changes costume for the current avatar.")]
        [CommandUsage("player costume [name|reset|default]")]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string Costume(string[] @params, NetClient client)
        {
            PrototypeId costumeProtoRef;

            switch (@params[0].ToLower())
            {
                case "reset":
                    costumeProtoRef = PrototypeId.Invalid;
                    break;

                case "default": // This undoes visual updates for most heroes
                    costumeProtoRef = (PrototypeId)HardcodedBlueprints.Costume;
                    break;

                default:
                    var matches = GameDatabase.SearchPrototypes(@params[0], DataFileSearchFlags.SortMatchesByName | DataFileSearchFlags.CaseInsensitive, HardcodedBlueprints.Costume);

                    if (matches.Any() == false)
                        return $"Failed to find any costumes containing {@params[0]}.";

                    if (matches.Count() > 1)
                    {
                        CommandHelper.SendMessage(client, $"Found multiple matches for {@params[0]}:");
                        CommandHelper.SendMessages(client, matches.Select(match => GameDatabase.GetPrototypeName(match)), false);
                        return string.Empty;
                    }

                    costumeProtoRef = matches.First();
                    break;
            }

            PlayerConnection playerConnection = (PlayerConnection)client;
            var player = playerConnection.Player;
            var avatar = player.CurrentAvatar;

            avatar.ChangeCostume(costumeProtoRef);

            if (costumeProtoRef == PrototypeId.Invalid)
                return "Resetting costume.";

            return $"Changing costume to {GameDatabase.GetPrototypeName(costumeProtoRef)}.";
        }

        [Command("wipe")]
        [CommandDescription("Wipes all progress associated with the current account.")]
        [CommandUsage("player wipe [playerName]")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Wipe(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            string playerName = playerConnection.Player.GetName();

            if (@params.Length == 0)
                return $"Type '!player wipe {playerName}' to wipe all progress associated with this account.\nWARNING: THIS ACTION CANNOT BE REVERTED.";

            if (string.Equals(playerName, @params[0], StringComparison.OrdinalIgnoreCase) == false)
                return "Incorrect player name.";

            playerConnection.WipePlayerData();
            return string.Empty;
        }

        [Command("givecurrency")]
        [CommandDescription("Gives all currencies.")]
        [CommandUsage("player givecurrency [amount]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string GiveCurrency(string[] @params, NetClient client)
        {
            if (int.TryParse(@params[0], out int amount) == false)
                return $"Failed to parse amount from {@params[0]}.";

            PlayerConnection playerConnection = (PlayerConnection)client;
            Player player = playerConnection.Player;

            foreach (PrototypeId currencyProtoRef in DataDirectory.Instance.IteratePrototypesInHierarchy<CurrencyPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
                player.Properties.AdjustProperty(amount, new(PropertyEnum.Currency, currencyProtoRef));

            return $"Successfully given {amount} of all currencies.";
        }
        [Command("killplayer")]
        [CommandDescription("Instantly kills the specified player's avatar.")]
        [CommandUsage("player killplayer [playerName]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string KillPlayer(string[] @params, NetClient client)
        {
            string playerName = @params[0];

            PlayerConnection adminConnection = (PlayerConnection)client;
            if (adminConnection == null) return "Error: Could not get admin player connection.";

            WorldEntity killer = adminConnection.Player?.CurrentAvatar;

            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
            if (playerManager == null) return "Error: PlayerManagerService is not available.";

            var groupingManager = ServerManager.Instance.GetGameService(ServerType.GroupingManager) as GroupingManagerService;
            if (groupingManager == null) return "Error: GroupingManagerService is not available.";

            if (!groupingManager.TryGetClient(playerName, out IFrontendClient targetFrontendClient))
            {
                return $"Player '{playerName}' not found or is not online.";
            }

            if (!(targetFrontendClient is FrontendClient targetGameClient))
            {
                return $"Player '{playerName}' is not a recognized game client type.";
            }

            if (targetGameClient.GameId == 0)
            {
                return $"Player '{playerName}' is not currently associated with a game world.";
            }

            Game targetPlayerGame = playerManager.GetGameByPlayer(targetGameClient);
            if (targetPlayerGame == null)
            {
                return $"Could not find the game instance for player '{playerName}'.";
            }

            if (targetGameClient.Session == null || targetGameClient.Session.Account == null)
            {
                return $"Player '{playerName}' session or account information is missing.";
            }
            ulong targetAccountDbId = (ulong)targetGameClient.Session.Account.Id;
            // MODIFIED: Use IterateEntities() instead of GetAllEntities()
            Player targetPlayer = targetPlayerGame.EntityManager.IterateEntities().OfType<Player>().FirstOrDefault(p => p.DatabaseUniqueId == targetAccountDbId);

            if (targetPlayer == null)
            {
                return $"Could not find player entity for '{playerName}' (Account ID: {targetAccountDbId}) in their game instance.";
            }

            Avatar targetAvatar = targetPlayer.CurrentAvatar;
            if (targetAvatar == null || !targetAvatar.IsInWorld)
            {
                return $"Player '{playerName}' does not have an active avatar in the world to kill.";
            }

            if (targetAvatar.IsDead)
            {
                return $"Player '{playerName}' is already dead.";
            }

            targetAvatar.Kill(killer, KillFlags.None);

            string invokerMessage = $"Successfully killed player '{playerName}'.";
            ChatHelper.SendMetagameMessage(targetGameClient, $"You have been slain by an administrator.");
            return invokerMessage;
        }
        [Command("boostdamage")]
        [CommandDescription("Sets DamagePctBonus for the specified player's current avatar.")]
        [CommandUsage("player boostdamage [playerName] [value]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string BoostPlayerDamage(string[] @params, NetClient client)
        {
            string playerName = @params[0];
            int damageValue = 1000;

            if (@params.Length > 1)
            {
                if (!int.TryParse(@params[1], out damageValue))
                {
                    return "Invalid damage value. Please provide a number.";
                }
            }

            damageValue = Math.Clamp(damageValue, 1, 10000);

            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
            if (playerManager == null) return "Error: PlayerManagerService is not available.";
            var groupingManager = ServerManager.Instance.GetGameService(ServerType.GroupingManager) as GroupingManagerService;
            if (groupingManager == null) return "Error: GroupingManagerService is not available.";

            if (!groupingManager.TryGetClient(playerName, out IFrontendClient targetFrontendClient))
            {
                return $"Player '{playerName}' not found or is not online.";
            }
            if (!(targetFrontendClient is FrontendClient targetGameClient))
            {
                return $"Player '{playerName}' is not a recognized game client type.";
            }
            if (targetGameClient.GameId == 0)
            {
                return $"Player '{playerName}' is not currently associated with a game world.";
            }
            Game targetPlayerGame = playerManager.GetGameByPlayer(targetGameClient);
            if (targetPlayerGame == null)
            {
                return $"Could not find the game instance for player '{playerName}'.";
            }
            if (targetGameClient.Session == null || targetGameClient.Session.Account == null)
            {
                return $"Player '{playerName}' session or account information is missing.";
            }
            ulong targetAccountDbId = (ulong)targetGameClient.Session.Account.Id;
            Player targetPlayer = targetPlayerGame.EntityManager.IterateEntities().OfType<Player>().FirstOrDefault(p => p.DatabaseUniqueId == targetAccountDbId);
            if (targetPlayer == null)
            {
                return $"Could not find player entity for '{playerName}'.";
            }
            Avatar targetAvatar = targetPlayer.CurrentAvatar;
            if (targetAvatar == null || !targetAvatar.IsInWorld)
            {
                return $"Player '{playerName}' does not have an active avatar in the world to boost.";
            }

            targetAvatar.Properties[PropertyEnum.DamagePctBonus] = (float)damageValue;

            string invokerMessage = $"Applied DamagePctBonus x{damageValue} to player '{playerName}'.";
            ChatHelper.SendMetagameMessage(targetGameClient, $"Your damage has been boosted by an administrator (x{damageValue})!");
            return invokerMessage;
        }

        [Command("boostvsboss")]
        [CommandDescription("Sets DamagePctBonusVsBosses for the specified player's current avatar.")]
        [CommandUsage("player boostvsboss [playerName] [value]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string BoostPlayerVsBossDamage(string[] @params, NetClient client)
        {
            string playerName = @params[0];
            int vsBossValue = 1000;

            if (@params.Length > 1)
            {
                if (!int.TryParse(@params[1], out vsBossValue))
                {
                    return "Invalid damage vs boss value. Please provide a number.";
                }
            }

            vsBossValue = Math.Clamp(vsBossValue, 1, 10000);

            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
            if (playerManager == null) return "Error: PlayerManagerService is not available.";
            var groupingManager = ServerManager.Instance.GetGameService(ServerType.GroupingManager) as GroupingManagerService;
            if (groupingManager == null) return "Error: GroupingManagerService is not available.";

            if (!groupingManager.TryGetClient(playerName, out IFrontendClient targetFrontendClient))
            {
                return $"Player '{playerName}' not found or is not online.";
            }
            if (!(targetFrontendClient is FrontendClient targetGameClient))
            {
                return $"Player '{playerName}' is not a recognized game client type.";
            }
            if (targetGameClient.GameId == 0)
            {
                return $"Player '{playerName}' is not currently associated with a game world.";
            }
            Game targetPlayerGame = playerManager.GetGameByPlayer(targetGameClient);
            if (targetPlayerGame == null)
            {
                return $"Could not find the game instance for player '{playerName}'.";
            }
            if (targetGameClient.Session == null || targetGameClient.Session.Account == null)
            {
                return $"Player '{playerName}' session or account information is missing.";
            }
            ulong targetAccountDbId = (ulong)targetGameClient.Session.Account.Id;
            Player targetPlayer = targetPlayerGame.EntityManager.IterateEntities().OfType<Player>().FirstOrDefault(p => p.DatabaseUniqueId == targetAccountDbId);
            if (targetPlayer == null)
            {
                return $"Could not find player entity for '{playerName}'.";
            }
            Avatar targetAvatar = targetPlayer.CurrentAvatar;
            if (targetAvatar == null || !targetAvatar.IsInWorld)
            {
                return $"Player '{playerName}' does not have an active avatar in the world to boost.";
            }

            targetAvatar.Properties[PropertyEnum.DamagePctBonusVsBosses] = (float)vsBossValue;

            string invokerMessage = $"Applied DamagePctBonusVsBosses x{vsBossValue} to player '{playerName}'.";
            ChatHelper.SendMetagameMessage(targetGameClient, $"Your damage vs bosses has been boosted by an administrator (x{vsBossValue})!");
            return invokerMessage;
        }

        // --- NEW Kill Entity Command ---
        [Command("killentity")]
        [CommandDescription("Instantly kills the specified entity by its ID.")]
        [CommandUsage("player killentity [entityId]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)] // Admin invokes this
        [CommandParamCount(1)] // Requires entityId
        public string KillEntity(string[] @params, NetClient client)
        {
            if (!ulong.TryParse(@params[0], out ulong entityId))
            {
                return "Invalid Entity ID. Please provide a number.";
            }

            PlayerConnection adminConnection = (PlayerConnection)client;
            if (adminConnection == null) return "Error: Could not get admin player connection.";

            Player adminPlayer = adminConnection.Player;
            if (adminPlayer == null) return "Error: Could not get admin player entity.";

            Game currentGame = adminPlayer.Game;
            if (currentGame == null) return "Error: Admin is not currently in a game instance.";

            // The killer will be the admin's avatar, or null if no avatar is active.
            WorldEntity killer = adminPlayer.CurrentAvatar;

            // Find the target entity in the admin's current game instance
            Entity entityToKill = currentGame.EntityManager.GetEntity<Entity>(entityId);

            if (entityToKill == null)
            {
                return $"Entity with ID '{entityId}' not found in your current game instance.";
            }

            if (!(entityToKill is WorldEntity targetWorldEntity))
            {
                return $"Entity with ID '{entityId}' is not a WorldEntity and cannot be killed in this manner.";
            }

            if (!targetWorldEntity.IsInWorld)
            {
                return $"Entity '{GameDatabase.GetPrototypeName(targetWorldEntity.PrototypeDataRef)}' (ID: {entityId}) is not currently in the world.";
            }

            if (targetWorldEntity.IsDead)
            {
                return $"Entity '{GameDatabase.GetPrototypeName(targetWorldEntity.PrototypeDataRef)}' (ID: {entityId}) is already dead.";
            }

            // Kill the target entity
            // The Kill method is on Agent.cs, which WorldEntity might not directly have.
            // If it's an Agent (like NPCs, enemies), this will work.
            // For other WorldEntity types, you might need a more generic "Destroy" or similar.
            if (targetWorldEntity is Agent targetAgent)
            {
                targetAgent.Kill(killer, KillFlags.None);
                return $"Successfully killed entity '{GameDatabase.GetPrototypeName(targetAgent.PrototypeDataRef)}' (ID: {entityId}).";
            }
            else
            {
                // For non-Agent WorldEntities, a more generic Destroy might be appropriate
                // or you might decide this command only works on Agents.
                // For now, let's attempt a generic Destroy if it's not an Agent.
                // Note: Destroy() might behave differently than Kill() (e.g., no loot, no death animations).
                targetWorldEntity.Destroy(); // This is a method on the base Entity class.
                return $"Attempted to destroy entity '{GameDatabase.GetPrototypeName(targetWorldEntity.PrototypeDataRef)}' (ID: {entityId}). Non-Agent entities are destroyed, not 'killed'.";
            }

        }
            [Command("clearconditions")]
        [CommandDescription("Clears persistent conditions.")]
        [CommandUsage("player clearconditions")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string ClearConditions(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Player player = playerConnection.Player;
            Avatar avatar = player.CurrentAvatar;

            int count = 0;

            foreach (Condition condition in avatar.ConditionCollection)
            {
                if (condition.IsPersistToDB() == false)
                    continue;

                avatar.ConditionCollection.RemoveCondition(condition.Id);
                count++;
            }

            return $"Cleared {count} persistent conditions.";
        }
    }
}
