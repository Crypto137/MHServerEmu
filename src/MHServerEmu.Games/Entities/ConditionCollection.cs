using System.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Powers;

namespace MHServerEmu.Games.Entities
{
    public class ConditionCollection : IEnumerable<KeyValuePair<ulong, Condition>>, ISerialize
    {
        // NOTE: Current state of implementation:
        // - Adding and removing conditions works, but conditions don't affect their owners ("accrue").
        // - Condition management is semi-implemented (conditions are currently not aware of the collection they belong to).
        // - Some data for stacking (e.g. StackId) is implemented, but none of the actual functionality is.
        // - ConditionCollection::Iterator and ConditionCollection::Handle nested classes are not implemented (do we even need them?).

        public const int MaxConditions = 256;
        public const ulong InvalidConditionId = 0;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly WorldEntity _owner;

        private SortedDictionary<ulong, Condition> _currentConditionDict = new();   // m_currentConditions

        public int Count { get => _currentConditionDict.Count; }    // Temp property for compatibility with our existing hacks
        public KeywordsMask ConditionKeywordsMask { get; internal set; }

        public ConditionCollection(WorldEntity owner)
        {
            _owner = owner;
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            // if (archive.IsTransient) -> This wasn't originally used for persistent serialization, same as individual conditions
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
            if (condition == null)
                return Logger.WarnReturn(false, "AddCondition(): condition == null");

            if (InsertCondition(condition) == false)
                return false;

            OnInsertCondition(condition);
            return true;
        }

        public bool RemoveCondition(ulong conditionId)
        {
            if (_currentConditionDict.TryGetValue(conditionId, out Condition condition) == false)
                return false;

            return RemoveCondition(condition);
        }

        public void RemoveAllConditions(bool removePersistToDB = true)
        {
            //if (_owner.Game == null) return;

            // Convert values to an array so that we can remove items from the dict while we iterate
            foreach (Condition condition in _currentConditionDict.Values.ToArray())
            {
                if (removePersistToDB == false && condition.IsPersistToDB()) continue;
                RemoveCondition(condition);
            }
        }

        public bool HasANegativeStatusEffectCondition()
        {
            foreach (Condition condition in _currentConditionDict.Values)
                if (condition != null && condition.IsANegativeStatusEffect()) return true;
            return false;
        }

        // TODO: ConditionCollection::Iterator implementation
        public IEnumerator<KeyValuePair<ulong, Condition>> GetEnumerator() => _currentConditionDict.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public Condition AllocateCondition()
        {
            // ConditionCollection::AllocateCondition() in the client also checks to make sure this collection has a valid owner / game.
            // However, this is going to break parsing of individual messages where owners do not exist in a game.
            // Possible solution: create a dummy game for parsing purposes.
            // if (_owner == null || _owner.Game == null)
            //     return null;

            return new();
        }

        private bool InsertCondition(Condition condition)
        {
            // TODO: ConditionCollection::Handle
            return _currentConditionDict.TryAdd(condition.Id, condition);
        }

        private void OnInsertCondition(Condition condition)
        {
            // NYI
        }

        private bool RemoveCondition(Condition condition)
        {
            if (_owner == null) return Logger.WarnReturn(false, "RemoveCondition(): _owner == null");
            if (condition == null) return false;

            // TODO: more checks
            // TODO: ConditionCollection::unaccrueCondition()

            if (_currentConditionDict.Remove(condition.Id) == false)
                Logger.Warn($"RemoveCondition(): Failed to remove condition id {condition.Id}");

            return true;
        }

        // See ConditionCollection::MakeConditionStackId()
        public readonly struct StackId
        {
            public PrototypeId PrototypeRef { get; }    // ConditionPrototype or PowerPrototype
            public int CreatorPowerIndex { get; }
            public ulong CreatorId { get; }             // EntityId or PlayerGuid

            public StackId(PrototypeId prototypeRef, int creatorPowerIndex, ulong creatorId)
            {
                PrototypeRef = prototypeRef;
                CreatorPowerIndex = creatorPowerIndex;
                CreatorId = creatorId;
            }
        }
    }
}
