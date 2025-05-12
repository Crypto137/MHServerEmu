using System.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions
{
    public readonly struct MissionConditionPrototypeIterator
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        // NOTE: We have to use class enumerators here because of recursion (any condition can be a list of conditions).
        // To avoid generating too much garbage we pool enumerators per thread using ThreadStatic.
        [ThreadStatic]
        private static Stack<Enumerator> _enumerators;

        private readonly MissionConditionPrototype[] _conditions;
        private readonly Type _conditionType;

        public MissionConditionPrototypeIterator(MissionConditionListPrototype list, Type conditionType = null)
        {
            _conditions = list?.Conditions;
            _conditionType = conditionType;
        }

        public Enumerator GetEnumerator()
        {
            _enumerators ??= new();

            Enumerator enumerator = _enumerators.Count > 0 ? _enumerators.Pop() : new();
            enumerator.Initialize(_conditions, _conditionType);
            return enumerator;
        }

        private static void ReturnEnumerator(Enumerator enumerator)
        {
            _enumerators.Push(enumerator);
        }

        public sealed class Enumerator : IEnumerator<MissionConditionPrototype>
        {
            private MissionConditionPrototype[] _conditions;
            private Type _conditionType;

            private int _currentIndex;
            private Enumerator _sublistIterator;

            public MissionConditionPrototype Current { get; private set; }
            object IEnumerator.Current { get => Current; }

            public Enumerator()
            {
            }

            public void Initialize(MissionConditionPrototype[] conditions, Type conditionType)
            {
                _conditions = conditions;
                _conditionType = conditionType;

                _currentIndex = -1;
                ClearSublistIterator();
            }

            public bool MoveNext()
            {
                if (_conditions == null)
                    return false;

                // Continue iterating the current sublist if there is one
                if (_sublistIterator != null)
                {
                    if (_sublistIterator.MoveNext())
                    {
                        Current = _sublistIterator.Current;
                        return true;
                    }
                    else
                    {
                        // Finish iterating the sublist
                        ClearSublistIterator();
                    }
                }

                // Advance to the next condition in the current list
                while (++_currentIndex < _conditions.Length)
                {
                    MissionConditionPrototype condition = _conditions[_currentIndex];
                    if (condition == null)
                    {
                        Logger.Warn("MoveNext(): condition == null");
                        continue;
                    }

                    // Start enumerating this condition as a sublist
                    if (condition is MissionConditionListPrototype conditionList)
                    {
                        _sublistIterator = new MissionConditionPrototypeIterator(conditionList, _conditionType).GetEnumerator();
                        if (_sublistIterator.MoveNext())
                        {
                            Current = _sublistIterator.Current;
                            return true;
                        }
                        else
                        {
                            // Skip this sublist if it doesn't contain any valid conditions
                            ClearSublistIterator();
                            continue;
                        }
                    }

                    // Filter by type if needed
                    if (_conditionType != null && condition.GetType() != _conditionType)
                        continue;

                    Current = condition;
                    return true;
                }

                Current = null;
                return false;
            }

            public void Reset()
            {
                Initialize(_conditions, _conditionType);
            }

            public void Dispose()
            {
                ClearSublistIterator();
                ReturnEnumerator(this);
            }
            
            private void ClearSublistIterator()
            {
                // NOTE: We need to call Dispose() on all sublist iterators to make sure they return to the pool.
                _sublistIterator?.Dispose();
                _sublistIterator = null;
            }
        }
    }
}
