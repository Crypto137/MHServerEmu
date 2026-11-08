using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace MHServerEmu.Core.Extensions
{
    public static class ReflectionExtensions
    {
        private static readonly MethodInfo ArrayCloneMethod = typeof(Array).GetMethod("Clone");

        private static readonly Dictionary<PropertyInfo, FieldInfo> PropertyBackingFields = new();
        private static readonly Dictionary<PropertyInfo, InlineArray4<Delegate>> PropertyDelegates = new();

        // Notes:
        // - Reflection.Emit is faster than expression trees.
        // - Get/set using FieldInfo is faster than PropertyInfo.
        // - Use generic delegates to avoid value type boxing.
        // - Reflection is expensive, so cache everything.

        /// <summary>
        /// Returns the <see cref="FieldInfo"/> for the backing field of the auto property represented by this <see cref="PropertyInfo"/>.
        /// </summary>
        public static FieldInfo GetBackingField(this PropertyInfo propertyInfo)
        {
            if (PropertyBackingFields.TryGetValue(propertyInfo, out FieldInfo fieldInfo) == false)
            {
                string backingFieldName = $"<{propertyInfo.Name}>k__BackingField";
                fieldInfo = propertyInfo.DeclaringType.GetField(backingFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                Debug.Assert(fieldInfo != null);
                PropertyBackingFields.Add(propertyInfo, fieldInfo);
            }

            return fieldInfo;
        }

        /// <summary>
        /// Returns a delegate that retrieves the value of the auto property represented by this <see cref="PropertyInfo"/> avoiding boxing.
        /// </summary>
        public static Func<TInstance, TValue> CreateGetDelegate<TInstance, TValue>(this PropertyInfo propertyInfo)
        {
            ref Delegate getDelegate = ref GetDelegateRef(propertyInfo, PropertyDelegate.Get);
            if (getDelegate == null)
            {
                FieldInfo fieldInfo = propertyInfo.GetBackingField();

                DynamicMethod dm = new("GetValue", typeof(TValue), [typeof(TInstance)]);
                ILGenerator il = dm.GetILGenerator();

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, fieldInfo);
                il.Emit(OpCodes.Ret);

                getDelegate = dm.CreateDelegate<Func<TInstance, TValue>>();
            }

            return (Func<TInstance, TValue>)getDelegate;
        }

        /// <summary>
        /// Returns a delegate that sets the value of the auto property represented by this <see cref="PropertyInfo"/> avoiding boxing.
        /// </summary>
        public static Action<TInstance, TValue> CreateSetDelegate<TInstance, TValue>(this PropertyInfo propertyInfo)
        {
            ref Delegate setDelegate = ref GetDelegateRef(propertyInfo, PropertyDelegate.Set);
            if (setDelegate == null)
            {
                FieldInfo fieldInfo = propertyInfo.GetBackingField();

                DynamicMethod dm = new("SetValue", null, [typeof(TInstance), typeof(TValue)]);
                ILGenerator il = dm.GetILGenerator();

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, fieldInfo);
                il.Emit(OpCodes.Ret);

                setDelegate = dm.CreateDelegate<Action<TInstance, TValue>>();
            }

            return (Action<TInstance, TValue>)setDelegate;
        }

        /// <summary>
        /// Returns a delegate that copies the value of the auto property represented by this <see cref="PropertyInfo"/> from one instance to another.
        /// </summary>
        public static Action<T, T> CreateCopyDelegate<T>(this PropertyInfo propertyInfo)
        {
            ref Delegate copyDelegate = ref GetDelegateRef(propertyInfo, PropertyDelegate.Copy);
            if (copyDelegate == null)
            {
                FieldInfo fieldInfo = propertyInfo.GetBackingField();

                DynamicMethod dm = new("CopyValue", null, [typeof(T), typeof(T)]);
                ILGenerator il = dm.GetILGenerator();

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, fieldInfo);
                il.Emit(OpCodes.Stfld, fieldInfo);
                il.Emit(OpCodes.Ret);

                copyDelegate = dm.CreateDelegate<Action<T, T>>();
            }

            return (Action<T, T>)copyDelegate;
        }

        /// <summary>
        /// Returns a delegate that creates a shallow copy of the array value of the auto property represented by this <see cref="PropertyInfo"/>
        /// and assigns it to the destination instance.
        /// </summary>
        public static Action<T, T> CreateCopyArrayDelegate<T>(this PropertyInfo propertyInfo)
        {
            ref Delegate copyArrayDelegate = ref GetDelegateRef(propertyInfo, PropertyDelegate.CopyArray);
            if (copyArrayDelegate == null)
            {
                FieldInfo fieldInfo = propertyInfo.GetBackingField();
                Type fieldType = fieldInfo.FieldType;
                Debug.Assert(fieldType.IsAssignableTo(typeof(Array)));

                DynamicMethod dm = new("CopyArrayValue", null, [typeof(T), typeof(T)]);
                ILGenerator il = dm.GetILGenerator();

                il.DeclareLocal(fieldInfo.FieldType);
                Label retLabel = il.DefineLabel();

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, fieldInfo);
                il.Emit(OpCodes.Stloc_0);

                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Brfalse_S, retLabel);

                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Callvirt, ArrayCloneMethod);
                il.Emit(OpCodes.Castclass, fieldType);
                il.Emit(OpCodes.Stloc_0);

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Stfld, fieldInfo);

                il.MarkLabel(retLabel);
                il.Emit(OpCodes.Ret);

                copyArrayDelegate = dm.CreateDelegate<Action<T, T>>();
            }

            return (Action<T, T>)copyArrayDelegate;
        }

        private static ref Delegate GetDelegateRef(PropertyInfo propertyInfo, PropertyDelegate delegateEnum)
        {
            ref InlineArray4<Delegate> delegates = ref PropertyDelegates.GetValueRefOrAddDefault(propertyInfo);
            return ref delegates[(int)delegateEnum];
        }

        private enum PropertyDelegate
        {
            Get,
            Set,
            Copy,
            CopyArray,
            NumDelegates,
        }
    }
}
