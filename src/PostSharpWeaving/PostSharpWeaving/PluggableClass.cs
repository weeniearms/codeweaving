using System;
using System.Linq;
using System.Reflection;
using PostSharp.Aspects;
using PostSharp.Extensibility;

namespace PostSharpWeaving
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    [RequirePostSharp("PostSharpWeaving", "GeneratePluginBaseClassesTask")]
    public class PluggableClass : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs args)
        {
            var argumentArray = args.Arguments.ToArray() ?? new object[0];
            var methodInfo = (args.Method as MethodInfo);
            if (methodInfo == null)
                return;

            var parameters = new object[methodInfo.ReturnType == typeof(void) ? argumentArray.Length : argumentArray.Length + 1];
            for (var i = 0; i < argumentArray.Length; i++)
            {
                parameters[i] = argumentArray[i];
            }

            if (!(methodInfo.ReturnType == typeof(void)))
                parameters[parameters.Length - 1] = args.ReturnValue ?? (methodInfo.ReturnType.IsValueType ? Activator.CreateInstance(methodInfo.ReturnType) : null);

            var pluginMethod = GetPluginMethod(args, PluginMethodType.Before);
            if (pluginMethod == null)
                return;

            foreach (var pluginInstance in PluginFactory.GetPluginInstances(GetPluginType(args)))
            {
                if ((bool)pluginMethod.Invoke(pluginInstance, parameters))
                {
                    args.FlowBehavior = FlowBehavior.Return;
                    break;
                }
            }

            for (var i = 0; i < argumentArray.Length; i++)
            {
                args.Arguments[i] = parameters[i];
            }

            if (!(methodInfo.ReturnType == typeof(void)))
                args.ReturnValue = parameters[parameters.Length - 1];

            base.OnEntry(args);
        }

        public override void OnExit(MethodExecutionArgs args)
        {
            var argumentArray = args.Arguments.ToArray() ?? new object[0];
            var methodInfo = (args.Method as MethodInfo);
            if (methodInfo == null)
                return;

            var parameters = new object[methodInfo.ReturnType == typeof(void) ? argumentArray.Length : argumentArray.Length + 1];
            for (var i = 0; i < argumentArray.Length; i++)
            {
                parameters[i] = argumentArray[i];
            }

            if (!(methodInfo.ReturnType == typeof(void)))
                parameters[parameters.Length - 1] = args.ReturnValue ?? (methodInfo.ReturnType.IsValueType ? Activator.CreateInstance(methodInfo.ReturnType) : null);

            var pluginMethod = GetPluginMethod(args, PluginMethodType.After);
            if (pluginMethod == null)
                return;

            foreach (var pluginInstance in PluginFactory.GetPluginInstances(GetPluginType(args)))
            {
                pluginMethod.Invoke(pluginInstance, parameters);
            }

            for (var i = 0; i < argumentArray.Length; i++)
            {
                args.Arguments[i] = parameters[i];
            }

            if (!(methodInfo.ReturnType == typeof(void)))
                args.ReturnValue = parameters[parameters.Length - 1];

            base.OnExit(args);
        }

        private static MethodInfo GetPluginMethod(MethodExecutionArgs args, PluginMethodType pluginMethodType)
        {
            var pluginType = GetPluginType(args);
            if (pluginType == null)
                return null;

            var methodInfo = args.Method as MethodInfo;
            var parameterTypes =
                args.Method.GetParameters()
                           .Select(p => p.ParameterType)
                           .Concat(methodInfo.ReturnType == typeof(void) ?
                                   new Type[0] :
                                   new[] { methodInfo.ReturnType.Assembly.GetType(methodInfo.ReturnType.FullName + "&")})
                           .ToArray();

            return pluginType.GetMethod(pluginMethodType.ToString() + methodInfo.Name, parameterTypes);
        }

        private static Type GetPluginType(MethodExecutionArgs args)
        {
            var declaringType = args.Method.DeclaringType;
            return declaringType.Assembly.GetType(declaringType.FullName + "PluginBase");
        }

        public enum PluginMethodType
        {
            Before,
            After
        }
    }
}