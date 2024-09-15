using System.Linq.Expressions;
using System.Reflection;

namespace MHServerEmu.Core.Extensions
{
    public static class ReflectionExtensions
    {
        private static readonly Dictionary<PropertyInfo, Delegate> CopyPropertyValueDelegateDict = new();

        private delegate void CopyValueDelegate(object source, object destination);

        /// <summary>
        /// Copies the value of a property from one instance to another. Both instances need to be of the same type.
        /// </summary>
        public static void CopyValue<T>(this PropertyInfo propertyInfo, T source, T destination)
        {
            // Use compiled lambda expressions to avoid doing expensive reflection every time
            if (CopyPropertyValueDelegateDict.TryGetValue(propertyInfo, out Delegate copyValueDelegate) == false)
            {
                ParameterExpression sourceParam = Expression.Parameter(typeof(object));
                ParameterExpression destinationParam = Expression.Parameter(typeof(object));

                // We assume source and destination are going to be the same type
                Type type = propertyInfo.DeclaringType;

                UnaryExpression castSourceParam = Expression.Convert(sourceParam, type);
                UnaryExpression castDestinationParam = Expression.Convert(destinationParam, type);

                MethodCallExpression getCall = Expression.Call(castSourceParam, propertyInfo.GetGetMethod());
                MethodCallExpression setCall = Expression.Call(castDestinationParam, propertyInfo.GetSetMethod(true), getCall);

                copyValueDelegate = Expression.Lambda<CopyValueDelegate>(setCall, sourceParam, destinationParam).Compile();
                CopyPropertyValueDelegateDict.Add(propertyInfo, copyValueDelegate);
            }

            var copy = (CopyValueDelegate)copyValueDelegate;
            copy(source, destination);
        }
    }
}
