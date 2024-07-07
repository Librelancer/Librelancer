// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Collections.Generic;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.ContentEdit.Model;
using LibreLancer.Utf;
using LibreLancer.Utf.Cmp;

namespace LancerEdit
{
    public partial class ModelViewer
    {
        List<HardpointEditor> partEditors = new List<HardpointEditor>();
        void AddPartEditor(AbstractConstruct p)
        {
            partEditors.Add(new HardpointEditor(p,this));
        }

        void PartEditor()
        {
            List<HardpointEditor> toRemove = new List<HardpointEditor>();
            foreach(var ed in partEditors) {
                ed.PartEditor();
                if (!ed.Open)
                    toRemove.Add(ed);
            }
            foreach (var ed in toRemove)
                partEditors.Remove(ed);
        }

        void WriteConstructs()
        {
            FixConstructor fix = null;
            RevConstructor rev = null;
            PrisConstructor pris = null;
            SphereConstructor sphere = null;

            foreach (var p in vmsModel.AllParts)
            {
                var con = p.Construct;
                if (con is FixConstruct)
                {
                    if (fix == null) fix = new FixConstructor();
                    fix.Add((FixConstruct)con);
                }
                else if (con is RevConstruct)
                {
                    if (rev == null) rev = new RevConstructor();
                    rev.Add((RevConstruct)con);
                }
                else if (con is PrisConstruct)
                {
                    if (pris == null) pris = new PrisConstructor();
                    pris.Add((PrisConstruct)con);
                }
                else if (con is SphereConstruct)
                {
                    if (sphere == null) sphere = new SphereConstructor();
                    sphere.Add((SphereConstruct)con);
                }
            }

            hprefs.Cons.Children = new List<LUtfNode>();
            if(fix != null) {
                hprefs.Cons.Children.Add(new LUtfNode() {
                    Name = "Fix", Parent = hprefs.Cons, Data = fix.GetData()
                });
            }
            if(rev != null) {
                hprefs.Cons.Children.Add(new LUtfNode() {
                    Name = "Rev", Parent = hprefs.Cons, Data = rev.GetData()
                });
            }
            if(pris != null) {
                hprefs.Cons.Children.Add(new LUtfNode() {
                    Name = "Pris", Parent = hprefs.Cons, Data = pris.GetData()
                });
            }
            if(sphere != null) {
                hprefs.Cons.Children.Add(new LUtfNode() {
                    Name = "Sphere", Parent = hprefs.Cons, Data = sphere.GetData()
                });
            }
        }

    }
    class HardpointEditor
    {
        public bool Open {
            get {
                return editingPart != null;
            }
        }
        ModelViewer mv;
        public HardpointEditor(AbstractConstruct part, ModelViewer mv)
        {
            editingPart = part;
            this.mv = mv;
        }
        AbstractConstruct editingPart = null;

        bool partEditorOpen = false;
        bool partFirst = false;
        float partX, partY, partZ;
        float partPitch, partYaw, partRoll;
        float partOX, partOY, partOZ;
        float partAxX, partAxY, partAxZ;
        float partMin, partMax;
        float partPreview = 0;
        float min1, max1, min2, max2, min3, max3;
        void SetPartValues()
        {
            partX = editingPart.Origin.X;
            partY = editingPart.Origin.Y;
            partZ = editingPart.Origin.Z;
            var euler = editingPart.Rotation.GetEulerDegrees();
            partPitch = euler.X;
            partYaw = euler.Y;
            partRoll = euler.Z;
            partPreview = 0;
            if(editingPart is RevConstruct) {
                var rev = (RevConstruct)editingPart;
                partOX = rev.Offset.X;
                partOY = rev.Offset.Y;
                partOZ = rev.Offset.Z;
                partAxX = rev.AxisRotation.X;
                partAxY = rev.AxisRotation.Y;
                partAxZ = rev.AxisRotation.Z;
                partMin = MathHelper.RadiansToDegrees(rev.Min);
                partMax = MathHelper.RadiansToDegrees(rev.Max);
            }
            else if(editingPart is PrisConstruct) {
                var pris = (PrisConstruct)editingPart;
                partOX = pris.Offset.X;
                partOY = pris.Offset.Y;
                partOZ = pris.Offset.Z;
                partAxX = pris.AxisTranslation.X;
                partAxY = pris.AxisTranslation.Y;
                partAxZ = pris.AxisTranslation.Z;
                partMin = pris.Min;
                partMax = pris.Max;
            } else if (editingPart is SphereConstruct) {
                var sphere = ((SphereConstruct)editingPart);
                min1 = sphere.Min1;
                max1 = sphere.Max1;
                min2 = sphere.Min2;
                max2 = sphere.Max2;
                min3 = sphere.Min3;
                max3 = sphere.Max3;
                partOX = partOY = partOZ = 0;
            } else {
                partOX = partOY = partOZ = 0;
            }
        }
        public unsafe void PartEditor()
        {
            if (editingPart == null) return;
            if(editingPart != null && !partEditorOpen) {
                partEditorOpen = true;
                partFirst = true;
                SetPartValues();
            }
            if(ImGui.Begin("Part Editor##" + editingPart.ChildName,ref partEditorOpen,partFirst ? ImGuiWindowFlags.AlwaysAutoResize : ImGuiWindowFlags.None)) {
                partFirst = false;
                ImGui.Text(editingPart.ChildName);
                ImGui.Text("Type: " + ModelViewer.ConType(editingPart));
                if (ImGui.Button("Reset")) SetPartValues();
                ImGui.Separator();
                ImGui.Text("Position");
                    ImGui.InputFloat("X##posX", ref partX, 0.01f, 0.25f, "%.5f", ImGuiInputTextFlags.CharsDecimal);
                    ImGui.InputFloat("Y##posY", ref partY, 0.01f, 0.25f, "%.5f", ImGuiInputTextFlags.CharsDecimal);
                    ImGui.InputFloat("Z##posZ", ref partZ, 0.01f, 0.25f, "%.5f", ImGuiInputTextFlags.CharsDecimal);
                ImGui.Separator();
                ImGui.Text("Rotation");
                    ImGui.InputFloat("Pitch", ref partPitch, 0.1f, 1f, "%.7f", ImGuiInputTextFlags.CharsDecimal);
                    ImGui.InputFloat("Yaw", ref partYaw, 0.1f, 1f, "%.7f", ImGuiInputTextFlags.CharsDecimal);
                    ImGui.InputFloat("Roll", ref partRoll, 0.1f, 1f, "%.7f", ImGuiInputTextFlags.CharsDecimal);
                ImGui.Separator();
                if (!(editingPart is FixConstruct))
                {
                    ImGui.Text("Offset");
                        ImGui.InputFloat("X##offX", ref partOX, 0.01f, 0.25f, "%.5f", ImGuiInputTextFlags.CharsDecimal);
                        ImGui.InputFloat("Y##offY", ref partOY, 0.01f, 0.25f, "%.5f", ImGuiInputTextFlags.CharsDecimal);
                        ImGui.InputFloat("Z##offZ", ref partOZ, 0.01f, 0.25f, "%.5f", ImGuiInputTextFlags.CharsDecimal);
                    ImGui.Separator();
                }
                if((editingPart is RevConstruct) || (editingPart is PrisConstruct)) {
                    ImGui.Text("Axis");
                        ImGui.InputFloat("X##axX", ref partAxX, 0.01f, 0.25f, "%.5f", ImGuiInputTextFlags.CharsDecimal);
                        ImGui.InputFloat("Y##axY", ref partAxY, 0.01f, 0.25f, "%.5f", ImGuiInputTextFlags.CharsDecimal);
                        ImGui.InputFloat("Z##axZ", ref partAxZ, 0.01f, 0.25f, "%.5f", ImGuiInputTextFlags.CharsDecimal);
                        ImGui.InputFloat("Min", ref partMin, 0.1f, 1f, "%.4f", ImGuiInputTextFlags.CharsDecimal);
                        ImGui.InputFloat("Max", ref partMax, 0.1f, 1f, "%.4f", ImGuiInputTextFlags.CharsDecimal);
                    if (ImGui.Button("0")) partPreview = 0;
                    ImGui.SameLine();
                    ImGui.PushItemWidth(-1);
                    if (partMax > partMin)
                        ImGui.SliderFloat("Preview", ref partPreview, partMin, partMax, "%f");
                    else
                        ImGui.SliderFloat("Preview", ref partPreview, partMax, partMin, "%f");
                    ImGui.PopItemWidth();
                    ImGui.Separator();
                }
                if(editingPart is SphereConstruct) {
                        ImGui.InputFloat("Min1", ref min1, 0.01f, 0.25f, "%.5f", ImGuiInputTextFlags.CharsDecimal);
                        ImGui.InputFloat("Max1", ref max1, 0.01f, 0.25f, "%.5f", ImGuiInputTextFlags.CharsDecimal);
                        ImGui.InputFloat("Min2", ref min2, 0.01f, 0.25f, "%.5f", ImGuiInputTextFlags.CharsDecimal);
                        ImGui.InputFloat("Max2", ref max2, 0.01f, 0.25f, "%.5f", ImGuiInputTextFlags.CharsDecimal);
                        ImGui.InputFloat("Min3", ref min3, 0.01f, 0.25f, "%.5f", ImGuiInputTextFlags.CharsDecimal);
                        ImGui.InputFloat("Max3", ref max3, 0.01f, 0.25f, "%.5f", ImGuiInputTextFlags.CharsDecimal);
                    ImGui.Separator();
                }
                var jointPreview = Matrix4x4.Identity;
                if(editingPart is RevConstruct) {
                    jointPreview = Matrix4x4.CreateFromAxisAngle(
                    new Vector3(partAxX, partAxY, partAxZ),
                        MathHelper.DegreesToRadians(partPreview));
                } else if (editingPart is PrisConstruct) {
                    var translate = new Vector3(partAxX, partAxY, partAxZ).Normalized() * partPreview;
                    jointPreview = Matrix4x4.CreateTranslation(translate);
                }

                editingPart.OverrideTransform = Transform3D.FromMatrix(
                    MathHelper.MatrixFromEulerDegrees(partPitch, partYaw, partRoll) * jointPreview *
                    Matrix4x4.CreateTranslation(new Vector3(partX, partY, partZ) +
                                                new Vector3(partOX, partOY, partOZ)));
                if(ImGui.Button("Apply")) {
                    editingPart.Origin = new Vector3(partX, partY, partZ);
                    editingPart.Rotation = MathHelper.QuatFromEulerDegrees(partPitch, partYaw, partRoll);
                    if(editingPart is RevConstruct) {
                        var rev = (RevConstruct)editingPart;
                        rev.Offset = new Vector3(partOX, partOY, partOZ);
                        rev.AxisRotation = new Vector3(partAxX, partAxY, partAxZ);
                        rev.Min = MathHelper.DegreesToRadians(partMin);
                        rev.Max = MathHelper.DegreesToRadians(partMax);
                    }
                    if(editingPart is PrisConstruct) {
                        var pris = (PrisConstruct)editingPart;
                        pris.Offset = new Vector3(partOX, partOY, partOZ);
                        pris.AxisTranslation = new Vector3(partAxX, partAxY, partAxZ);
                        pris.Min = partMin;
                        pris.Max = partMax;
                    }
                    if(editingPart is SphereConstruct) {
                        var sphere = (SphereConstruct)editingPart;
                        sphere.Offset = new Vector3(partOX, partOY, partOZ);
                        sphere.Min1 = min1;
                        sphere.Max1 = max1;
                        sphere.Min2 = min2;
                        sphere.Max2 = max2;
                        sphere.Min3 = min3;
                        sphere.Max3 = max3;
                    }
                    mv.OnDirtyPart();
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel")) partEditorOpen = false;
                ImGui.End();
            }
            if (!partEditorOpen) {
                editingPart.OverrideTransform = null;
                editingPart = null;
            }

        }
    }
}
