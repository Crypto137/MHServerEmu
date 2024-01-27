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
        WithEditorOnly  = 1 << 3    // Records that have EditorOnly set are skipped if this is not set
    }

    /// <summary>
    /// Iterates through prototype records using specified filters.
    /// </summary>
    public class PrototypeIterator : IEnumerable<PrototypeId>
    {
        private readonly IEnumerable<PrototypeDataRefRecord> _prototypeRecords;
        private readonly PrototypeIterateFlags _flags;

        /// <summary>
        /// Constructs an empty <see cref="PrototypeIterator"/>.
        /// </summary>
        public PrototypeIterator()
        {
            _prototypeRecords = Enumerable.Empty<PrototypeDataRefRecord>();
            _flags = PrototypeIterateFlags.None;
        }

        /// <summary>
        /// Constructs a new <see cref="PrototypeIterator"/> with the provided records and flags.
        /// </summary>
        public PrototypeIterator(IEnumerable<PrototypeDataRefRecord> records, PrototypeIterateFlags flags = PrototypeIterateFlags.None)
        {
            _prototypeRecords = records;
            _flags = flags;
        }

        public IEnumerator<PrototypeId> GetEnumerator()
        {
            // Based on PrototypeIterator::advanceToValid()
            foreach (var record in _prototypeRecords)
            {
                // Skip abstract prototypes if needed
                if (record.Flags.HasFlag(Calligraphy.PrototypeRecordFlags.Abstract) && _flags.HasFlag(PrototypeIterateFlags.NoAbstract))
                    continue;

                // Skip editor-only prototypes (which is just NaviFragmentPrototype) unless explicitly requested to include editor-only prototypes
                if (record.Flags.HasFlag(Calligraphy.PrototypeRecordFlags.EditorOnly) && _flags.HasFlag(PrototypeIterateFlags.WithEditorOnly) == false)
                    continue;

                // Skip unapproved prototypes if needed (note: PrototypeIsApproved() forces the prototype to load)
                if (_flags.HasFlag(PrototypeIterateFlags.ApprovedOnly) && GameDatabase.DataDirectory.PrototypeIsApproved(record) == false)
                    continue;

                // For now we return PrototypeId instead of Prototype to simplify the implementation.
                // The more accurate way would be a full IEnumerator where you could get either the id
                // or the prototype itself after calling MoveNext().
                yield return record.PrototypeId;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
