using Gazillion;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Networking;

namespace MHServerEmu.Games.Powers
{
    public static class PowerLoader
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static GameMessage[] LoadAvatarPowerCollection(HardcodedAvatarEntityId avatar)
        {
            List<GameMessage> messageList = new();

            ulong replicationId = (ulong)avatar.ToPropertyCollectionReplicationId();
            var avatarPrototype = GameDatabase.GetPrototype<AvatarPrototype>(avatar.ToAvatarPrototypeId());

            // Gather all the powers we need to unlock
            List<PrototypeId> powersToUnlockList = new();

            // Progression table powers
            foreach (var progressionTable in avatarPrototype.PowerProgressionTables)
            {
                foreach (var entry in progressionTable.PowerProgressionEntries)
                {
                    //Logger.Debug(GameDatabase.GetPrototypeName(entry.PowerAssignment.Ability));
                    powersToUnlockList.Add(entry.PowerAssignment.Ability);
                }
            }

            // Mapped powers (power replacements from talents)
            // AvatarPrototype -> TalentGroups -> Talents -> Talent -> ActionsTriggeredOnPowerEvent -> PowerEventContext -> MappedPower
            foreach (var talentGroup in avatarPrototype.TalentGroups)
            {
                foreach (var talentEntry in talentGroup.Talents)
                {
                    var talent = talentEntry.Talent.As<SpecializationPowerPrototype>();

                    foreach (var powerEventAction in talent.ActionsTriggeredOnPowerEvent)
                    {
                        if (powerEventAction.PowerEventContext is PowerEventContextMapPowersPrototype mapPowerEvent)
                        {
                            foreach (MapPowerPrototype mapPower in mapPowerEvent.MappedPowers)
                            {
                                powersToUnlockList.Add(mapPower.MappedPower);
                            }
                        }
                    }
                }
            }

            // Stolen powers for Rogue
            if (avatar == HardcodedAvatarEntityId.Rogue)
            {
                foreach (PrototypeId stolenPowerInfoId in GameDatabase.DataDirectory.IteratePrototypesInHierarchy(typeof(StealablePowerInfoPrototype)))
                {
                    var stolenPowerInfo = stolenPowerInfoId.As<StealablePowerInfoPrototype>();
                    powersToUnlockList.Add(stolenPowerInfo.Power);
                }
            }

            // Travel
            powersToUnlockList.Add(avatarPrototype.TravelPower);

            // Emotes
            foreach (ulong powerProtoId in Enum.GetValues(typeof(PowerPrototypes.Emotes)))
                powersToUnlockList.Add((PrototypeId)powerProtoId);

            // Assign a power collection for all gathered powers
            List<NetMessagePowerCollectionAssignPower> powerList = new();
            foreach (PrototypeId powerProtoId in powersToUnlockList)
            {
                powerList.Add(NetMessagePowerCollectionAssignPower.CreateBuilder()
                    .SetEntityId((ulong)avatar)
                    .SetPowerProtoId((ulong)powerProtoId)
                    //.SetPowerRank(powerProtoId == (ulong)PowerPrototypes.Angela.AngelaFlight ? 1 : 0)
                    .SetPowerRank(0)
                    .SetCharacterLevel(60)
                    .SetCombatLevel(60)
                    .SetItemLevel(1)
                    .SetItemVariation(1)
                    .Build());
            }
            messageList.Add(new(NetMessageAssignPowerCollection.CreateBuilder().AddRangePower(powerList).Build()));

            // Set PowerRankCurrentBest for all powers in the collection to make them usable
            foreach (PrototypeId protoId in powersToUnlockList)
            {
                PropertyParam enumValue = Property.ToParam(PropertyEnum.PowerRankBase, 0, protoId);
                PropertyId propertyId = new(PropertyEnum.PowerRankCurrentBest, enumValue);
                messageList.Add(new(NetMessageSetProperty.CreateBuilder()
                    .SetReplicationId(replicationId)
                    .SetPropertyId(propertyId.Raw.ReverseBits())
                    .SetValueBits(2)
                    .Build()));
            }

            // PowerRankBase needs to be set for the powers window to show powers without changing spec tabs
            // NOTE: PowerRankBase is also supposed to have a power prototype param
            messageList.Add(new(Property.ToNetMessageSetProperty(replicationId, new(PropertyEnum.PowerRankBase), 1)));

            return messageList.ToArray();
        }
    }
}
