// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using LibreLancer;
using WattleScript.Interpreter.Interop;

namespace LibreLancer.Interface.Reflection
{
    public static class UiXmlReflection
    {
        class XmlTypeInfo
        {
            public Dictionary<string, PropertyInfo> Elements = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, PropertyInfo> Attributes = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            public PropertyInfo Content;
        }
        static Dictionary<Type, XmlTypeInfo> typeInfos = new Dictionary<Type, XmlTypeInfo>();
        static Dictionary<Type, object> cachedObjects = new Dictionary<Type, object>();
        private static Dictionary<string, Type> allowedTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        static List<Assembly> registered = new List<Assembly>();
        private static object _lockTypes = new object();

        static UiXmlReflection()
        {
            RegisterAssembly(typeof(UiContext).Assembly);    
        }
        
        public static void RegisterAssembly(Assembly assembly)
        {
            lock (_lockTypes)
            {
                if (registered.Contains(assembly)) return;
                registered.Add(assembly);
                foreach (var t in assembly.GetTypes())
                {
                    if (t.GetCustomAttribute<UiLoadableAttribute>() != null)
                    {
                        allowedTypes.Add(t.Name, t);
                    }
                }
            }
        }

        public static object Instantiate(string name)
        {
            lock (_lockTypes)
            {
                Type t;
                if (allowedTypes.TryGetValue(name, out t))
                    return Activator.CreateInstance(t);
                throw new Exception($"Type {name} doesn't exist'");
            }
        }

        static object GetObject(Type t)
        {
            object o;
            if (!cachedObjects.TryGetValue(t, out o))
            {
                o = Activator.CreateInstance(t);
                cachedObjects.Add(t, o);
            }

            return o;
        }
        
        public static bool WriteProperty(PropertyInfo prop, object obj)
        {
            var value = prop.GetValue(obj);
            if (value == null) return false;
            if (prop.PropertyType == typeof(InterfaceColor) ||
                prop.PropertyType == typeof(InterfaceModel) ||
                prop.PropertyType == typeof(InterfaceImage))
            {
                return true;
            }
            else
            {
                var orig = GetObject(obj.GetType());
                var defaultValue = prop.GetValue(orig);
                return !value.Equals(defaultValue);
            }
        }
        
        public static void GetProperties(
            Type type, 
            out Dictionary<string, PropertyInfo> elements,
            out Dictionary<string, PropertyInfo> attributes, 
            out PropertyInfo contentProperty
         )
        {
            XmlTypeInfo info;
            if (!typeInfos.TryGetValue(type, out info))
            {
                info = new XmlTypeInfo();
                foreach (var property in type.GetRuntimeProperties())
                {
                    if (!property.IsPropertyInfoPublic()) continue;
                    if(property.GetCustomAttribute<UiIgnoreAttribute>() != null) 
                        continue;
                    var ptype = property.PropertyType;
                    
                    if (ptype.IsPrimitive || ptype.IsEnum ||
                        ptype == typeof(string) || ptype == typeof(InterfaceColor) ||
                        ptype ==typeof(InterfaceModel) || ptype == typeof(InterfaceImage) ||
                        ptype == typeof(Vector3))
                    {
                        if (property.SetMethod == null)
                            continue;
                        info.Attributes.Add(property.Name, property);
                    }
                    else
                    {
                        if (property.GetCustomAttribute<UiContentAttribute>() != null)
                            info.Content = property;
                        info.Elements.Add(property.Name, property);
                    }
                }
                typeInfos.Add(type, info);
            }

            elements = info.Elements;
            attributes = info.Attributes;
            contentProperty = info.Content;
        }
    }
}