// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Reflection;
using ImGuiNET;
using LibreLancer.ImUI;

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
            var enterPressed = ImGuiExt.InputFloatExpr($"##{Property.Name}", ref currentValue, "%.3f",
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