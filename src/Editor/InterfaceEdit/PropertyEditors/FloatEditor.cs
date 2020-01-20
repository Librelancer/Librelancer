// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Reflection;
using ImGuiNET;

namespace InterfaceEdit
{
    public class FloatEditor : PropertyEditor
    {
        private float originalValue;
        private float currentValue;
        public FloatEditor(object obj, PropertyInfo property) : base(obj, property)
        {
            currentValue = originalValue = property.ValueOrDefault(obj, 0.0f);
        }

        public override bool Edit()
        {
            ImGui.Text(Property.Name);
            ImGui.NextColumn();
            var enterPressed = ImGui.InputFloat($"##{Property.Name}", ref currentValue, 0.0f, 0f, "%.3f",
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