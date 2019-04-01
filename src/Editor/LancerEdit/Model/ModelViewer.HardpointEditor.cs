// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;

using ImGuiNET;
using LibreLancer;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf;
using LibreLancer.ImUI;
namespace LancerEdit
{
    public partial class ModelViewer
    {
        HardpointDefinition hpEditing;
        HardpointDefinition hpDelete;
        List<HardpointDefinition> hpDeleteFrom;
        void ConfirmDelete(PopupData data)
        {
            ImGui.Text(string.Format("Are you sure you wish to delete '{0}'?", hpDelete.Name));
            if (ImGui.Button("Yes"))
            {
                hpDeleteFrom.Remove(hpDelete);
                var gz = gizmos.Where((x) => x.Definition == hpDelete).First();
                if (hpDelete == hpEditing) hpEditing = null;
                gizmos.Remove(gz);
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("No"))
            {
                ImGui.CloseCurrentPopup();
            }
        }
        void MinMaxWarning(PopupData data)
        {
            ImGui.Text("Min was bigger than max, swapped.");
            if (ImGui.Button("Ok")) ImGui.CloseCurrentPopup();
        }
        int GetHpNumbering(string name)
        {
            int val = 0;
            foreach (var gz in gizmos)
            {
                if (gz.Definition.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                {
                    int a;
                    if (int.TryParse(gz.Definition.Name.Substring(name.Length), out a))
                    {
                        val = Math.Max(a, val);
                    }
                }
            }
            return val + 1;
        }
        char GetHpLettering(string name)
        {
            int letter = (int)'`';
            foreach (var gz in gizmos)
            {
                if (gz.Definition.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                {
                    if (gz.Definition.Name.Length > name.Length)
                    {
                        letter = Math.Max(char.ToLowerInvariant(gz.Definition.Name[name.Length]), letter);
                    }
                }
            }
            return char.ToUpperInvariant((char)(letter + 1));
        }
        TextBuffer newHpBuffer = new TextBuffer(256);
        bool newIsFixed = false;
        List<HardpointDefinition> addTo;
        AbstractConstruct addConstruct;
        double newErrorTimer = 0;
        void NewHardpoint(PopupData data)
        {
            ImGui.Text("Name: ");
            ImGui.SameLine();
            newHpBuffer.InputText("##hpname", ImGuiInputTextFlags.None);
            ImGui.SameLine();
            if (ImGui.Button(".."))
            {
                ImGui.OpenPopup("names");
            }
            if (ImGui.BeginPopupContextItem("names"))
            {
                var infos = newIsFixed ? HardpointInformation.Fix : HardpointInformation.Rev;
                foreach (var item in infos)
                {
                    if (Theme.IconMenuItem(item.Name, item.Icon, item.Color, true))
                    {
                        switch (item.Autoname)
                        {
                            case HpNaming.None:
                                newHpBuffer.SetText(item.Name);
                                break;
                            case HpNaming.Number:
                                newHpBuffer.SetText(item.Name + GetHpNumbering(item.Name).ToString("00"));
                                break;
                            case HpNaming.Letter:
                                newHpBuffer.SetText(item.Name + GetHpLettering(item.Name));
                                break;
                        }
                    }
                }
                ImGui.EndPopup();
            }
            ImGui.Text("Type: " + (newIsFixed ? "Fixed" : "Revolute"));
            if (newErrorTimer > 0)
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), "Hardpoint with that name already exists.");
            }
            if (ImGui.Button("Ok"))
            {
                var txt = newHpBuffer.GetText();
                if (txt.Length == 0)
                {
                    return;
                }
                if (gizmos.Any((x) => x.Definition.Name.Equals(txt, StringComparison.OrdinalIgnoreCase)))
                    newErrorTimer = 6;
                else
                {
                    HardpointDefinition def;
                    if (newIsFixed) def = new FixedHardpointDefinition(txt);
                    else def = new RevoluteHardpointDefinition(txt);
                    gizmos.Add(new HardpointGizmo(def, addConstruct));
                    addTo.Add(def);
                    ImGui.CloseCurrentPopup();
                }
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }
        }
        bool hpEditOpen = false;
        HardpointGizmo editingGizmo;
        float HPpitch, HPyaw, HProll;
        float HPx, HPy, HPz;
        float HPaxisX, HPaxisY, HPaxisZ;
        float HPmin, HPmax;
        void SetHardpointValues()
        {
            HPx = hpEditing.Position.X;
            HPy = hpEditing.Position.Y;
            HPz = hpEditing.Position.Z;
            var euler = hpEditing.Orientation.GetEulerDegrees();
            HPpitch = euler.X;
            HPyaw = euler.Y;
            HProll = euler.Z;
            if (hpEditing is RevoluteHardpointDefinition)
            {
                var rev = (RevoluteHardpointDefinition)hpEditing;
                HPmin = MathHelper.RadiansToDegrees(rev.Min);
                HPmax = MathHelper.RadiansToDegrees(rev.Max);
                HPaxisX = rev.Axis.X;
                HPaxisY = rev.Axis.Y;
                HPaxisZ = rev.Axis.Z;
            }
        }
        bool hpFirst;
        unsafe void HardpointEditor()
        {
            if (hpEditing == null)
            {
                hpEditOpen = false;
                return;
            }
            if (hpEditing != null && hpEditOpen == false)
            {
                editingGizmo = gizmos.First((x) => x.Definition == hpEditing);
                hpEditOpen = true;
                hpFirst = true;
                SetHardpointValues();
            }
            if (ImGui.Begin("Hardpoint Editor##" + Unique, ref hpEditOpen, hpFirst ? ImGuiWindowFlags.AlwaysAutoResize : ImGuiWindowFlags.None))
            {
                hpFirst = false;
                ImGui.Text(hpEditing.Name);
                bool isFix = hpEditing is FixedHardpointDefinition;
                ImGui.Text("Type: " + (isFix ? "Fixed" : "Revolute"));
                if (ImGui.Button("Reset")) SetHardpointValues();
                ImGui.Separator();
                ImGui.Text("Position");
                    ImGui.InputFloat("X##posX", ref HPx, 0.01f, 0.25f, "%.5f", ImGuiInputTextFlags.CharsDecimal);
                    ImGui.InputFloat("Y##posY", ref HPy, 0.01f, 0.25f, "%.5f", ImGuiInputTextFlags.CharsDecimal);
                    ImGui.InputFloat("Z##posZ", ref HPz, 0.01f, 0.25f, "%.5f", ImGuiInputTextFlags.CharsDecimal);
                ImGui.Separator();
                ImGui.Text("Rotation");
                    ImGui.InputFloat("Pitch", ref HPpitch, 0.1f, 1f, "%.4f", ImGuiInputTextFlags.CharsDecimal);
                    ImGui.InputFloat("Yaw", ref HPyaw, 0.1f, 1f, "%.4f", ImGuiInputTextFlags.CharsDecimal);
                    ImGui.InputFloat("Roll", ref HProll, 0.1f, 1f, "%.4f", ImGuiInputTextFlags.CharsDecimal);
                ImGui.Separator();
                if (!isFix)
                {
                    ImGui.Text("Axis");
                    ImGui.InputFloat("X##axisX", ref HPaxisX, 0.01f, 0.25f, "%.5f", ImGuiInputTextFlags.CharsDecimal);
                    ImGui.InputFloat("Y##axisY", ref HPaxisY, 0.01f, 0.25f, "%.5f", ImGuiInputTextFlags.CharsDecimal);
                    ImGui.InputFloat("Z##axisZ", ref HPaxisZ, 0.01f, 0.25f, "%.5f", ImGuiInputTextFlags.CharsDecimal);
                    ImGui.InputFloat("Min", ref HPmin, 0.1f, 1f, "%.4f", ImGuiInputTextFlags.CharsDecimal);
                    ImGui.InputFloat("Max", ref HPmax, 0.1f, 1f, "%.4f", ImGuiInputTextFlags.CharsDecimal);
                    ImGui.Separator();
                }
                if (ImGui.Button("Apply"))
                {
                    hpEditing.Position = new Vector3(HPx, HPy, HPz);
                    hpEditing.Orientation = Matrix4.CreateFromEulerAngles(
                        MathHelper.DegreesToRadians((double)HPpitch),
                        MathHelper.DegreesToRadians((double)HPyaw),
                        MathHelper.DegreesToRadians((double)HProll)
                    );
                    if (!isFix)
                    {
                        var rev = (RevoluteHardpointDefinition)hpEditing;
                        if(HPmin > HPmax)
                        {
                            var t = HPmin;
                            HPmin = HPmax;
                            HPmax = t;
                            popups.OpenPopup("Warning");
                        }
                        rev.Min = MathHelper.DegreesToRadians(HPmin);
                        rev.Max = MathHelper.DegreesToRadians(HPmax);
                        rev.Axis = new Vector3(HPaxisX, HPaxisY, HPaxisZ);
                    }
                    hpEditOpen = false;
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    hpEditOpen = false;
                }
                editingGizmo.Override = Matrix4.CreateFromEulerAngles(
                            MathHelper.DegreesToRadians((double)HPpitch),
                            MathHelper.DegreesToRadians((double)HPyaw),
                            MathHelper.DegreesToRadians((double)HProll)
                        ) * Matrix4.CreateTranslation(HPx, HPy, HPz);
                editingGizmo.EditingMin = MathHelper.DegreesToRadians(HPmin);
                editingGizmo.EditingMax = MathHelper.DegreesToRadians(HPmax);
                ImGui.End();
            }
            if (hpEditOpen == false)
            {
                hpEditing = null;
                editingGizmo.Override = null;
                editingGizmo = null;
            }
        }
    }
}
