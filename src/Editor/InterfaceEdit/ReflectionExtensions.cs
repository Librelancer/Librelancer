// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Reflection;

namespace InterfaceEdit
{
    public static class ReflectionExtensions
    {
        public static T ValueOrDefault<T>(this PropertyInfo property, object obj, T d)
        {
            return (T) (property.GetValue(obj) ?? d);
        }
    }
}