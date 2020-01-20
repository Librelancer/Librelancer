// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Reflection;
using ImGuiNET;

namespace InterfaceEdit
{
    public class EnumEditor : PropertyEditor
    {
        private string[] items;
        private int originalIndex;
        private int selectedIndex;
        public EnumEditor(object obj, PropertyInfo property) : base(obj, property)
        {
            items = Enum.GetNames(property.PropertyType);
            var v = property.GetValue(obj).ToString();
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].Equals(v))
                {
                    originalIndex = selectedIndex = i;
                    break;
                }
            }
        }

        public override bool Edit()
        {
            ImGui.Text(Property.Name);
            ImGui.NextColumn();
            ImGui.Combo($"##{Property.Name}", ref selectedIndex, items, items.Length);
            ImGui.NextColumn();
            if (selectedIndex != originalIndex)
            {
                Property.SetValue(Object, Enum.Parse(Property.PropertyType, items[selectedIndex]));
                return true;
            }
            return false;
        }
    }
}