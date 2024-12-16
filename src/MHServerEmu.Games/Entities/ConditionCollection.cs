using System.Collections;
using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;

namespace MHServerEmu.Games.Entities
{
    public class ConditionCollection : IEnumerable<KeyValuePair<ulong, Condition>>, ISerialize
    {
        // NOTE: Current state of implementation (March 2024):
        // - Adding and removing conditions works, but conditions don't affect their owners ("accrue").
        // - Condition management is semi-implemented (conditions are currently not aware of the collection they belong to).
        // - Some data for stacking (e.g. StackId) is implemented, but none of the actual functionality is.
        // - ConditionCollection::Iterator and ConditionCollection::Handle nested classes are not implemented (do we even need them?).

        public const int MaxConditions = 256;
        public const ulong InvalidConditionId = 0;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly WorldEntity _owner;

        private readonly SortedDictionary<ulong, Condition> _currentConditionDict = new();
        private readonly EventGroup _pendingEvents = new();

        private ulong _currentConditionId = 1;

        public ulong NextConditionId { get => _currentConditionId++; }

        public KeywordsMask ConditionKeywordsMask { get; internal set; }

        public ConditionCollection(WorldEntity owner)
        {
            _owner = owner;
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            // TODO: Persistent serialization
            if (archive.IsTransient)
            {
                if (archive.IsPacking)
                {
                    if (_currentConditionDict.Count >= MaxConditions)
                        return Logger.ErrorReturn(false, $"Serialize(): _currentConditionDict.Count >= MaxConditions");

                    uint numConditions = (uint)_currentConditionDict.Count;
                    success &= Serializer.Transfer(archive, ref numConditions);

                    foreach (Condition condition in _currentConditionDict.Values)
                        success &= condition.Serialize(archive, _owner);
                }
                else
                {
                    if (_currentConditionDict.Count != 0)
                        return Logger.ErrorReturn(false, $"Serialize(): _currrentConditionDict is not empty");

                    uint numConditions = 0;
                    success &= Serializer.Transfer(archive, ref numConditions);

                    if (numConditions >= MaxConditions)
                        return Logger.ErrorReturn(false, $"Serialize(): numConditions >= MaxConditions");

                    for (uint i = 0; i < numConditions; i++)
                    {
                        Condition condition = AllocateCondition();
                        success &= condition.Serialize(archive, _owner);
                        InsertCondition(condition);
                    }
                }
            }

            return success;
        }

        /// <summary>
        /// Returns the <see cref="Condition"/> with the specified condition id (key).
        /// Returns <see langword="null"/> if no <see cref="Condition"/> with such id is present in this <see cref="ConditionCollection"/>.
        /// </summary>
        public Condition GetCondition(ulong conditionId)
        {
            if (_currentConditionDict.TryGetValue(conditionId, out Condition condition) == false)
                return null;

            return condition;
        }

        /// <summary>
        /// Returns the <see cref="Condition"/> with the specified <see cref="PrototypeId"/>.
        /// Returns <see langword="null"/> if no <see cref="Condition"/> with such prototype is present in this <see cref="ConditionCollection"/>.
        /// </summary>
        public Condition GetConditionByRef(PrototypeId conditionRef)
        {
            if (conditionRef == PrototypeId.Invalid) return Logger.WarnReturn<Condition>(null, $"GetConditionByRef(): conditionRef == PrototypeId.Invalid");

            foreach (Condition condition in _currentConditionDict.Values)
            {
                if (condition.ConditionPrototypeRef == conditionRef)
                    return condition;
            }

            return null; 
        }

        /// <summary>
        /// Returns the id (key) of the condition with the specified <see cref="PrototypeId"/>.
        /// Returns invalid id (0) if no condition with such prototype is present in this <see cref="ConditionCollection"/>.
        /// </summary>
        public ulong GetConditionIdByRef(PrototypeId conditionRef)
        {
            Condition condition = GetConditionByRef(conditionRef);
            if (condition == null) return InvalidConditionId;
            return condition.Id;
        }

        public IEnumerable<Condition> IterateConditions(bool skipDisabled)
        {
            if (skipDisabled)
            {
                foreach (Condition condition in _currentConditionDict.Values)
                {
                    if (condition.IsEnabled)
                        yield return condition;
                }
            }
            else
            {
                foreach (Condition condition in _currentConditionDict.Values)
                    yield return condition;
            }
        }

        public int GetNumberOfStacks(Condition condition)
        {
            throw new NotImplementedException();
        }

        public int GetNumberOfStacks(StackId stackId)
        {
            throw new NotImplementedException();
        }

        public bool AddCondition(Condition condition)
        {
            if (_owner == null) return Logger.WarnReturn(false, "AddCondition(): _owner == null");
            if (_owner.Game == null) return Logger.WarnReturn(false, "AddCondition(): _owner.Game == null");
            if (condition == null) return Logger.WarnReturn(false, "AddCondition(): condition == null");

            Logger.Debug($"AddCondition(): {condition.CreatorPowerPrototype} {condition.ConditionPrototype}");

            if (InsertCondition(condition) == false)
                return false;

            condition.ResetStartTime();

            // Notify interested clients if any
            if (_owner != null)     // TODO: remove null check when we separate parsing from ConditionCollection implementation
            {
                var networkManager = _owner.Game.NetworkManager;
                var interestedClients = networkManager.GetInterestedClients(_owner);
                if (interestedClients.Any())
                {
                    NetMessageAddCondition addConditionMessage = ArchiveMessageBuilder.BuildAddConditionMessage(_owner, condition);
                    networkManager.SendMessageToMultiple(interestedClients, addConditionMessage);
                }
            }

            OnInsertCondition(condition);
            return true;
        }

        public bool RemoveCondition(ulong conditionId)
        {
            if (_currentConditionDict.TryGetValue(conditionId, out Condition condition) == false)
                return false;

            return RemoveCondition(condition);
        }

        public bool ResetOrRemoveCondition(ulong conditionId)
        {
            if (_owner == null) return Logger.WarnReturn(false, "ResetOrRemoveCondition(): _owner == null");

            Condition condition = GetCondition(conditionId);
            if (condition == null) return false;

            return ResetOrRemoveCondition(condition);
        }

        public bool ResetOrRemoveCondition(Condition condition)
        {
            Logger.Debug($"ResetOrRemoveCondition(): {condition}");

            // TODO: reset

            // Removing by id also checks to make sure this condition is in this collection
            RemoveCondition(condition.Id);
            return true;
        }

        public bool RemoveAllConditions(bool removePersistToDB = true)
        {
            if (_owner == null) return Logger.WarnReturn(false, "RemoveAllConditions(): _owner == null");
            if (_owner.Game == null) return Logger.WarnReturn(false, "RemoveAllConditions(): _owner.Game == null");

            // Convert values to an array so that we can remove items from the dict while we iterate
            foreach (Condition condition in _currentConditionDict.Values.ToArray())
            {
                if (removePersistToDB == false && condition.IsPersistToDB()) continue;
                RemoveCondition(condition);
            }

            return true;
        }

        /// <summary>
        /// Attempts to readd a condition to the <see cref="Power"/> that created it.
        /// Returns <see langword="false"/> if condition is no longer valid.
        /// </summary>
        public bool TryRestorePowerCondition(Condition condition, WorldEntity owner)
        {
            if (owner == null)
                return false;

            if (condition.CreatorPowerPrototypeRef == PrototypeId.Invalid)
                return false;

            // Restore tracking if the power is still assigned and can be used
            Power power = owner.PowerCollection?.GetPower(condition.CreatorPowerPrototypeRef);
            if (power != null)
            {
                if (power.CanBeUsedInRegion(owner.Region) == false)
                    return false;

                if (power.IsTrackingCondition(owner.Id, condition) == false)
                    power.TrackCondition(owner.Id, condition);

                return true;
            }

            // Sometimes the condition should remain even if the power that created it is gone
            PowerPrototype powerProto = condition.CreatorPowerPrototype;
            if (powerProto == null) return Logger.WarnReturn(false, "TryRestorePowerCondition(): powerProto == null");

            // Consumable items that grant conditions (e.g. boosts)
            if (Power.IsItemPower(powerProto))
                return true;

            // Powers that do not cancel their conditions when they are gone
            if (powerProto.Activation != PowerActivationType.Passive && powerProto.CancelConditionsOnEnd == false && powerProto.CancelConditionsOnUnassign == false)
                return true;

            // Remove conditions in other cases
            return false;
        }

        public bool HasANegativeStatusEffectCondition()
        {
            foreach (Condition condition in _currentConditionDict.Values)
                if (condition != null && condition.IsANegativeStatusEffect()) return true;
            return false;
        }

        public IEnumerator<KeyValuePair<ulong, Condition>> GetEnumerator() => _currentConditionDict.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public Condition AllocateCondition()
        {
            if (_owner == null) return Logger.WarnReturn<Condition>(null, "AllocateCondition(): _owner == null");
            if (_owner.Game == null) return Logger.WarnReturn<Condition>(null, "AllocateCondition(): _owner.Game == null");
            return new();
        }

        public void OnOwnerDeallocate()
        {
            _owner.Game.GameEventScheduler.CancelAllEvents(_pendingEvents);

            // We need to remove all conditions here to unbind their property collections.
            // If we don't do that, the garbage collector can't clean them and we end up with a memory leak.
            RemoveAllConditions();
        }

        private bool InsertCondition(Condition condition)
        {
            // TODO: ConditionCollection::Handle?

            if (_currentConditionDict.TryAdd(condition.Id, condition))
            {
                condition.Properties.Bind(_owner, AOINetworkPolicyValues.AllChannels);
                return true;
            }

            return false;
        }

        private bool RemoveCondition(Condition condition)
        {
            if (_owner == null) return Logger.WarnReturn(false, "RemoveCondition(): _owner == null");
            if (condition == null) return false;

            Logger.Debug($"RemoveCondition(): {condition.CreatorPowerPrototype} {condition.ConditionPrototype}");

            // TODO: more checks
            // TODO: ConditionCollection::unaccrueCondition()

            CancelScheduledConditionEnd(condition);

            if (_currentConditionDict.Remove(condition.Id) == false)
                Logger.Warn($"RemoveCondition(): Failed to remove condition id {condition.Id}");

            // Notify interested clients if any
            var networkManager = _owner.Game.NetworkManager;
            var interestedClients = networkManager.GetInterestedClients(_owner);
            if (interestedClients.Any())
            {
                var deleteConditionMessage = NetMessageDeleteCondition.CreateBuilder()
                    .SetIdEntity(_owner.Id)
                    .SetKey(condition.Id)
                    .Build();

                networkManager.SendMessageToMultiple(interestedClients, deleteConditionMessage);
            }

            condition.Properties.Unbind();
            return true;
        }

        private void OnInsertCondition(Condition condition)
        {
            // TODO

            OnPreAccrueCondition(condition);

            OnPostAccrueCondition(condition);
        }

        private void OnPreAccrueCondition(Condition condition)
        {

        }

        private void OnPostAccrueCondition(Condition condition)
        {
            if (ScheduleConditionEnd(condition) == false)
                return;
        }

        private void UnaccrueCondition(Condition condition)
        {
            // TODO
            OnPostUnaccrueCondition(condition);
        }

        private void OnPostUnaccrueCondition(Condition condition)
        {

        }

        private bool ScheduleConditionEnd(Condition condition)
        {
            if (condition == null) return Logger.WarnReturn(false, "ScheduleConditionEnd(): condition == null");

            if (condition.Duration > TimeSpan.Zero)
            {
                TimeSpan timeRemaining = condition.TimeRemaining;
                if (timeRemaining <= TimeSpan.Zero)
                {
                    RemoveCondition(condition);
                    return false;
                }

                EventPointer<RemoveConditionEvent> removeEvent = new();
                condition.RemoveEvent = removeEvent;

                _owner.Game.GameEventScheduler.ScheduleEvent(removeEvent, timeRemaining, _pendingEvents);
                removeEvent.Get().Initialize(this, condition.Id);
            }

            return true;
        }

        private bool CancelScheduledConditionEnd(Condition condition)
        {
            if (condition == null) return Logger.WarnReturn(false, "CancelScheduledConditionEnd(): condition == null");

            EventPointer<RemoveConditionEvent> removeEvent = condition.RemoveEvent;

            if (removeEvent?.IsValid == true)
                _owner.Game.GameEventScheduler.CancelEvent(condition.RemoveEvent);

            return true;
        }

        public readonly struct StackId
        {
            public PrototypeId PrototypeRef { get; }    // ConditionPrototype or PowerPrototype
            public int CreatorPowerIndex { get; }
            public ulong CreatorId { get; }             // EntityId or PlayerGuid

            public StackId(PrototypeId prototypeRef, int creatorPowerIndex, ulong creatorId)
            {
                // See ConditionCollection::MakeConditionStackId()
                PrototypeRef = prototypeRef;
                CreatorPowerIndex = creatorPowerIndex;
                CreatorId = creatorId;
            }
        }

        public class RemoveConditionEvent : CallMethodEventParam1<ConditionCollection, ulong>
        {
            protected override CallbackDelegate GetCallback() => (t, p1) => t.RemoveCondition(p1);
        }
    }
}
