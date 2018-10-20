// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and confiditons defined in
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
            ImGui.InputText("##hpname", newHpBuffer.Pointer, (uint)newHpBuffer.Size, InputTextFlags.Default, newHpBuffer.Callback);
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
                ImGui.Text("Hardpoint with that name already exists.", new Vector4(1, 0, 0, 1));
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
            if (ImGui.BeginWindow("Hardpoint Editor##" + Unique, ref hpEditOpen, hpFirst ? WindowFlags.AlwaysAutoResize : WindowFlags.Default))
            {
                hpFirst = false;
                ImGui.Text(hpEditing.Name);
                bool isFix = hpEditing is FixedHardpointDefinition;
                ImGui.Text("Type: " + (isFix ? "Fixed" : "Revolute"));
                if (ImGui.Button("Reset")) SetHardpointValues();
                ImGui.Separator();
                ImGui.Text("Position");
                fixed (float* hpx = &HPx)
                    ImGuiNative.igInputFloat("X##posX", hpx, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                fixed (float* hpy = &HPy)
                    ImGuiNative.igInputFloat("Y##posY", hpy, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                fixed (float* hpz = &HPz)
                    ImGuiNative.igInputFloat("Z##posZ", hpz, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                ImGui.Separator();
                ImGui.Text("Rotation");
                fixed (float* hpp = &HPpitch)
                    ImGuiNative.igInputFloat("Pitch", hpp, 0.1f, 1f, 4, InputTextFlags.CharsDecimal);
                fixed (float* hpy = &HPyaw)
                    ImGuiNative.igInputFloat("Yaw", hpy, 0.1f, 1f, 4, InputTextFlags.CharsDecimal);
                fixed (float* hpr = &HProll)
                    ImGuiNative.igInputFloat("Roll", hpr, 0.1f, 1f, 4, InputTextFlags.CharsDecimal);
                ImGui.Separator();
                if (!isFix)
                {
                    ImGui.Text("Axis");
                    fixed (float* axx = &HPaxisX)
                        ImGuiNative.igInputFloat("X##axisX", axx, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                    fixed (float* axy = &HPaxisY)
                        ImGuiNative.igInputFloat("Y##axisY", axy, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                    fixed (float* axz = &HPaxisZ)
                        ImGuiNative.igInputFloat("Z##axisZ", axz, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                    fixed (float* axmn = &HPmin)
                        ImGuiNative.igInputFloat("Min", axmn, 0.1f, 1f, 4, InputTextFlags.CharsDecimal);
                    fixed (float* axmx = &HPmax)
                        ImGuiNative.igInputFloat("Max", axmx, 0.1f, 1f, 4, InputTextFlags.CharsDecimal);
                    ImGui.Separator();
                }
                if (ImGui.Button("Apply"))
                {
                    hpEditing.Position = new Vector3(HPx, HPy, HPz);
                    hpEditing.Orientation = Matrix4.CreateFromEulerAngles(
                        MathHelper.DegreesToRadians(HPpitch),
                        MathHelper.DegreesToRadians(HPyaw),
                        MathHelper.DegreesToRadians(HProll)
                    );
                    if (!isFix)
                    {
                        var rev = (RevoluteHardpointDefinition)hpEditing;
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
                            MathHelper.DegreesToRadians(HPpitch),
                            MathHelper.DegreesToRadians(HPyaw),
                            MathHelper.DegreesToRadians(HProll)
                        ) * Matrix4.CreateTranslation(HPx, HPy, HPz);
                editingGizmo.EditingMin = MathHelper.DegreesToRadians(HPmin);
                editingGizmo.EditingMax = MathHelper.DegreesToRadians(HPmax);
                ImGui.EndWindow();
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
