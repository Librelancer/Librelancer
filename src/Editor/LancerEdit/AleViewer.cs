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
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.Fx;
using LibreLancer.Utf.Ale;
using ImGuiNET;
namespace LancerEdit
{
    public partial class AleViewer : EditorTab
    {
        //GL
        Viewport3D aleViewport;
        RenderState rstate;
        CommandBuffer buffer;
        Billboards billboards;
        PolylineRender polyline;
        PhysicsDebugRenderer debug;
        //Tab
        string name;
        ParticleLibrary plib;
        int lastEffect = 0;
        int currentEffect = 0;

        float sparam = 1;
        string[] effectNames;
        public AleViewer(string title, string name, AleFile ale, MainWindow main)
        {
            plib = new ParticleLibrary(main.Resources, ale);
            effectNames = new string[plib.Effects.Count];
            for (int i = 0; i < effectNames.Length; i++)
                effectNames[i] = string.Format("{0} (0x{1:X})", plib.Effects[i].Name, plib.Effects[i].CRC);
            Title = title;
            this.name = name;
            this.rstate = main.RenderState;
            aleViewport = new Viewport3D(rstate, main.Viewport);
            aleViewport.Zoom = 200;
            aleViewport.ZoomStep = 25;
            buffer = main.Commands;
            billboards = main.Billboards;
            polyline = main.Polyline;
            debug = main.DebugRender;
            SetupRender(0);
        }
        ParticleEffectInstance instance;
        void SetupRender(int index)
        {
            selectedReference = null;
            instance = new ParticleEffectInstance(plib.Effects[index]);
            instance.Resources = plib.Resources;
        }

        bool[] openTabs = new bool[] { false, false };
        void TabButton(string name, int idx)
        {
            if (TabHandler.VerticalTab(name, openTabs[idx]))
            {
                if (!openTabs[idx])
                {
                    for (int i = 0; i < openTabs.Length; i++) openTabs[i] = false;
                    openTabs[idx] = true;
                }
                else
                    openTabs[idx] = false;
            }
        }
        void TabButtons()
        {
            ImGuiNative.igBeginGroup();
            TabButton("Hierachy", 0);
            //TabButton("Library", 1);
            ImGuiNative.igEndGroup();
            ImGui.SameLine();
        }

        public override void Draw()
        {
            bool doTabs = false;
            foreach (var t in openTabs) if (t) { doTabs = true; break; }
            var contentw = ImGui.GetContentRegionAvailableWidth();
            if (doTabs)
            {
                ImGui.Columns(2, "##alecolumns", true);
                ImGui.BeginChild("##leftpanel");
                if (openTabs[0]) NodePanel();
                ImGui.EndChild();
                ImGui.NextColumn();
            }
            TabButtons();
            ImGui.SameLine();
            ImGui.BeginChild("##renderpanel");
            //Fx management
            lastEffect = currentEffect;
            ImGui.Text("Effect:");
            ImGui.SameLine();
            ImGui.Combo("##effect", ref currentEffect, effectNames);
            if (currentEffect != lastEffect) SetupRender(currentEffect);
            ImGui.SameLine();
            ImGui.Button("+");
            ImGui.SameLine();
            ImGui.Button("-");
            ImGui.Separator();
            //Viewport
            ImGui.BeginChild("##renderchild");
            //Generate render target
            aleViewport.Begin();
            DrawGL(aleViewport.RenderWidth, aleViewport.RenderHeight);
            //Display + Camera controls
            aleViewport.End();
            //Action Bar
            if (ImGui.Button("Reset"))
            {
                instance.Reset();
            }
            ImGui.SameLine();
            ImGui.Text(string.Format("T: {0:0.000}", instance.GlobalTime));
            ImGui.EndChild();
            ImGui.EndChild();
        }

        void NodePanel()
        {
            if (selectedReference != null)
            {
                NodeOptions();
                ImGui.Separator();
            }
            NodeHierachy();
        }
        NodeReference selectedReference = null;
        void NodeHierachy()
        {
            var enabledColor = (Vector4)ImGui.GetStyle().GetColor(ColorTarget.Text);
            var disabledColor = (Vector4)ImGui.GetStyle().GetColor(ColorTarget.TextDisabled);

            foreach (var reference in instance.Effect.References)
            {
                int j = 0;
                if (reference.Parent == null)
                    DoNode(reference, j++, enabledColor, disabledColor);
            }
        }

        void NodeOptions()
        {
            ImGui.Text(string.Format("Selected: {0}", selectedReference.Node.NodeName));
            ImGui.Text(selectedReference.Node.Name);
            //Node enabled
            var enabled = instance.NodeEnabled(selectedReference);
            var wasEnabled = enabled;
            ImGui.Checkbox("Enabled", ref enabled);
            if (enabled != wasEnabled) instance.EnableStates[selectedReference] = enabled;
            //Normals?

            //Textures?

            //Debug volumes?
        }

        void DoNode(NodeReference reference, int idx, Vector4 enabled, Vector4 disabled)
        {
            var col = instance.NodeEnabled(reference) ? enabled : disabled;
            string label = null;
            if (reference.IsAttachmentNode)
                label = string.Format("Attachment##{0}", idx);
            else
                label = string.Format("{0}##{1}", reference.Node.NodeName, idx);
            ImGui.PushStyleColor(ColorTarget.Text, col);
            string icon;
            Color4 color;
            NodeIcon(reference.Node, out icon, out color);
            if (reference.Children.Count > 0)
            {
                if (ImGui.TreeNodeEx(ImGuiExt.Pad(label), TreeNodeFlags.OpenOnDoubleClick | TreeNodeFlags.OpenOnArrow))
                {
                    Theme.RenderTreeIcon(label.Split('#')[0], icon, color);
                    int j = 0;
                    foreach (var child in reference.Children)
                        DoNode(child, j++, enabled, disabled);
                    ImGui.TreePop();
                }
                else
                    Theme.RenderTreeIcon(label.Split('#')[0], icon, color);
            }
            else
            {
                Theme.Icon(icon, color);
                ImGui.SameLine();
                if (ImGui.Selectable(label, selectedReference == reference))
                {
                    selectedReference = reference;
                }
            }
            ImGui.PopStyleColor();
        }

        void DrawGL(int renderWidth, int renderHeight)
        {
            var cam = new LookAtCamera();
            cam.Update(renderWidth, renderHeight, new Vector3(aleViewport.Zoom, 0, 0), Vector3.Zero);
            buffer.StartFrame(rstate);
            polyline.SetCamera(cam);
            billboards.Begin(cam, buffer);
            debug.StartFrame(cam, rstate);
            instance.Draw(polyline, billboards, debug, transform, sparam);
            polyline.FrameEnd();
            billboards.End();
            buffer.DrawOpaque(rstate);
            rstate.DepthWrite = false;
            buffer.DrawTransparent(rstate);
            rstate.DepthWrite = true;
            debug.Render();
        }

        public override void DetectResources(List<MissingReference> missing, List<uint> matrefs, List<string> texrefs)
        {
            foreach (var reference in instance.Effect.References)
            {
                if (reference.Node is FxBasicAppearance)
                {
                    var node = reference.Node;
                    var fx = (FxBasicAppearance)reference.Node;
                    if (fx.Texture != null && !ResourceDetection.HasTexture(texrefs, fx.Texture)) texrefs.Add(fx.Texture);
                    if (fx.Texture != null && plib.Resources.FindTexture(fx.Texture) == null)
                    {
                        var str = "Texture: " + fx.Texture; //TODO: This is wrong - handle properly
                        if (!ResourceDetection.HasMissing(missing, str)) missing.Add(new MissingReference(
                            str, string.Format("{0}: {1} ({2})", instance.Effect.Name, node.NodeName, node.Name)));
                    }
                }
            }
        }

        public override void OnHotkey(Hotkeys hk)
        {
            if (hk == Hotkeys.ResetViewport) aleViewport.ResetControls();
        }

        Matrix4 transform = Matrix4.Identity;
        public override void Update(double elapsed)
        {
            transform = Matrix4.CreateRotationX(aleViewport.Rotation.Y) * Matrix4.CreateRotationY(aleViewport.Rotation.X);
            instance.Update(TimeSpan.FromSeconds(elapsed), transform, sparam);
        }

        public override void Dispose()
        {
            aleViewport.Dispose();
        }
    }
}
