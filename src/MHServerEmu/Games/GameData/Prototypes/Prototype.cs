using MHServerEmu.Common.Logging;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public readonly struct PrototypeDataHeader
    {
        // CalligraphyReader::ReadPrototypeHeader

        [Flags]
        private enum PrototypeDataDesc : byte
        {
            None            = 0,
            ReferenceExists = 1 << 0,
            DataExists      = 1 << 1,
            PolymorphicData = 1 << 2
        }

        public bool ReferenceExists { get; }
        public bool DataExists { get; }
        public bool PolymorphicData { get; }
        public PrototypeId ReferenceType { get; }     // Parent prototype id, invalid (0) for .defaults

        public PrototypeDataHeader(BinaryReader reader)
        {
            var flags = (PrototypeDataDesc)reader.ReadByte();
            ReferenceExists = flags.HasFlag(PrototypeDataDesc.ReferenceExists);
            DataExists = flags.HasFlag(PrototypeDataDesc.DataExists);
            PolymorphicData = flags.HasFlag(PrototypeDataDesc.PolymorphicData);

            ReferenceType = ReferenceExists ? (PrototypeId)reader.ReadUInt64() : 0;
        }
    }

    public class Prototype
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        // Child prototypes need to have separate mixin lists from their parents so that when we modify a child we don't change its parent.
        // To ensure this each prototype keeps track of mixins that belong to it in this field. This is accurate to how the client does this.
        // The client uses a sorted vector here, but we use a HashSet to ensure that the same field data doesn't get added twice for some reason.
        private HashSet<object> _ownedDynamicFields;

        public PrototypeId DataRef { get; set; }
        public PrototypeId ParentDataRef { get; set; }
        public PrototypeDataRefRecord DataRefRecord { get; set; }

        public Prototype() { }

        /// <summary>
        /// Returns <see langword="false"/> if this is a prototype in development.
        /// </summary>
        public virtual bool ApprovedForUse()
        {
            return true;
        }

        // These dynamic field management methods are part of the PrototypeClassManager in the client, but it doesn't really make sense so we moved them here.
        // They work only with reference types, but we use them only for list mixins, so it's fine.

        /// <summary>
        /// Assigns this prototype as the owner of field data of type <typeparamref name="T"/>. Field data must be a reference type.
        /// </summary>
        public void SetDynamicFieldOwner<T>(T fieldData) where T: class
        {
            // Create the owned field collection if we don't have one
            if (_ownedDynamicFields == null)
                _ownedDynamicFields = new();

            _ownedDynamicFields.Add(fieldData);
        }

        /// <summary>
        /// Removes this prototype as the owner of field data of type <typeparamref name="T"/>. Field data must be a reference type.
        /// </summary>
        public bool RemoveDynamicFieldOwner<T>(T fieldData) where T: class
        {
            // Check if we have any dynamic fields at all
            if (_ownedDynamicFields == null)
                Logger.WarnReturn(false, $"Failed to remove {GameDatabase.GetPrototypeName(DataRef)} as the owner of dynamic field data: this prototype has no owned fields");

            if (_ownedDynamicFields.Remove(fieldData) == false)
                Logger.WarnReturn(false, $"Failed to remove {GameDatabase.GetPrototypeName(DataRef)} as the owner of dynamic field data: field data not found");

            return true;
        }

        /// <summary>
        /// Checks if this prototype is the owner of field data of type <typeparamref name="T"/>. Field data must be a reference type.
        /// </summary>
        public bool IsDynamicFieldOwnedBy<T>(T fieldData) where T: class
        {
            // Make sure this prototype has any dynamic fields assigned to it at all
            if (_ownedDynamicFields == null)
                return false;

            return _ownedDynamicFields.Contains(fieldData);
        }
    }
}
