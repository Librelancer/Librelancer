// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Reflection;
using ImGuiNET;

namespace InterfaceEdit
{
    public class BoolEditor : PropertyEditor
    {
        bool check;            
        public BoolEditor(object obj, PropertyInfo property) : base(obj, property)
        {
            check = (bool) property.GetValue(obj);
        }

        public override bool Edit()
        {
            ImGui.Text(Property.Name);
            ImGui.NextColumn();
            if (ImGui.Checkbox($"##{Property.Name}", ref check))
            {
                Property.SetValue(Object, check);
                ImGui.NextColumn();
                return true;
            }
            ImGui.NextColumn();
            return false;
        }
    }
}