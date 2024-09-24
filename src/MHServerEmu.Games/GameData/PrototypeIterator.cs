using System.Collections;

namespace MHServerEmu.Games.GameData
{
    [Flags]
    public enum PrototypeIterateFlags : byte
    {
        None            = 0,
        //Flag0         = 1 << 0,   // Does nothing
        NoAbstract      = 1 << 1,
        ApprovedOnly    = 1 << 2,
        WithEditorOnly  = 1 << 3,   // Records that have EditorOnly set are skipped if this is not set

        NoAbstractApprovedOnly = NoAbstract | ApprovedOnly
    }

    /// <summary>
    /// Iterates through prototype records using specified filters.
    /// </summary>
    public readonly struct PrototypeIterator
    {
        private readonly List<PrototypeDataRefRecord> _prototypeRecordList;
        private readonly PrototypeIterateFlags _flags;

        /// <summary>
        /// Constructs an empty <see cref="PrototypeIterator"/>.
        /// </summary>
        public PrototypeIterator()
        {
            _prototypeRecordList = new();
            _flags = PrototypeIterateFlags.None;
        }

        /// <summary>
        /// Constructs a new <see cref="PrototypeIterator"/> with the provided records and flags.
        /// </summary>
        public PrototypeIterator(List<PrototypeDataRefRecord> records, PrototypeIterateFlags flags = PrototypeIterateFlags.None)
        {
            _prototypeRecordList = records;
            _flags = flags;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_prototypeRecordList, _flags);
        }

        public struct Enumerator : IEnumerator<PrototypeId>
        {
            private readonly List<PrototypeDataRefRecord> _recordList;
            private readonly PrototypeIterateFlags _flags;

            private int _index = -1;

            public PrototypeId Current { get; private set; } = default;
            object IEnumerator.Current { get => Current; }

            public Enumerator(List<PrototypeDataRefRecord> recordList, PrototypeIterateFlags flags)
            {
                _recordList = recordList;
                _flags = flags;
            }

            public bool MoveNext()
            {
                // Based on PrototypeIterator::advanceToValid()
                Current = PrototypeId.Invalid;

                while (++_index < _recordList.Count)
                {
                    PrototypeDataRefRecord record = _recordList[_index];

                    // Skip abstract prototypes if needed
                    if (record.Flags.HasFlag(Calligraphy.PrototypeRecordFlags.Abstract) && _flags.HasFlag(PrototypeIterateFlags.NoAbstract))
                        continue;

                    // Skip editor-only prototypes (which is just NaviFragmentPrototype) unless explicitly requested to include editor-only prototypes
                    if (record.Flags.HasFlag(Calligraphy.PrototypeRecordFlags.EditorOnly) && _flags.HasFlag(PrototypeIterateFlags.WithEditorOnly) == false)
                        continue;

                    // Skip unapproved prototypes if needed (NOTE: PrototypeIsApproved() forces the prototype to load)
                    if (_flags.HasFlag(PrototypeIterateFlags.ApprovedOnly) && GameDatabase.DataDirectory.PrototypeIsApproved(record) == false)
                        continue;

                    // We return PrototypeId instead of Prototype to simplify the implementation.
                    Current = record.PrototypeId;
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                _index = -1;
            }

            public void Dispose()
            {
            }
        }
    }
}
