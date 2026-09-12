using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Auga
{
    // Compiled only into the optional integration shim, never into the runtime plugin.
    public static partial class API
    {
        private static readonly Dictionary<string, MethodInfo> Methods = new Dictionary<string, MethodInfo>();
        private static Assembly _runtime;

        public static Assembly LoadAssembly()
        {
            if (_runtime == null)
                _runtime = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly =>
                    assembly != typeof(API).Assembly && assembly.GetName().Name == "Auga" && assembly.GetType("Auga.API") != null);
            return _runtime;
        }

        public static bool IsLoaded() => LoadAssembly() != null;

        private static T Invoke<T>(string name, Type[] parameterTypes, object[] arguments)
        {
            var runtime = LoadAssembly();
            if (runtime == null) return default(T);
            string key = name + "(" + string.Join(",", parameterTypes.Select(type => type.AssemblyQualifiedName)) + ")";
            MethodInfo method;
            lock (Methods)
            {
                if (!Methods.TryGetValue(key, out method))
                {
                    method = runtime.GetType("Auga.API").GetMethod(name, BindingFlags.Public | BindingFlags.Static, null,
                        parameterTypes.Select(type => RuntimeType(type, runtime)).ToArray(), null);
                    if (method != null) Methods.Add(key, method);
                }
            }
            if (method == null)
            {
                if (name == "IsReady" || name == "IsInventoryReady" || name == "SupportsFeature" || name == "GetApiVersion")
                    return default(T);
                throw new NotSupportedException("The installed Auga does not provide API method " + name + ". Update Auga or check its API capabilities.");
            }
            var parameters = method.GetParameters();
            var converted = arguments.Select((argument, index) => ConvertValue(argument, parameters[index].ParameterType)).ToArray();
            try { return (T)ConvertValue(method.Invoke(null, converted), typeof(T)); }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }

        private static bool IsContractType(Type type) => type.FullName == "Auga.RequirementWireState" ||
            type.FullName == "Auga.PlayerPanelTabData" || type.FullName == "Auga.WorkbenchTabData";

        private static Type RuntimeType(Type type, Assembly runtime)
        {
            if (type.IsArray) return RuntimeType(type.GetElementType(), runtime).MakeArrayType();
            if (IsContractType(type)) return runtime.GetType(type.FullName, true);
            if (type.IsGenericType) return type.GetGenericTypeDefinition().MakeGenericType(type.GetGenericArguments().Select(t => RuntimeType(t, runtime)).ToArray());
            return type;
        }

        private static object ConvertValue(object value, Type destination)
        {
            if (value == null) return destination.IsValueType ? Activator.CreateInstance(destination) : null;
            if (destination.IsInstanceOfType(value)) return value;
            if (destination.IsArray && value is Array array)
            {
                var element = destination.GetElementType();
                var result = Array.CreateInstance(element, array.Length);
                for (int i = 0; i < array.Length; i++) result.SetValue(ConvertValue(array.GetValue(i), element), i);
                return result;
            }
            if (IsContractType(destination) && destination.FullName == value.GetType().FullName)
            {
                if (destination.IsEnum) return Enum.ToObject(destination, Convert.ToInt32(value));
                var result = Activator.CreateInstance(destination);
                foreach (var field in destination.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    var source = value.GetType().GetField(field.Name);
                    if (source != null) field.SetValue(result, ConvertValue(source.GetValue(value), field.FieldType));
                }
                return result;
            }
            throw new InvalidCastException("Cannot bridge Auga API value " + value.GetType().FullName + " to " + destination.FullName);
        }
    }
}
