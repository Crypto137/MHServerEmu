using System.Reflection;
using Gazillion;
using MHServerEmu.Networking;
using MHServerEmu.Common;
using MHServerEmu.GameServer.Entities;

namespace MHServerEmu.GameServer.Powers
{
    public static class PowerLoader
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static GameMessage[] LoadAvatarPowerCollection(HardcodedAvatarEntity avatar)
        {
            List<GameMessage> messageList = new();
            List<NetMessagePowerCollectionAssignPower> powerList = new();

            string avatarName = Enum.GetName(typeof(HardcodedAvatarEntity), avatar);

            if (avatarName != null)
            {
                Type powerPrototypeEnumType = typeof(PowerPrototypes).GetNestedType(avatarName, BindingFlags.Public);
                if (powerPrototypeEnumType != null)
                {
                    foreach (ulong powerProtoId in Enum.GetValues(powerPrototypeEnumType))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)avatar)
                            .SetPowerProtoId(powerProtoId)
                            //.SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Angela.AngelaFlight ? 1 : 0)
                            .SetPowerRank(0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                }
                else
                {
                    Logger.Warn($"Failed to get power prototype ids for {avatarName}");
                }

                Type propertyEnumType = typeof(PowerProperties).GetNestedType(avatarName, BindingFlags.Public);
                if (propertyEnumType != null)
                {
                    foreach (ulong propertyId in Enum.GetValues(propertyEnumType))
                    {
                        messageList.Add(new(GameServerToClientMessage.NetMessageSetProperty, NetMessageSetProperty.CreateBuilder()
                            .SetReplicationId(((ulong)Enum.Parse(typeof(HardcodedAvatarReplicationId), avatarName)))
                            .SetPropertyId(propertyId)
                            .SetValueBits(2)
                            .Build().ToByteArray()));
                    }
                }
                else
                {
                    Logger.Warn($"Failed to get power property ids for {avatarName}");
                }
            }
            else
            {
                Logger.Warn($"Failed to parse avatar {avatar}");
            }

            // TODO: figure out property ids that unlock power panels for other heroes
            if (avatar == HardcodedAvatarEntity.BlackCat)
            {
                messageList.Add(new(GameServerToClientMessage.NetMessageSetProperty, NetMessageSetProperty.CreateBuilder()
                    .SetReplicationId(9078507)
                    .SetPropertyId(52364730) // This Id unlocks Grapple Swing Line on the UI Panel
                    .SetValueBits(2)
                    .Build().ToByteArray()));
            }
            else if (avatar == HardcodedAvatarEntity.Thor)   
            {
                messageList.Add(new(GameServerToClientMessage.NetMessageSetProperty, NetMessageSetProperty.CreateBuilder()
                    .SetReplicationId((ulong)HardcodedAvatarReplicationId.Thor)
                    .SetPropertyId(18863546) // no idea why this loads Thors power panel
                    .SetValueBits(2)
                    .Build().ToByteArray()));
            }

            /* Dumped PowerCollection for Black Cat
            GameMessage[] loadedMessages = PacketHelper.LoadMessagesFromPacketFile("AvengersTowerFinishLoading.bin");
            foreach (GameMessage gameMessage in loadedMessages)
            {
                switch ((GameServerToClientMessage)gameMessage.Id)
                {
                    case GameServerToClientMessage.NetMessageAssignPowerCollection:
                        messageList.Add(gameMessage);
                        break;
                    case GameServerToClientMessage.NetMessagePowerCollectionAssignPower:
                        messageList.Add(gameMessage);
                        break;
                    case GameServerToClientMessage.NetMessagePowerCollectionUnassignPower:
                        messageList.Add(gameMessage);
                        break;
                    default:
                        break;
                }
            }
            */

            /* Dumped NetMessageSetProperty values for Black Cat
            messageList.Add(new(GameServerToClientMessage.NetMessageSetProperty, NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(9078507)
                .SetPropertyId(41083834) // This Id unlocks Unleashed
                .SetValueBits(40)
                .Build().ToByteArray()));
            messageList.Add(new(GameServerToClientMessage.NetMessageSetProperty, NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(9078507)
                .SetPropertyId(52364218) // This Id unlocks Graple Swing Line
                .SetValueBits(2)
                .Build().ToByteArray()));
            messageList.Add(new(GameServerToClientMessage.NetMessageSetProperty, NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(9078507)
                .SetPropertyId(18863034) // This Id unlocks Quick Getaway
                .SetValueBits(2)
                .Build().ToByteArray()));
            messageList.Add(new(GameServerToClientMessage.NetMessageSetProperty, NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(9078507)
                .SetPropertyId(16094138) // This Id unlocks Cat's Claws
                .SetValueBits(2)
                .Build().ToByteArray()));
            messageList.Add(new(GameServerToClientMessage.NetMessageSetProperty, NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(9078507)
                .SetPropertyId(40391610) // This Id unlocks Deep Cuts
                .SetValueBits(2)
                .Build().ToByteArray()));
            messageList.Add(new(GameServerToClientMessage.NetMessageSetProperty, NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(9078507)
                .SetPropertyId(24478650) // This Id unlocks Claws Out
                .SetValueBits(2)
                .Build().ToByteArray()));
            messageList.Add(new(GameServerToClientMessage.NetMessageSetProperty, NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(9078507)
                .SetPropertyId(22909882) // This Id unlocks Master Thief
                .SetValueBits(2)
                .Build().ToByteArray()));
            messageList.Add(new(GameServerToClientMessage.NetMessageSetProperty, NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(9078507)
                .SetPropertyId(3257274) // This Id unlocks Foe Fillet
                .SetValueBits(2)
                .Build().ToByteArray()));
            messageList.Add(new(GameServerToClientMessage.NetMessageSetProperty, NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(9078507)
                .SetPropertyId(52511674) // This Id unlocks Land On Your Feet
                .SetValueBits(2)
                .Build().ToByteArray()));
            messageList.Add(new(GameServerToClientMessage.NetMessageSetProperty, NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(9078507)
                .SetPropertyId(55354298) // This Id unlocks Cat Nap
                .SetValueBits(2)
                .Build().ToByteArray()));
            messageList.Add(new(GameServerToClientMessage.NetMessageSetProperty, NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(9078507)
                .SetPropertyId(38609850) // This Id unlocks The Cat's Meow
                .SetValueBits(2)
                .Build().ToByteArray()));
            messageList.Add(new(GameServerToClientMessage.NetMessageSetProperty, NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(9078507)
                .SetPropertyId(32191418) // This Id unlocks Explosive Trap
                .SetValueBits(2)
                .Build().ToByteArray()));
            messageList.Add(new(GameServerToClientMessage.NetMessageSetProperty, NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(9078507)
                .SetPropertyId(16360378) // This Id unlocks Gas Trap
                .SetValueBits(2)
                .Build().ToByteArray()));
            messageList.Add(new(GameServerToClientMessage.NetMessageSetProperty, NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(9078507)
                .SetPropertyId(57111482)  // This Id unlocks Whip Crack
                .SetValueBits(2)
                .Build().ToByteArray()));
            messageList.Add(new(GameServerToClientMessage.NetMessageSetProperty, NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(9078507)
                .SetPropertyId(7828410)  // This Id unlocks Grappling Whip
                .SetValueBits(2)
                .Build().ToByteArray()));
            messageList.Add(new(GameServerToClientMessage.NetMessageSetProperty, NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(9078507)
                .SetPropertyId(38605754) // This Id unlocks C'mere Kitty
                .SetValueBits(2)
                .Build().ToByteArray()));
            messageList.Add(new(GameServerToClientMessage.NetMessageSetProperty, NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(9078507)
                .SetPropertyId(50938810) // This Id unlocks Sticky Trap
                .SetValueBits(2)
                .Build().ToByteArray()));
            messageList.Add(new(GameServerToClientMessage.NetMessageSetProperty, NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(9078507)
                .SetPropertyId(63943610) // This Id unlocks Put 'Em Down
                .SetValueBits(2)
                .Build().ToByteArray()));
            messageList.Add(new(GameServerToClientMessage.NetMessageSetProperty, NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(9078507)
                .SetPropertyId(34288570) // This Id unlocks Taser Trap
                .SetValueBits(2)
                .Build().ToByteArray()));
            messageList.Add(new(GameServerToClientMessage.NetMessageSetProperty, NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(9078507)
                .SetPropertyId(52364730) // This Id unlocks Grapple Swing Line on the UI Panel
                .SetValueBits(2)
                .Build().ToByteArray()));
            */

            return messageList.ToArray();
        }
    }
}
