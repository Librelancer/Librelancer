// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using ImGuiNET;
using LibreLancer.ImUI;
using System.Reflection;

namespace InterfaceEdit
{
    public class ObjectStringEditor : PropertyEditor
    {
        private string ogString;
        TextBuffer textBuffer = new TextBuffer();
        private string editId;
        private string popupId;
        private bool open = false;
        public ObjectStringEditor(object obj, PropertyInfo property) : base(obj, property)
        {
            ogString = property.ValueOrDefault<string>(obj, "");
            editId = $"...##editButton{Property.Name}";
            popupId = $"{Property.Name}##stringEditor";
        }

        public override bool Edit()
        {
            ImGui.Text(Property.Name);
            ImGui.NextColumn(); 
            ImGui.Text(ogString);
            ImGui.SameLine();
            if (ImGui.Button(editId))
            {
                textBuffer.SetText(ogString);
                ImGui.OpenPopup(popupId);
                open = true;
            }

            if (ImGui.BeginPopupModal(popupId, ref open, ImGuiWindowFlags.AlwaysAutoResize))
            {
                textBuffer.InputText("Value", ImGuiInputTextFlags.None, 200);
                if (ImGui.Button("Ok"))
                {
                    ImGui.CloseCurrentPopup();
                    Property.SetValue(Object, textBuffer.GetText());
                    return true;
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
            ImGui.NextColumn();
            return false;
        }

        public override void Dispose()
        {
            textBuffer.Dispose();
        }
    }
}