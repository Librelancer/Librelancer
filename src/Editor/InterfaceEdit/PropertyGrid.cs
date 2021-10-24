// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using System.Reflection;
using LibreLancer.Interface;

namespace InterfaceEdit
{
    public class PropertyGrid
    {
        delegate void AddPropertyAction(object o, List<PropertyEditor> editors);
        private static Dictionary<Type, List<AddPropertyAction>> constructors = new Dictionary<Type, List<AddPropertyAction>>();

        static readonly string[] DefinedOrder =
        {
            "ID", "Anchor", "X", "Y", "Width", "Height", "Strid", "InfocardId"
        };

        static int PropertyOrdering(PropertyInfo property)
        {
            var n = property.Name;
            for (int i = 0; i < DefinedOrder.Length; i++)
            {
                if (property.Name.Equals(DefinedOrder[i], StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return int.MaxValue;
        }
        static void PopulateEditors(object o, List<PropertyEditor> editors)
        {
            List<AddPropertyAction> constructor;
            var type = o.GetType();
            if (!constructors.TryGetValue(type, out constructor))
            {
                constructor = new List<AddPropertyAction>();
                foreach (var property in type.GetRuntimeProperties().OrderBy(PropertyOrdering).ThenBy(x => x.Name))
                {
                    if(property.GetCustomAttribute<UiIgnoreAttribute>() != null) 
                        continue;
                    if (property.PropertyType == typeof(string))
                    {
                        constructor.Add((obj,l) => l.Add(new ObjectStringEditor(obj, property)));
                    }
                    else if (property.PropertyType == typeof(float))
                    {
                        constructor.Add((obj, l) =>  l.Add(new FloatEditor(obj, property)));
                    }
                    else if (property.PropertyType == typeof(bool))
                    {
                        constructor.Add((obj, l) => l.Add(new BoolEditor(obj, property)));
                    }
                    else if (property.PropertyType == typeof(int))
                    {
                        constructor.Add((obj, l) => l.Add(new IntEditor(obj, property)));
                    }
                    else if (property.PropertyType.IsEnum)
                    {
                        constructor.Add((obj, l) => l.Add(new EnumEditor(obj, property)));
                    }
                }
                constructors.Add(type, constructor);
            }
            foreach (var c in constructor)
                c(o, editors);
        }
        
        private List<PropertyEditor> editors;
        private object editingObject;

        public void SetEditingObject(object obj)
        {
            if (editingObject == obj) return;
            editingObject = obj;
            if (editors != null)
            {
                foreach (var e in editors)
                    e.Dispose();
            }
            if (obj == null) editors = null;
            else
            {
                PopulateEditors();
            }
        }

        void PopulateEditors()
        {
            editors = new List<PropertyEditor>();
            PopulateEditors(editingObject, editors);
        }

        private bool first = true;
        public bool Draw()
        {
            if (editors == null) return false;
            ImGui.BeginChild("##propertyGrid");
            ImGui.Columns(2);
            if (first)
            {
                ImGui.SetColumnWidth(0, 110);
                first = false;
            }
            ImGui.Text("Name");
            ImGui.NextColumn();
            ImGui.Text("Value");
            ImGui.Separator();
            ImGui.NextColumn();
            bool edited = false;
            foreach(var e in editors)
                if (e.Edit())
                    edited = true;
            ImGui.EndChild();
            return edited;
        }
    }
}