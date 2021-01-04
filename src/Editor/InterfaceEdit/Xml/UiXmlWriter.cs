// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Xml.Linq;
using LibreLancer;
using LibreLancer.Interface;
using LibreLancer.Interface.Reflection;

namespace InterfaceEdit
{
    public class UiXmlWriter
    {
        public static void FillSimpleProperties(XElement element, object obj)
        {
            UiXmlReflection.GetProperties(obj.GetType(),
                out var elements,
                out var attributes,
                out var contentProperty
                );
            foreach (var prop in attributes)
            {
                var attribute = FindAttribute(element, prop.Key);
                if (attribute == null)
                {
                    if (UiXmlReflection.WriteProperty(prop.Value, obj))
                    {
                        var v = GetWriteable(prop.Value.GetValue(obj));
                        element.SetAttributeValue(prop.Key, v);
                    }
                }
                else
                {
                    if (UiXmlReflection.WriteProperty(prop.Value, obj))
                    {
                        var v = GetWriteable(prop.Value.GetValue(obj));
                        element.SetAttributeValue(attribute.Name, v);
                    }
                    else
                        attribute.Remove();
                }
            }
        
        }

        static object GetWriteable(object input)
        {
            if (input is InterfaceColor color)
            {
                if (!string.IsNullOrEmpty(color.Name)) return color.Name;
                var r = (int) (color.Color.R * 255);
                var g = (int) (color.Color.G * 255);
                var b = (int) (color.Color.B * 255);
                var a = (int) (color.Color.A * 255);
                return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
            }
            if (input is InterfaceModel model)
            {
                return model.Name;
            }
            if (input is InterfaceImage image)
            {
                return image.Name;
            }
            if (input is Vector3 vec)
            {
                const string FMT = "0.##########";
                return $"{vec.X.ToString(FMT)},{vec.Y.ToString(FMT)},{vec.Z.ToString(FMT)}";
            }
            return input;
        }
        
        
        static XAttribute FindAttribute(XElement element, string name) =>
                element.Attributes().SingleOrDefault(xa =>
                string.Equals(xa.Name.LocalName, name, StringComparison.OrdinalIgnoreCase));
    }
}