// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using LibreLancer.Interface.Reflection;

namespace LibreLancer.Interface
{
    public class XmlObjectMap
    {
        public int Line;
        public int Column;
        public XElement Element;
        public object Object;
    }

    
    
    public class UiXmlLoader
    {
        public InterfaceResources Resources;
        
        public UiXmlLoader(InterfaceResources res)
        {
            Resources = res;
        }
        
        static bool PrimitiveList(PropertyInfo property, XElement element, Type type, out UiPrimitiveList list)
        {
            list = null;
            if (!type.IsEnum && !type.IsPrimitive || type != typeof(string)) return false;
            var values = new List<object>();
            foreach (var child in element.Elements())
            {
                var value = child.Value;
                if (type.IsEnum)
                    values.Add(Enum.Parse(type, value, true));
                else
                    values.Add(Convert.ChangeType(value, type, CultureInfo.InvariantCulture));
            }
            list = new UiPrimitiveList(property, values.ToArray());
            return true;
        }

        static bool CheckKeyType(Type type) => type.IsEnum || type.IsPrimitive || type == typeof(string);

        static object GetDictionaryKey(XElement element, Type keyType)
        {
            if (!CheckKeyType(keyType)) throw new InvalidCastException();
            var attr = element.Attribute("key");
            if (attr == null)  { throw new MissingFieldException("key"); }
            if (keyType.IsEnum)
                return Enum.Parse(keyType, attr.Value, true);
            return Convert.ChangeType(attr.Value, keyType, CultureInfo.InvariantCulture);
        }


        static bool PrimitiveDictionary(XElement element, PropertyInfo p, Type keyType, Type valueType, out UiPrimitiveDictionary primdict)
        {
            primdict = null;
            if(!CheckKeyType(keyType)) throw new InvalidCastException();
            if (!valueType.IsEnum && !valueType.IsPrimitive || valueType != typeof(string)) return false;
            List<object> keys = new List<object>();
            List<object> values = new List<object>();
            foreach (var child in element.Elements())
            {
                object value;
                var valueStr = child.Value;
                var key = GetDictionaryKey(child, keyType);
                if (valueType.IsEnum)
                    value = Enum.Parse(valueType, valueStr, true);
                else
                    value = Convert.ChangeType(valueStr, valueType, CultureInfo.InvariantCulture);
                keys.Add(key);
                values.Add(value);
            }
            primdict = new UiPrimitiveDictionary(p, keys.ToArray(), values.ToArray());
            return true;
        }

        public string StylesheetToLua(string source)
        {
            var elem = XElement.Parse(source);
            var obj = UiXmlReflection.Instantiate(elem.Name.ToString());
            if (obj.GetType() != typeof(Stylesheet)) throw new Exception();
            var x = ParseObject(typeof(Stylesheet), elem, null);
            return x.PrintStylesheetInit();
        }

        public (string, string) LuaClassDesigner(string xmlSource, string className)
        {
            var elem = XElement.Parse(xmlSource, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            var obj = UiXmlReflection.Instantiate(elem.Name.ToString());
            var x = FillObject(obj, elem, null);
            if (obj is UiWidget widget) {
                if (!string.IsNullOrWhiteSpace(widget.ClassName)) className = widget.ClassName;
            }

            return (className, x.PrintClassInit(className, "Widget"));
        }
        
        public object FromString(string source, List<XmlObjectMap> objectMaps)
        {
            var elem = XElement.Parse(source, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            var obj = UiXmlReflection.Instantiate(elem.Name.ToString());
            var x = FillObject(obj, elem, objectMaps);
            if (objectMaps != null)
            {
                objectMaps.Sort((a, b) =>
                {
                    var comp1 = a.Line.CompareTo(b.Line);
                    if (comp1 != 0) return comp1;
                    return a.Column.CompareTo(b.Column);
                });
            }
            return obj;
        }

        public UiLoadedObject ParseObject(Type objType, XElement el, List<XmlObjectMap> objectMaps)
        {
            Dictionary<string,InterfaceColor> interfaceColors = new Dictionary<string, InterfaceColor>();
            Dictionary<string,InterfaceModel> interfaceModels = new Dictionary<string, InterfaceModel>();
            Dictionary<string,InterfaceImage> interfaceImages = new Dictionary<string, InterfaceImage>();
            foreach(var clr in Resources.Colors)
                interfaceColors.Add(clr.Name, clr);
            foreach(var clr in Resources.Models)
                interfaceModels.Add(clr.Name, clr);
            foreach(var clr in Resources.Images)
                interfaceImages.Add(clr.Name, clr);
            return ParseObjectInternal(objType, el, objectMaps, interfaceColors, interfaceModels, interfaceImages);
        }

        static Vector3 ParseVector3(string s)
        {
            var sp = s.Split(',');
            if(sp.Length != 3) throw new FormatException();
            return new Vector3(
                float.Parse(sp[0], CultureInfo.InvariantCulture),
                float.Parse(sp[1], CultureInfo.InvariantCulture),
                float.Parse(sp[2], CultureInfo.InvariantCulture)
                );
        }
        
        UiLoadedObject ParseObjectInternal(
            Type objType, 
            XElement el, 
            List<XmlObjectMap> objectMaps,
            Dictionary<string,InterfaceColor> interfaceColors,
            Dictionary<string,InterfaceModel> interfaceModels,
            Dictionary<string,InterfaceImage> interfaceImages
         )
        {
            UiXmlReflection.GetProperties(
                objType, 
                out Dictionary<string,PropertyInfo> elements,
                out Dictionary<string,PropertyInfo> attributes,
                out PropertyInfo contentProperty
            );
            var result = new UiLoadedObject(objType);
            List<UiLoadedObject> contentObjects = null;
            bool isContentList = false;
            if (contentProperty != null)
            {
                isContentList = Activator.CreateInstance(contentProperty.PropertyType) is IList;
                if(isContentList) contentObjects = new List<UiLoadedObject>();
            }
            if (objectMaps != null)
            {
                var li = (IXmlLineInfo)el;
                objectMaps.Add(new XmlObjectMap()
                {
                    Line = li.LineNumber,
                    Column = li.LinePosition,
                    Element = el,
                    Object =  result
                });
            }
            foreach (var attr in el.Attributes())
            {
                var pname = attr.Name.ToString();
                PropertyInfo property;
                if (attributes.TryGetValue(pname, out property))
                {
                    object value = null;
                    var ptype = property.PropertyType;
                    if (ptype.IsEnum)
                        value = Enum.Parse(ptype, attr.Value, true);
                    else if (ptype == typeof(Vector3))
                    {
                        value = ParseVector3(attr.Value);
                    }
                    else if (ptype == typeof(InterfaceColor))
                    {
                        InterfaceColor color;
                        if (!interfaceColors.TryGetValue(attr.Value, out color))
                        {
                            color = new InterfaceColor() {Color = Parser.Color(attr.Value)};
                        }
                        value = color;
                    }
                    else if (ptype == typeof(InterfaceModel) && interfaceModels.TryGetValue(attr.Value, out var model))
                    {
                        value = model;
                    }
                    else if (ptype == typeof(InterfaceImage) && interfaceImages.TryGetValue(attr.Value, out var image))
                    {
                        value = image;
                    }
                    else if (ptype == typeof(string))
                        value = attr.Value;
                    else
                        value = Convert.ChangeType(attr.Value, ptype, CultureInfo.InvariantCulture);
                    result.Setters.Add(new UiSimpleProperty(property, value));
                }
                else
                {
                    //Throw error?
                }
            }
            foreach (var child in el.Elements())
            {
                var childName = child.Name.ToString();
                PropertyInfo property = null;
                if (childName.Contains("."))
                {
                    childName = childName.Split('.')[1];
                    if (!elements.TryGetValue(childName, out property))
                        throw new Exception($"{el.Name} does not have property {childName}");
                }
                if (property != null)
                {
                    var v = Activator.CreateInstance(property.PropertyType);
                    if (v is IList list)
                    {
                        var listType = property.PropertyType.GenericTypeArguments[0];
                        if (PrimitiveList(property, child, listType, out var primlist))
                        {
                            result.Setters.Add(primlist);
                        }
                        else
                        {
                            var objs = new List<UiLoadedObject>();
                            foreach (var itemXml in child.Elements())
                            {
                                var nt = UiXmlReflection.Instantiate(itemXml.Name.ToString()).GetType();
                                objs.Add(ParseObjectInternal(nt, itemXml, objectMaps, interfaceColors, interfaceModels, interfaceImages));
                            }

                            result.Setters.Add(new UiComplexList(property, objs.ToArray()));
                        }
                    } 
                    else if (v is IDictionary dict)
                    {
                        var keyType = property.PropertyType.GenericTypeArguments[0];
                        var valueType = property.PropertyType.GenericTypeArguments[1];
                        if (PrimitiveDictionary(child, property, keyType, valueType, out var primdict))
                        {
                            result.Setters.Add(primdict);
                        }
                        else
                        {
                            var keys = new List<object>();
                            var values = new List<UiLoadedObject>();
                            foreach (var itemXml in child.Elements())
                            {
                                var nt = UiXmlReflection.Instantiate(itemXml.Name.ToString()).GetType();
                                values.Add(ParseObjectInternal(nt, itemXml, objectMaps, interfaceColors, interfaceModels, interfaceImages));
                                keys.Add(GetDictionaryKey(itemXml, keyType));
                            }
                            result.Setters.Add(new UiComplexDictionary(property, keys.ToArray(), values.ToArray()));
                        }
                    }
                    else if (property.SetMethod != null)
                    {
                        var obj = ParseObjectInternal(property.PropertyType, child, objectMaps, interfaceColors,
                            interfaceModels, interfaceImages);
                        result.Setters.Add(new UiComplexProperty(property, obj));
                    }
                }
                else
                {
                    if(contentProperty == null)
                        throw new Exception($"{objType.FullName} lacks UiContentAttribute property");
                    var nt = UiXmlReflection.Instantiate(child.Name.ToString()).GetType();
                    var content = ParseObjectInternal(nt, child, objectMaps, interfaceColors, interfaceModels,
                        interfaceImages);
                    if(isContentList)
                        contentObjects.Add(content);
                    else
                        result.Setters.Add(new UiComplexProperty(contentProperty, content));
                }
                if (objectMaps != null)
                {
                    var li = child.NodesAfterSelf().FirstOrDefault() as IXmlLineInfo;
                    if (li != null)
                    {
                        objectMaps.Add(new XmlObjectMap()
                        {
                            Line = li.LineNumber,
                            Column = li.LinePosition,
                            Object = result
                        });
                    }
                }
            }
            if (isContentList)
            {
                result.Setters.Add(new UiComplexList(contentProperty, contentObjects.ToArray()));
            }
            return result;
        }
        
        public UiLoadedObject FillObject(object obj, XElement el, List<XmlObjectMap> objectMaps)
        {
            var parsed = ParseObject(obj.GetType(), el, objectMaps);
            parsed.Fill(obj, objectMaps);
            return parsed;
        }
    }
}