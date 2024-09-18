using System.Reflection;
using System.Reflection.Emit;

namespace MHServerEmu.Core.Extensions
{
    public static class ReflectionExtensions
    {
        private static readonly Type[] CopyValueArgs = new Type[] { typeof(object), typeof(object) }; 

        private static readonly Dictionary<PropertyInfo, Delegate> CopyPropertyValueDelegateDict = new();

        private delegate void CopyValueDelegate(object source, object destination);

        /// <summary>
        /// Copies the value of a property from one instance to another. Both instances need to be of the same type.
        /// </summary>
        public static void CopyValue<T>(this PropertyInfo propertyInfo, T source, T destination)
        {
            // Cache copy delegates to avoid expensive reflection every time.
            // Emit IL directly because it's faster than doing expression trees.
            if (CopyPropertyValueDelegateDict.TryGetValue(propertyInfo, out Delegate copyValueDelegate) == false)
            {
                // We assume source and destination are going to be the same type
                Type type = propertyInfo.DeclaringType;

                DynamicMethod dm = new("CopyValue", null, CopyValueArgs);
                ILGenerator il = dm.GetILGenerator();

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Castclass, type);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, type);
                il.Emit(OpCodes.Callvirt, propertyInfo.GetGetMethod());
                il.Emit(OpCodes.Callvirt, propertyInfo.GetSetMethod(true));
                il.Emit(OpCodes.Ret);

                copyValueDelegate = dm.CreateDelegate<CopyValueDelegate>();
                CopyPropertyValueDelegateDict.Add(propertyInfo, copyValueDelegate);
            }

            var copy = (CopyValueDelegate)copyValueDelegate;
            copy(source, destination);
        }
    }
}
