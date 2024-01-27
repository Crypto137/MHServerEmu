using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    public partial class CalligraphySerializer
    {
        /// <summary>
        /// Copies field values from a <see cref="Prototype"/> with the specified data ref.
        /// </summary>
        private static bool CopyPrototypeDataRefFields(Prototype destPrototype, PrototypeId sourceDataRef)
        {
            // Check to make sure our reference is valid
            if (sourceDataRef == PrototypeId.Invalid)
                return Logger.WarnReturn(false, "Failed to copy prototype data ref fields: invalid source ref");

            // Get source prototype and copy fields from it
            Prototype sourcePrototype = GameDatabase.GetPrototype<Prototype>(sourceDataRef);
            return CopyPrototypeFields(destPrototype, sourcePrototype);
        }

        /// <summary>
        /// Copies field values from one <see cref="Prototype"/> to another.
        /// </summary>
        private static bool CopyPrototypeFields(Prototype destPrototype, Prototype sourcePrototype)
        {
            // Get type information for both prototypes and make sure they are the same
            Type destType = destPrototype.GetType();
            Type sourceType = sourcePrototype.GetType();

            if (sourceType != destType)
                return Logger.WarnReturn(false, $"Failed to copy prototype fields: source type ({sourceType.Name}) does not match destination type ({destType.Name})");

            foreach (var fieldInfo in destType.GetProperties())
            {
                if (fieldInfo.DeclaringType == typeof(Prototype)) continue;      // Skip base prototype properties

                if (IsMixin(fieldInfo.PropertyType))
                    CopyMixin(destPrototype, sourcePrototype, fieldInfo);
                else if (fieldInfo.PropertyType.IsValueType || fieldInfo.PropertyType.IsSubclassOf(typeof(Prototype)))
                    AssignPointedAtValues(destPrototype, sourcePrototype, fieldInfo);
                else if (fieldInfo.PropertyType.IsArray)
                    ShallowCopyCollection(destPrototype, sourcePrototype, fieldInfo);
                else if (fieldInfo.PropertyType == typeof(List<PrototypeMixinListItem>))
                    CopyMixinCollection();
                else if (fieldInfo.PropertyType == typeof(PrototypePropertyCollection))
                    CopyPrototypePropertyCollection();
                else
                    Logger.Warn($"Trying to copy unknown ref type {fieldInfo.PropertyType}");
            }

            return true;
        }

        private static void AssignPointedAtValues(Prototype destPrototype, Prototype sourcePrototype, System.Reflection.PropertyInfo fieldInfo)
        {
            fieldInfo.SetValue(destPrototype, fieldInfo.GetValue(sourcePrototype));
        }

        private static void ShallowCopyCollection(Prototype destPrototype, Prototype sourcePrototype, System.Reflection.PropertyInfo fieldInfo)
        {
            var sourceData = (Array)fieldInfo.GetValue(sourcePrototype);
            if (sourceData == null) return;

            int numItems = sourceData.Length;
            var destData = Array.CreateInstance(fieldInfo.PropertyType.GetElementType(), numItems);
            Array.Copy(sourceData, destData, numItems);
            fieldInfo.SetValue(destPrototype, destData);
        }

        private static void CopyMixin(Prototype destPrototype, Prototype sourcePrototype, System.Reflection.PropertyInfo fieldInfo)
        {
            // Population info is always null
            if (fieldInfo.PropertyType == typeof(PopulationInfoPrototype)) return;

            var destMixin = (Prototype)fieldInfo.GetValue(destPrototype);
            var sourceMixin = (Prototype)fieldInfo.GetValue(sourcePrototype);

            // Create the mixin instance on the destination prototype if it doesn't exist
            if (destMixin == null)
            {
                destMixin = (Prototype)Activator.CreateInstance(fieldInfo.PropertyType);
                fieldInfo.SetValue(destPrototype, destMixin);
            }

            CopyPrototypeFields(destMixin, sourceMixin);
        }

        private static void CopyMixinCollection()
        {
            // NYI
        }

        private static void CopyPrototypePropertyCollection()
        {
            // NYI
        }

        private static bool IsMixin(Type type)
        {
            return type == typeof(LocomotorPrototype) || type == typeof(PopulationInfoPrototype) || type == typeof(ProductPrototype);
        }

    }
}
