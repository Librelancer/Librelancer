// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Reflection;

namespace InterfaceEdit
{
    public abstract class PropertyEditor : IDisposable
    {
        public Object Object;
        public PropertyInfo Property;
        public PropertyEditor(object obj, PropertyInfo property)
        {
            Object = obj;
            Property = property;
        }

        public virtual void Dispose()
        {
        }

        public abstract bool Edit();
    }
}