using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace PostSharpWeaving
{
    public static class PluginFactory
    {
        private static readonly Dictionary<Type, List<Type>> PluginTypes = new Dictionary<Type, List<Type>>();

        public static void Reset()
        {
            PluginTypes.Clear();
        }

        public static void RegisterPlugin<TPluginBase, TPlugin>()
            where TPlugin : TPluginBase, new()
        {
            if (PluginTypes.ContainsKey(typeof(TPluginBase)))
            {
                PluginTypes[typeof(TPluginBase)].Add(typeof(TPlugin));
            }
            else
            {
                PluginTypes[typeof(TPluginBase)] = new List<Type> { typeof(TPlugin) };
            }
        }

        public static IEnumerable GetPluginInstances(Type pluginType)
        {
            return PluginTypes.ContainsKey(pluginType) ? PluginTypes[pluginType].Select(Activator.CreateInstance).ToArray() : new object[0];
        }
    }
}