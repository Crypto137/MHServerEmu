using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public readonly struct PrototypeDataHeader
    {
        [Flags]
        private enum PrototypeDataDesc : byte
        {
            None                = 0,
            ReferenceExists     = 1 << 0,
            InstanceDataExists  = 1 << 1,
            PolymorphicData     = 1 << 2
        }

        public bool ReferenceExists { get; }
        public bool InstanceDataExists { get; }
        public bool PolymorphicData { get; }
        public PrototypeId ReferenceType { get; }     // Parent prototype id, invalid (0) for .defaults

        public PrototypeDataHeader(BinaryReader reader)
        {
            var flags = (PrototypeDataDesc)reader.ReadByte();
            ReferenceExists = flags.HasFlag(PrototypeDataDesc.ReferenceExists);
            InstanceDataExists = flags.HasFlag(PrototypeDataDesc.InstanceDataExists);
            PolymorphicData = flags.HasFlag(PrototypeDataDesc.PolymorphicData);

            ReferenceType = ReferenceExists ? (PrototypeId)reader.ReadUInt64() : PrototypeId.Invalid;
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

        /// <summary>
        /// Returns <see cref="true"/> if this <see cref="Prototype"/> needs to have its CRC calculated.
        /// </summary>
        public virtual bool ShouldCacheCRC { get => false; }

        /// <summary>
        /// Returns <see langword="true"/> if this prototype is approved for use. Approval criteria differ depending on the prototype type.
        /// </summary>
        public virtual bool ApprovedForUse()
        {
            return true;
        }

        /// <summary>
        /// Post-processes data contained in this <see cref="Prototype"/>.
        /// </summary>
        public virtual void PostProcess()
        {
            GameDatabase.PrototypeClassManager.PostProcessContainedPrototypes(this);
            // Prototypes override this to post-process data
        }

        /// <summary>
        /// PreCheck data contained in this <see cref="Prototype"/>.
        /// </summary>
        public void PreCheck()
        {
            GameDatabase.PrototypeClassManager.PreCheck(this);
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

        public int GetEnumValueFromBlueprint(BlueprintId blueprintId)
        {
            // Fall back to parent prototype for RHStructs
            PrototypeId protoRef = DataRef == PrototypeId.Invalid ? ParentDataRef : DataRef;

            if (protoRef == PrototypeId.Invalid)
                return 0;

            DataOrigin dataOrigin = DataDirectory.Instance.GetDataOrigin(protoRef);
            if (dataOrigin != DataOrigin.Calligraphy && dataOrigin != DataOrigin.Dynamic)
                return 0;

            Blueprint blueprint = DataDirectory.Instance.GetBlueprint(blueprintId);
            if (blueprint == null) return Logger.WarnReturn(0, "GetEnumValueFromBlueprint(): blueprint == null");

            return blueprint.GetPrototypeEnumValue(protoRef);
        }

        public override string ToString() => DataRef != PrototypeId.Invalid ? GameDatabase.GetPrototypeName(DataRef) : base.ToString();
    }
}
