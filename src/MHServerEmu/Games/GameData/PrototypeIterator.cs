using MHServerEmu.Games.GameData.Prototypes;
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

    public class PrototypeIterator : IEnumerable<Prototype>
    {
        private readonly IEnumerable<PrototypeDataRefRecord> _prototypeRecords;
        private readonly PrototypeIterateFlags _flags;

        /// <summary>
        /// Constructs a new prototype iterator for the specified prototype class type. Iterates through all prototypes if no class is specified.
        /// </summary>
        public PrototypeIterator(Type prototypeClassType = null, PrototypeIterateFlags flags = PrototypeIterateFlags.None)
        {
            if (prototypeClassType == null) prototypeClassType = typeof(Prototype);     // Iterate through all prototypes if no class type is specified

            _prototypeRecords = DataDirectory.Instance.GetIteratedPrototypesInHierarchy(prototypeClassType);
            _flags = flags;
        }

        /// <summary>
        /// Constructs a new prototype iterator for the specified blueprint.
        /// </summary>
        public PrototypeIterator(BlueprintId blueprintId, PrototypeIterateFlags flags = PrototypeIterateFlags.None)
        {
            _prototypeRecords = DataDirectory.Instance.GetIteratedPrototypesInHierarchy(blueprintId);
            _flags = flags;
        }

        public IEnumerator<Prototype> GetEnumerator()
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

                // TODO: skip unapproved prototypes

                yield return record.Prototype;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
