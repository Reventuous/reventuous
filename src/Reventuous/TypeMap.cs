using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Reventuous {
    [PublicAPI]
    public static class TypeMap {
        static readonly Dictionary<string, Type> ReverseMap = new();
        static readonly Dictionary<Type, string> Map        = new();

        public static string GetTypeName<T>() => Map[typeof(T)];
        
        public static string GetTypeName(object o) => Map[o.GetType()];
        
        public static string GetTypeNameByType(Type type) => Map[type];

        public static Type GetType(string typeName) => ReverseMap[typeName];
        
        public static bool TryGetType(string typeName, out Type? type) => ReverseMap.TryGetValue(typeName, out type);

        public static void AddType<T>(string name) {
            ReverseMap[name] = typeof(T);
            Map[typeof(T)]   = name;
        }

        public static bool IsTypeRegistered<T>() => Map.ContainsKey(typeof(T));
    }
}
