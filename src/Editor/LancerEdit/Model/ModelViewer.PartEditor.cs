/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Collections.Generic;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Utf;
using LibreLancer.Utf.Cmp;
namespace LancerEdit
{
    public partial class ModelViewer
    {
        List<HardpointEditor> partEditors = new List<HardpointEditor>();
        void AddPartEditor(AbstractConstruct p)
        {
            partEditors.Add(new HardpointEditor(p));
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
            var cmp = (CmpFile)drawable;
            FixConstructor fix = null;
            RevConstructor rev = null;
            PrisConstructor pris = null;
            SphereConstructor sphere = null;

            foreach (var con in cmp.Constructs)
            {
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
        void ReplaceConstruct(ConstructNode c, AbstractConstruct newc)
        {
            var cmp = (CmpFile)drawable;
            for (int i = 0; i < cmp.Constructs.Count; i++)
            {
                if (cmp.Constructs[i] == c.Con)
                {
                    cmp.Constructs[i] = newc;
                    break;
                }
            }
            foreach(var gz in gizmos) {
                if (gz.Parent == c.Con) gz.Parent = newc;
            }
            if (addConstruct == c.Con) addConstruct = newc;
            c.Con = newc;
            cmp.Constructs.ClearParents();
        }
    }
    class HardpointEditor
    {
        public bool Open {
            get {
                return editingPart != null;
            }
        }
        public HardpointEditor(AbstractConstruct part)
        {
            editingPart = part;
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
            if(ImGui.BeginWindow("Part Editor##" + editingPart.ChildName,ref partEditorOpen,partFirst ? WindowFlags.AlwaysAutoResize : WindowFlags.Default)) {
                partFirst = false;
                ImGui.Text(editingPart.ChildName);
                ImGui.Text("Type: " + ModelViewer.ConType(editingPart));
                if (ImGui.Button("Reset")) SetPartValues();
                ImGui.Separator();
                ImGui.Text("Position");
                fixed (float* hpx = &partX)
                    ImGuiNative.igInputFloat("X##posX", hpx, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                fixed (float* hpy = &partY)
                    ImGuiNative.igInputFloat("Y##posY", hpy, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                fixed (float* hpz = &partZ)
                    ImGuiNative.igInputFloat("Z##posZ", hpz, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                ImGui.Separator();
                ImGui.Text("Rotation");
                fixed (float* hpp = &partPitch)
                    ImGuiNative.igInputFloat("Pitch", hpp, 0.1f, 1f, 7, InputTextFlags.CharsDecimal);
                fixed (float* hpy = &partYaw)
                    ImGuiNative.igInputFloat("Yaw", hpy, 0.1f, 1f, 7, InputTextFlags.CharsDecimal);
                fixed (float* hpr = &partRoll)
                    ImGuiNative.igInputFloat("Roll", hpr, 0.1f, 1f, 7, InputTextFlags.CharsDecimal);
                ImGui.Separator();
                if (!(editingPart is FixConstruct))
                {
                    ImGui.Text("Offset");
                    fixed (float* hpx = &partOX)
                        ImGuiNative.igInputFloat("X##posX", hpx, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                    fixed (float* hpy = &partOY)
                        ImGuiNative.igInputFloat("Y##posY", hpy, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                    fixed (float* hpz = &partOZ)
                        ImGuiNative.igInputFloat("Z##posZ", hpz, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                    ImGui.Separator();
                }
                if((editingPart is RevConstruct) || (editingPart is PrisConstruct)) {
                    ImGui.Text("Axis");
                    fixed (float* hpx = &partAxX)
                        ImGuiNative.igInputFloat("X##posX", hpx, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                    fixed (float* hpy = &partAxY)
                        ImGuiNative.igInputFloat("Y##posY", hpy, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                    fixed (float* hpz = &partAxZ)
                        ImGuiNative.igInputFloat("Z##posZ", hpz, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                    fixed (float* axmn = &partMin)
                        ImGuiNative.igInputFloat("Min", axmn, 0.1f, 1f, 4, InputTextFlags.CharsDecimal);
                    fixed (float* axmx = &partMax)
                        ImGuiNative.igInputFloat("Max", axmx, 0.1f, 1f, 4, InputTextFlags.CharsDecimal);
                    if (ImGui.Button("0")) partPreview = 0;
                    ImGui.SameLine();
                    ImGui.PushItemWidth(-1);
                    if (partMax > partMin)
                        ImGui.SliderFloat("Preview", ref partPreview, partMin, partMax, "%f", 1);
                    else
                        ImGui.SliderFloat("Preview", ref partPreview, partMax, partMin, "%f", 1);
                    ImGui.PopItemWidth();
                    ImGui.Separator();
                }
                if(editingPart is SphereConstruct) {
                    fixed (float* hpx = &min1)
                        ImGuiNative.igInputFloat("Min1", hpx, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                    fixed (float* hpx = &max1)
                        ImGuiNative.igInputFloat("Max1", hpx, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                    fixed (float* hpx = &min2)
                        ImGuiNative.igInputFloat("Min2", hpx, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                    fixed (float* hpx = &max2)
                        ImGuiNative.igInputFloat("Max2", hpx, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                    fixed (float* hpx = &min3)
                        ImGuiNative.igInputFloat("Min3", hpx, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                    fixed (float* hpx = &max3)
                        ImGuiNative.igInputFloat("Max3", hpx, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                    ImGui.Separator();
                }
                var jointPreview = Matrix4.Identity;
                if(editingPart is RevConstruct) {
                    jointPreview = Matrix4.CreateFromAxisAngle(
                    new Vector3(partAxX, partAxY, partAxZ),
                        MathHelper.DegreesToRadians(partPreview));
                } else if (editingPart is PrisConstruct) {
                    var translate = new Vector3(partAxX, partAxY, partAxZ).Normalized() * partPreview;
                    jointPreview = Matrix4.CreateTranslation(translate);
                }
                editingPart.OverrideTransform = Matrix4.CreateFromEulerAngles(
                MathHelper.DegreesToRadians(partPitch),
                MathHelper.DegreesToRadians(partYaw),
                MathHelper.DegreesToRadians(partRoll)) * jointPreview *
                    Matrix4.CreateTranslation(new Vector3(partX, partY, partZ) + new Vector3(partOX, partOY, partOZ));
                if(ImGui.Button("Apply")) {
                    editingPart.Origin = new Vector3(partX, partY, partZ);
                    editingPart.Rotation = Matrix4.CreateFromEulerAngles(
                        MathHelper.DegreesToRadians(partPitch),
                        MathHelper.DegreesToRadians(partYaw),
                        MathHelper.DegreesToRadians(partRoll)
                    );
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
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel")) partEditorOpen = false;
                ImGui.EndWindow();
            }
            if (!partEditorOpen) {
                editingPart.OverrideTransform = null;
                editingPart = null;
            }

        }
    }
}
