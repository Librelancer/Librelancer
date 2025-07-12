// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Utf.Cmp;
using LibreLancer.ImUI;
using LibreLancer.World;

namespace LancerEdit
{
    public partial class ModelViewer
    {
        Hardpoint hpEditing;
        void DeleteHardpoint(Hardpoint hpDelete, List<Hardpoint> hpDeleteFrom)
        {
            popups.MessageBox("Confirm?", $"Are you sure you wish to delete '{hpDelete.Name}'?",
                false, MessageBoxButtons.YesNo, r =>
                {
                    if (r == MessageBoxResponse.Yes)
                    {
                        hpDeleteFrom.Remove(hpDelete);
                        var gz = gizmos.Where((x) => x.Hardpoint == hpDelete).First();
                        if (hpDelete == hpEditing) hpEditing = null;
                        gizmos.Remove(gz);
                        OnDirtyHp();
                    }
                });
        }

        IEnumerable<string> HardpointNames() => gizmos.Select(gz => gz.Hardpoint.Definition.Name);

        string GetDupName(string name)
        {
            foreach(var hp in HardpointInformation.All())
            {
                if(name.StartsWith(hp.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (hp.Autoname == HpNaming.None)
                        return GetCopyName(name);
                    else if (hp.Autoname == HpNaming.Letter)
                        return (hp.Name + HardpointInformation.GetHpLettering(hp.Name, HardpointNames()));
                    else if (hp.Autoname == HpNaming.Number)
                        return (hp.Name + HardpointInformation.GetHpNumbering(hp.Name, HardpointNames())
                            .ToString("00"));
                }
            }
            return GetCopyName(name);
        }
        string GetCopyName(string name)
        {
            var src = name + "_Copy";
            foreach(var gz in gizmos)
            {
                if (gz.Hardpoint.Definition.Name.Equals(src, StringComparison.OrdinalIgnoreCase))
                    return GetCopyName(src);
            }
            return src;
        }

        void NewHardpoint(bool newIsFixed, RigidModelPart addTo)
        {
            popups.OpenPopup(new NewHardpointPopup(newIsFixed,
                HardpointNames,
                name =>
                {
                    HardpointDefinition def;
                    if (newIsFixed) def = new FixedHardpointDefinition(name);
                    else def = new RevoluteHardpointDefinition(name);
                    var hp = new Hardpoint(def, addTo);
                    gizmos.Add(new HardpointGizmo(hp, addTo));
                    addTo.Hardpoints.Add(hp);
                    OnDirtyHp();
                }));
        }

        bool hpEditOpen = false;
        HardpointGizmo editingGizmo;
        float HPpitch, HPyaw, HProll;
        float HPx, HPy, HPz;
        float HPaxisX, HPaxisY, HPaxisZ;
        float HPmin, HPmax;
        void SetHardpointValues()
        {
            var hp = hpEditing.Definition;
            HPx = hp.Position.X;
            HPy = hp.Position.Y;
            HPz = hp.Position.Z;
            var euler = hp.Orientation.GetEulerDegrees();
            HPpitch = euler.X;
            HPyaw = euler.Y;
            HProll = euler.Z;
            if (hp is RevoluteHardpointDefinition)
            {
                var rev = (RevoluteHardpointDefinition)hp;
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
                editingGizmo = gizmos.First((x) => x.Hardpoint == hpEditing);
                hpEditOpen = true;
                hpFirst = true;
                SetHardpointValues();
            }
            if (ImGui.Begin("Hardpoint Editor##" + Unique, ref hpEditOpen, hpFirst ? ImGuiWindowFlags.AlwaysAutoResize : ImGuiWindowFlags.None))
            {
                hpFirst = false;
                ImGui.Text(hpEditing.Name);
                bool isFix = hpEditing.Definition is FixedHardpointDefinition;
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
                    var hp = hpEditing.Definition;
                    hp.Position = new Vector3(HPx, HPy, HPz);
                    hp.Orientation = MathHelper.MatrixFromEulerDegrees(HPpitch, HPyaw, HProll);
                    if (!isFix)
                    {
                        var rev = (RevoluteHardpointDefinition)hp;
                        if(HPmin > HPmax)
                        {
                            var t = HPmin;
                            HPmin = HPmax;
                            HPmax = t;
                            popups.MessageBox("Warning", "Min was bigger than max, swapped.");
                        }
                        rev.Min = MathHelper.DegreesToRadians(HPmin);
                        rev.Max = MathHelper.DegreesToRadians(HPmax);
                        rev.Axis = new Vector3(HPaxisX, HPaxisY, HPaxisZ);
                    }
                    hpEditOpen = false;
                    OnDirtyHp();
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    hpEditOpen = false;
                }
                editingGizmo.Override = MathHelper.MatrixFromEulerDegrees(HPpitch, HPyaw, HProll) *
                                        Matrix4x4.CreateTranslation(HPx, HPy, HPz);
                editingGizmo.EditingMin = MathHelper.DegreesToRadians(HPmin);
                editingGizmo.EditingMax = MathHelper.DegreesToRadians(HPmax);
            }
            ImGui.End();
            if (hpEditOpen == false)
            {
                hpEditing = null;
                editingGizmo.Override = null;
                editingGizmo = null;
            }
        }

        bool ManipulateHardpoint(ICamera camera)
        {
            if (hpEditing == null || hpEditOpen == false)
                return false;
            var v = camera.View;
            var p = camera.Projection;
            var parentMatrix = (hpEditing.Parent.LocalTransform.Matrix() * GetModelMatrix());
            Matrix4x4.Invert(parentMatrix, out var invParentMatrix);

            var mode = ImGui.GetIO().KeyCtrl ? GuizmoMode.WORLD : GuizmoMode.LOCAL;
            Matrix4x4 delta = Matrix4x4.Identity;
            var hpTransform =  MathHelper.MatrixFromEulerDegrees(HPpitch, HPyaw, HProll) *
                             Matrix4x4.CreateTranslation(HPx, HPy, HPz);
            var editingTransform = hpTransform * parentMatrix;
            GuizmoOp op;
            if ((op = ImGuizmo.Manipulate(ref v, ref p, GuizmoOperation.TRANSLATE | GuizmoOperation.ROTATE_AXIS, mode,
                    ref editingTransform, out delta)) != GuizmoOp.Nothing && !delta.IsIdentity)
            {
                var invTransform = editingTransform * invParentMatrix;
                var rot = invTransform.GetEulerDegrees();
                var pos = Vector3.Transform(Vector3.Zero, invTransform);
                if (op == GuizmoOp.Translate)
                {
                    HPx = pos.X;
                    HPy = pos.Y;
                    HPz = pos.Z;
                }
                else if (op == GuizmoOp.Rotate)
                {
                    HPpitch = rot.X;
                    HPyaw = rot.Y;
                    HProll = rot.Z;
                }
            }
            return ImGuizmo.IsOver() || ImGuizmo.IsUsing();
        }
    }
}
