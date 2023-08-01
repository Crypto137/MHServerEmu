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
            GameMessage[] loadedMessages = PacketHelper.LoadMessagesFromPacketFile("AvengersTowerFinishLoading.bin");
            List<NetMessagePowerCollectionAssignPower> powerList = new();
            switch (avatar)
            {
                case HardcodedAvatarEntity.BlackCat:
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

                    #region NetMessageSetProperty
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
                    #endregion

                    break;

                case HardcodedAvatarEntity.Angela:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Angela)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Angela)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Angela.AngelaFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.AntMan:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.AntMan)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.AntMan)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.AntMan.AntmanFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Beast:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Beast)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Beast)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Beast.BeastSprint ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.BlackBolt:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.BlackBolt)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.BlackBolt)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.BlackBolt.BlackBoltFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.BlackPanther:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.BlackPanther)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.BlackPanther)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.BlackPanther.BlackPantherSprint ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.BlackWidow:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.BlackWidow)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.BlackWidow)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.BlackWidow.BlackWidowRide ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Blade:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Blade)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Blade)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Blade.BladeRide ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Cable:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Cable)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Cable)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Cable.CableSprint ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.CaptainAmerica:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.CaptainAmerica)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.CaptainAmerica)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.CaptainAmerica.CaptainAmericaSprint ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Carnage:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Carnage)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Carnage)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Carnage.CarnageFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Colossus:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Colossus)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Colossus)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Colossus.ColossusSprint ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Cyclops:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Cyclops)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Cyclops)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Cyclops.CyclopsRide ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Daredevil:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Daredevil)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Daredevil)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Daredevil.DaredevilFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Deadpool:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Deadpool)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Deadpool)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Deadpool.DeadpoolRide ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.DoctorStrange:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.DoctorStrange)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.DoctorStrange)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.DoctorStrange.DoctorStrangeFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.DrDoom:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.DrDoom)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.DrDoom)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.DrDoom.DrDoomFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Elektra:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Elektra)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Elektra)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Elektra.ElektraSprint ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.EmmaFrost:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.EmmaFrost)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.EmmaFrost)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.EmmaFrost.EmmaFrostSprint ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Gambit:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Gambit)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Gambit)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Gambit.GambitSprint ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.GhostRider:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.GhostRider)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.GhostRider)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.GhostRider.GhostRiderRide ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.GreenGoblin:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.GreenGoblin)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.GreenGoblin)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.GreenGoblin.GreenGoblinFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Hawkeye:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Hawkeye)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Hawkeye)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Hawkeye.HawkeyeFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Hulk:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Hulk)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Hulk)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Hulk.HulkSprint ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.HumanTorch:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.HumanTorch)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.HumanTorch)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.HumanTorch.HumanTorchFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Iceman:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Iceman)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Iceman)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Iceman.IcemanFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.InvisibleWoman:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.InvisibleWoman)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.InvisibleWoman)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.InvisibleWoman.InvisibleWomanFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.IronFist:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.IronFist)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.IronFist)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.IronFist.IronFistSprint ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.IronMan:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.IronMan)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.IronMan)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.IronMan.IronManFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.JeanGrey:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.JeanGrey)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.JeanGrey)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.JeanGrey.JeanGreyFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Juggernaut:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Juggernaut)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Juggernaut)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Juggernaut.JuggernautSprint ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.KittyPryde:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.KittyPryde)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.KittyPryde)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.KittyPryde.KittyPrydeFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Loki:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Loki)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Loki)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Loki.LokiFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.LukeCage:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.LukeCage)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.LukeCage)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.LukeCage.LukeCageSprint ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Magik:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Magik)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Magik)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Magik.MagikFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Magneto:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Magneto)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Magneto)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Magneto.MagnetoFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.MoonKnight:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.MoonKnight)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.MoonKnight)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.MoonKnight.MoonKnightFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.MrFantastic:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.MrFantastic)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.MrFantastic)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.MrFantastic.MrFantasticSprint ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.MsMarvel:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.MsMarvel)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.MsMarvel)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.MsMarvel.CaptainMarvelFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.NickFury:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.NickFury)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.NickFury)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.NickFury.NickFuryRide ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Nightcrawler:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Nightcrawler)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Nightcrawler)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Nightcrawler.NightcrawlerSprint ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Nova:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Nova)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Nova)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Nova.NovaFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Psylocke:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Psylocke)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Psylocke)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Psylocke.PsylockeSprint ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Punisher:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Punisher)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Punisher)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Punisher.PunisherSprint ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.RocketRaccoon:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.RocketRaccoon)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.RocketRaccoon)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.RocketRaccoon.RocketRacoonFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Rogue:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Rogue)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Rogue)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Rogue.RogueFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.ScarletWitch:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.ScarletWitch)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.ScarletWitch)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.ScarletWitch.ScarletWitchFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.SheHulk:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.SheHulk)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.SheHulk)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.SheHulk.SheHulkSprint ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.SilverSurfer:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.SilverSurfer)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.SilverSurfer)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.SilverSurfer.SilverSurferFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Spiderman:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Spiderman)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Spiderman)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Spiderman.SpidermanFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.SquirrelGirl:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.SquirrelGirl)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.SquirrelGirl)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.SquirrelGirl.SquirrelGirlSprint ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Starlord:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Starlord)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Starlord)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Starlord.StarlordFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Storm:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Storm)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Storm)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Storm.StormFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Taskmaster:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Taskmaster)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Taskmaster)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Taskmaster.TaskmasterFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Thing:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Thing)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Thing)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Thing.ThingFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Thor:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Thor)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Thor)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Thor.ThorFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));

                    messageList.Add(new(GameServerToClientMessage.NetMessageSetProperty, NetMessageSetProperty.CreateBuilder()
                        .SetReplicationId(9078450)
                        .SetPropertyId(18863546) // no idea why this loads Thors power panel
                        .SetValueBits(2)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Ultron:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Ultron)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Ultron)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Ultron.UltronFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Venom:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Venom)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Venom)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Venom.VenomFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Vision:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Vision)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Vision)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Vision.VisionFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.WarMachine:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.WarMachine)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.WarMachine)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.WarMachine.WarMarchineFlight ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.WinterSoldier:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.WinterSoldier)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.WinterSoldier)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.WinterSoldier.WinterSoldierSprint ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.Wolverine:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Wolverine)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.Wolverine)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Wolverine.WolverineRide ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;
                case HardcodedAvatarEntity.X23:
                    powerList = new();
                    foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.X23)))
                    {
                        powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId((ulong)HardcodedAvatarEntity.X23)
                            .SetPowerProtoId(powerProtoId)
                            .SetPowerRank(powerProtoId == (ulong)PowerPrototypes.X23.X23Sprint ? 1 : 0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(1)
                            .Build());
                    }

                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddRangePower(powerList)
                        .Build().ToByteArray()));
                    break;

            }

            return messageList.ToArray();
        }
    }
}
