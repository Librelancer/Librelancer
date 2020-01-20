// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;


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
        
        static bool PrimitiveList(XElement element, IList list, Type type)
        {
            if (!type.IsEnum && !type.IsPrimitive || type != typeof(string)) return false;
            foreach (var child in element.Elements())
            {
                var value = child.Value;
                if (type.IsEnum)
                    list.Add(Enum.Parse(type, value, true));
                else
                    list.Add(Convert.ChangeType(value, type, CultureInfo.InvariantCulture));
            }
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


        static bool PrimitiveDictionary(XElement element, IDictionary dictionary, Type keyType, Type valueType)
        {
            if(!CheckKeyType(keyType)) throw new InvalidCastException();
            if (!valueType.IsEnum && !valueType.IsPrimitive || valueType != typeof(string)) return false;
            foreach (var child in element.Elements())
            {
                object value;
                var valueStr = child.Value;
                var key = GetDictionaryKey(child, keyType);
                if (valueType.IsEnum)
                    value = Enum.Parse(valueType, valueStr, true);
                else
                    value = Convert.ChangeType(valueStr, valueType, CultureInfo.InvariantCulture);
                dictionary[key] = value;
            }
            return true;
        }

        public object FromString(string source, List<XmlObjectMap> objectMaps)
        {
            var elem = XElement.Parse(source, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            var obj = UiXmlReflection.Instantiate(elem.Name.ToString());
            FillObject(obj, elem, objectMaps);
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

        internal void ReinitObject(object obj, XElement el)
        {
            UiXmlReflection.GetProperties(
                obj.GetType(), out _, out _, out var contentProperty, out _);
            if (contentProperty != null)
            {
                var list = contentProperty.GetValue(obj) as IList;
                list?.Clear();
            }
            FillObject(obj, el, null);
        }
        
        public void FillObject(object obj, XElement el, List<XmlObjectMap> objectMaps)
        {
            UiXmlReflection.GetProperties(
                obj.GetType(), 
                out Dictionary<string,PropertyInfo> elements,
                out Dictionary<string,PropertyInfo> attributes,
                out PropertyInfo contentProperty,
                out PropertyInfo reinitProperty
                );
            if (reinitProperty != null)
            {
                var handle = new UiRecreateHandle() {
                    Element = el, Object = obj, Loader = this
                };
                reinitProperty.SetValue(obj, handle);
            }
            if (objectMaps != null) {
                var li = (IXmlLineInfo)el;
                objectMaps.Add(new XmlObjectMap()
                {
                    Line = li.LineNumber,
                    Column = li.LinePosition,
                    Element = el,
                    Object =  obj
                });
            }
            Dictionary<string,InterfaceColor> interfaceColors = new Dictionary<string, InterfaceColor>();
            Dictionary<string,InterfaceModel> interfaceModels = new Dictionary<string, InterfaceModel>();
            Dictionary<string,InterfaceImage> interfaceImages = new Dictionary<string, InterfaceImage>();
            foreach(var clr in Resources.Colors)
                interfaceColors.Add(clr.Name, clr);
            foreach(var clr in Resources.Models)
                interfaceModels.Add(clr.Name, clr);
            foreach(var clr in Resources.Images)
                interfaceImages.Add(clr.Name, clr);
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
                    property.SetValue(obj, value);
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
                    var v = property.GetValue(obj);
                    if (v is IList list)
                    {
                        var listType = property.PropertyType.GenericTypeArguments[0];
                        if (!PrimitiveList(child, list, listType))
                        {
                            foreach (var itemXml in child.Elements())
                            {
                                var item = Activator.CreateInstance(listType);
                                FillObject(item, itemXml, objectMaps);
                                list.Add(item);
                            }
                        }
                    } 
                    else if (v is IDictionary dict)
                    {
                        var keyType = property.PropertyType.GenericTypeArguments[0];
                        var valueType = property.PropertyType.GenericTypeArguments[1];
                        if (!PrimitiveDictionary(child, dict, keyType, valueType))
                        {
                            foreach (var itemXml in child.Elements())
                            {
                                var item = Activator.CreateInstance(valueType);
                                FillObject(item, itemXml, objectMaps);
                                var key = GetDictionaryKey(itemXml, keyType);
                                dict[key] = item;
                            }
                        }
                    }
                    else if (property.SetMethod != null)
                    {
                        var newValue = Activator.CreateInstance(property.PropertyType);
                        FillObject(newValue, child, objectMaps);
                        property.SetValue(obj, newValue);
                    }
                }
                else
                {
                    if(contentProperty == null)
                        throw new Exception($"{obj.GetType().FullName} lacks UiContentAttribute property");
                    var content = UiXmlReflection.Instantiate(child.Name.ToString());
                    FillObject(content, child, objectMaps);
                    var containerValue = contentProperty.GetValue(obj);
                    if (containerValue is IList list)
                    {
                        list.Add(content);
                    }
                    else
                    {
                        contentProperty.SetValue(obj, content);
                    }
                }

                if (objectMaps != null)
                {
                    var nEnd = child.NodesAfterSelf().FirstOrDefault();
                    var li = (IXmlLineInfo) nEnd;
                    objectMaps.Add(new XmlObjectMap()
                    {
                        Line = li.LineNumber,
                        Column = li.LinePosition,
                        Object = obj
                    });
                }
            }
        }
    }
}