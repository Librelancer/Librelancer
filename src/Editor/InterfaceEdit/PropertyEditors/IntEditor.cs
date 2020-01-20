// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Reflection;
using ImGuiNET;

namespace InterfaceEdit
{
    public class IntEditor : PropertyEditor
    {
        private int originalValue;
        private int currentValue;
        public IntEditor(object obj, PropertyInfo property) : base(obj, property)
        {
            currentValue = originalValue = property.ValueOrDefault(obj, 0);
        }

        public override bool Edit()
        {
            ImGui.Text(Property.Name);
            ImGui.NextColumn();
            var enterPressed = ImGui.InputInt($"##{Property.Name}", ref currentValue, 0, 0,
                ImGuiInputTextFlags.EnterReturnsTrue);
            ImGui.NextColumn();
            if (enterPressed)
            {
                Property.SetValue(Object, currentValue);
                return true;
            }

            return false;
        }
    }
}