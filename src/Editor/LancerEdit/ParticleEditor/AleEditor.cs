// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.Fx;
using LibreLancer.Utf.Ale;
using ImGuiNET;
using LibreLancer.ContentEdit;
using LibreLancer.Graphics;
using LibreLancer.Render;
using LibreLancer.Render.Cameras;
using LibreLancer.Resources;

namespace LancerEdit
{

    public partial class AleEditor : EditorTab
    {
        //GL
        Viewport3D aleViewport;
        RenderContext rstate;
        CommandBuffer buffer;
        PolylineRender polyline;
        LineRenderer debug;
        ParticleEffectPool pool;
        ResourceManager res;
        private int cameraMode = 0;
        private EditorUndoBuffer undoBuffer = new();
        private static readonly DropdownOption[] camModes= new[]
        {
            new DropdownOption("Arcball", Icons.Globe, CameraModes.Arcball),
            new DropdownOption("Walkthrough", Icons.StreetView, CameraModes.Walkthrough),
        };
        //Tab
        public string FilePath;
        public ParticleLibrary ParticleFile;
        private ParticleEffect lastEffect = null;
        private ParticleEffect currentEffect;
        private AppearanceReference[] allAppearances;
        private FieldReference[] allFields;

        float sparam = 1;

        private VerticalTabLayout layout;
        private EditorConfiguration config;
        private bool renderHistory = false;

        private PopupManager popups;

        public EditableUtf Utf;

        public void UpdateTitle()
        {
            Title = $"Ale Editor - {DocumentName}";
        }

        public AleEditor(string name, string? filePath, AleFile ale, EditableUtf utf, MainWindow main)
        {
            Utf = utf;
            config = main.Config;
            cameraMode = main.Config.DefaultCameraMode;
            ParticleFile = new ParticleLibrary(main.Resources, ale);
            res = main.Resources;
            pool = new ParticleEffectPool(main.RenderContext, main.Commands);
            DocumentName = name;
            FilePath = filePath;
            SaveStrategy = new AleSaveStrategy(main, this);
            UpdateTitle();
            this.rstate = main.RenderContext;
            aleViewport = new Viewport3D(main);
            aleViewport.DefaultOffset =
            aleViewport.CameraOffset = new Vector3(0, 0, 200);
            aleViewport.ModelScale = 25;
            aleViewport.ResetControls();
            aleViewport.Draw3D = DrawGL;
            buffer = main.Commands;
            polyline = main.Polyline;
            debug = main.LineRenderer;
            layout = new VerticalTabLayout(DrawLeft, DrawRight, DrawMiddle);
            layout.TabsLeft.Add(new(Icons.Tree, "Nodes", 0));
            layout.TabsRight.Add(new(Icons.Fire, "Effects", 0));
            layout.ActiveLeftTab = 0; // Open by default
            layout.ActiveRightTab = 0;
            undoBuffer.Hook += OnChanged;
            currentEffect = ParticleFile.Effects.First();
            OnChanged();
            popups = new();
        }

        private void OnChanged()
        {
            allAppearances = currentEffect.Appearances.ToArray();
            allFields = GetEffectNodes(currentEffect).OfType<FieldReference>().ToArray();
            SetupRender(currentEffect);
        }

        ParticleEffectInstance instance;
        void SetupRender(ParticleEffect effect)
        {
            instance = new ParticleEffectInstance(effect);
            instance.Pool = pool;
            instance.Resources = ParticleFile.Resources;
            lastEffect = currentEffect;
        }

        void DrawLeft(int tag)
        {
            if (tag == 0)
                NodePanel();
        }

        void DrawRight(int tag)
        {
            if (tag == 0)
                EffectsPanel();
        }


        void DrawMiddle()
        {
            //Fx management
            if (currentEffect != lastEffect)
            {
                selectedReference = null;
                OnChanged();
            }
            ImGui.SameLine();
            ImGui.PushItemWidth(80);
            ImGui.SliderFloat("SParam", ref sparam, 0, 1, "%f");
            ImGui.PopItemWidth();
            ImGui.SameLine();
            if (ImGuiExt.ToggleButton("Undo History", renderHistory))
                renderHistory = !renderHistory;
            ImGui.Separator();
            //Viewport
            aleViewport.MarginH = ImGui.GetFrameHeightWithSpacing() * 1.25f;
            aleViewport.Draw();
            //Action Bar
            ImGuiExt.DropdownButton("Camera Mode", ref cameraMode, camModes);
            aleViewport.Mode = (CameraModes) camModes[cameraMode].Tag;
            ImGui.SameLine();
            if (ImGui.Button("Reset Camera (Ctrl+R)"))
                aleViewport.ResetControls();
            ImGui.SameLine();
            if (ImGui.Button("Reset Fx"))
            {
                instance.Reset();
            }
            ImGui.SameLine();
            ImGui.Text(string.Format("T: {0:0.000}", instance.GlobalTime));
        }


        public override void Draw(double elapsed)
        {
            layout.Draw((VerticalTabStyle)config.TabStyle);
            for (int i = 0; i < openEditors.Count; i++)
            {
                var isOpen = true;
                ImGui.SetNextWindowSize(new Vector2(350, 400) * ImGuiHelper.Scale, ImGuiCond.FirstUseEver);
                if (ImGui.Begin(openEditors[i].NodeName, ref isOpen))
                {
                    AleNodeEditor.EditNode(openEditors[i], undoBuffer);
                }
                ImGui.End();
                if (!isOpen)
                {
                    openEditors.RemoveAt(i);
                    i--;
                }
            }
            if (renderHistory)
                undoBuffer.DisplayStack();
            popups.Run();
        }

        private float h1 = 150, h2 = 200;


        void NodePanel()
        {
            var totalH = ImGui.GetWindowHeight();
            ImGuiExt.SplitterV(2f, ref h1, ref h2, 15 * ImGuiHelper.Scale, 60 * ImGuiHelper.Scale, -1);
            h1 = totalH - h2 - 4f * ImGuiHelper.Scale;
            ImGui.BeginChild("top-panel", new Vector2(ImGui.GetWindowWidth(), h1));
            CurrentEffectPanel();
            ImGui.EndChild();
            ImGui.BeginChild("bottom-panel", new Vector2(ImGui.GetWindowWidth(), h2));
            NodeLibraryPanel();
            ImGui.EndChild();

        }

        void CurrentEffectPanel()
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Current Effect");
            ImGui.Separator();
            NodeOptions();
            ImGui.Separator();
            NodeHierachy();
        }

        NodeReference selectedReference = null;
        void NodeHierachy()
        {
            var enabledColor = (Vector4)ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
            var disabledColor = (Vector4)ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];

            var isTreeOpen = ImGui.TreeNodeEx("Effect", ImGuiTreeNodeFlags.DefaultOpen);
            if (isTreeOpen)
            {
                foreach (var reference in instance.Effect.Tree)
                {
                    int j = 0;
                    DoNode(reference, j++, enabledColor, disabledColor);
                }
                ImGui.TreePop();
            }
        }

        private List<FxNode> openEditors = new();


        void NodeOptions()
        {
            if (selectedReference == null)
            {
                ImGui.Text("No Selection");
                return;
            }
            ImGui.Text(string.Format("Selected: {0}", selectedReference.Node.NodeName));
            //Node enabled
            var enabled = selectedReference.Enabled;
            ImGui.Checkbox("Enabled", ref enabled);
            ImGui.SetItemTooltip("State of the node in this visualization (editor only).");
            selectedReference.Enabled = enabled;
            //
            if (ImGui.Button("Edit Node") && !openEditors.Contains(selectedReference.Node))
            {
                openEditors.Add(selectedReference.Node);
            }
            // Pairs
            if (selectedReference is EmitterReference emit)
            {
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Linked: ");
                ImGui.SameLine();
                SearchDropdown<AppearanceReference>.Draw("linked-app",
                    ref emit.Linked, allAppearances, x => x?.Node?.NodeName ?? "(none)",
                    (old, upd) =>
                        undoBuffer.Set("Linked", () => ref emit.Linked, old, upd),
                    true);
            }
            if (selectedReference is AppearanceReference app)
            {
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Linked: ");
                ImGui.SameLine();
                SearchDropdown<FieldReference>.Draw("linked-app",
                    ref app.Linked, allFields, x => x?.Node?.NodeName ?? "(none)",
                    (old, upd) =>
                        undoBuffer.Set("Linked", () => ref app.Linked, old, upd),
                    true);
            }
            // Re-order hierarchy
        }

        void DoNode(NodeReference reference, int idx, Vector4 enabled, Vector4 disabled)
        {
            var col = reference.Enabled ? enabled : disabled;
            string label = null;
            if (reference.IsAttachmentNode)
                label = string.Format("Attachment##{0}", idx);
            else
                label = ImGuiExt.IDWithExtra(reference.Node.NodeName, idx);
            ImGui.PushStyleColor(ImGuiCol.Text, col);
            if (reference.Children.Count > 0)
            {
                bool isTreeOpen = Theme.IconTreeNode(NodeIcon(reference.Node), label,
                    ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.OpenOnArrow);
                NodeDropTarget(reference);
                if(isTreeOpen)
                {
                    int j = 0;
                    foreach (var child in reference.Children)
                        DoNode(child, j++, enabled, disabled);
                    ImGui.TreePop();
                }

            }
            else
            {
                if (ImGui.Selectable($"{NodeIcon(reference.Node)}  {label}", selectedReference == reference)) {
                    selectedReference = reference;
                }
                NodeDropTarget(reference);
            }
            ImGui.PopStyleColor();
        }

        static unsafe bool AcceptDragDropPayload(string type, ImGuiDragDropFlags flags, out ImGuiPayloadPtr ptr)
        {
            return (ptr = ImGui.AcceptDragDropPayload(type, flags)) != null;
        }

        void OnTreeChange()
        {
            currentEffect.CalculateInfo();
            SetupRender(currentEffect);
        }

        unsafe void NodeDropTarget(NodeReference self)
        {
            if (ImGui.BeginDragDropTarget())
            {
                if (AcceptDragDropPayload("NodeLib", ImGuiDragDropFlags.None, out var ptr)
                    && ptr.DataSize == 4)
                {
                    var crc = *(uint*)ptr.Data;
                    var fxnode = ParticleFile.Nodes.Values.FirstOrDefault(x => x.CRC == crc);
                    if (ptr.Delivery)
                    {
                        var r = NodeReference.Create(fxnode);
                        r.Parent = self;
                        undoBuffer.Commit(new AddNodeReference(this, self, r));
                    }
                }
                ImGui.EndDragDropTarget();
            }
        }

        private FxNode[] displayedNodes;
        private string filter = "";
        private string? shownFilter = null;
        void RefreshNodeList()
        {
            var availableNodes = ParticleFile.Nodes.Values.OrderBy(x => x.NodeName);
            if (!string.IsNullOrWhiteSpace(filter))
            {
                displayedNodes = availableNodes
                    .Where(x => x.NodeName.Contains(filter, StringComparison.InvariantCultureIgnoreCase))
                    .ToArray();
            }
            else
            {
                displayedNodes = availableNodes.ToArray();
            }
            shownFilter = filter;
        }

        void OnNewNode(Func<string, FxNode> create)
        {
            popups.OpenPopup(new NameInputPopup(NameInputConfig.Nickname("New Node",
                x => ParticleFile.Nodes.ContainsKey(x)), "",
                x => undoBuffer.Commit(new NodeCreate
                ( ParticleFile.Nodes, create(x), this))));
        }

        unsafe void NodeLibraryPanel()
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Node Library");
            ImGui.SameLine();
            Controls.HelpMarker("Possible effect nodes.\nDrag to the top panel to add to the current effect.");
            ImGui.Separator();
            ImGui.SetNextItemWidth(-Controls.ButtonWidth("Add"));
            ImGui.InputTextWithHint("##filter", "Filter", ref filter, 255);
            ImGui.SameLine();
            if (ImGui.Button("Add"))
                ImGui.OpenPopup("addnode");
            if (ImGui.BeginPopup("addnode"))
            {
                if (!ImGui.BeginTable("choices", 3))
                    return;
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextColored(Color4.Gray, "Emitters");
                if (ImGui.Selectable("Cube Emitter"))
                    OnNewNode(x => new FxCubeEmitter(x));
                if (ImGui.Selectable("Sphere Emitter"))
                    OnNewNode(x => new FxSphereEmitter(x));
                if (ImGui.Selectable("Cone Emitter"))
                    OnNewNode(x => new FxConeEmitter(x));
                ImGui.TextColored(Color4.Gray, "Misc");
                if (ImGui.Selectable("Empty Node"))
                    OnNewNode(x => new FxNode(x));
                ImGui.TableNextColumn();
                ImGui.TextColored(Color4.Gray, "Appearances");
                if (ImGui.Selectable("Basic Appearance"))
                    OnNewNode(x => new FxBasicAppearance(x));
                if (ImGui.Selectable("Rect Appearance"))
                    OnNewNode(x => new FxRectAppearance(x));
                if (ImGui.Selectable("Perp Appearance"))
                    OnNewNode(x => new FxPerpAppearance(x));
                if (ImGui.Selectable("Oriented Appearance"))
                    OnNewNode(x => new FxOrientedAppearance(x));
                if (ImGui.Selectable("Beam Appearance"))
                    OnNewNode(x => new FLBeamAppearance(x));
                if (ImGui.Selectable("Particle Appearance"))
                    OnNewNode(x => new FxParticleAppearance(x));
                ImGui.TableNextColumn();
                ImGui.TextColored(Color4.Gray, "Fields");
                if (ImGui.Selectable("Beam Field"))
                    OnNewNode(x => new FLBeamField(x));
                if (ImGui.Selectable("Dust Field"))
                    OnNewNode(x => new FLDustField(x));
                if (ImGui.Selectable("Air Field"))
                    OnNewNode(x => new FxAirField(x));
                if (ImGui.Selectable("Collide Field"))
                    OnNewNode(x => new FxCollideField(x));
                if (ImGui.Selectable("Gravity Field"))
                    OnNewNode(x => new FxGravityField(x));
                if (ImGui.Selectable("Radial Field"))
                    OnNewNode(x => new FxRadialField(x));
                if (ImGui.Selectable("Turbulence Field"))
                    OnNewNode(x => new FxTurbulenceField(x));
                ImGui.EndTable();

                ImGui.EndPopup();
            }
            if(shownFilter != filter)
                RefreshNodeList();
            foreach (var n in displayedNodes)
            {
                ImGui.PushID((int)n.CRC);
                ImGui.Selectable($"{NodeIcon(n)} {n.NodeName}");
                ImGui.SetItemTooltip(TypeName(n));
                if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.PayloadNoCrossProcess))
                {
                    uint crc = n.CRC;
                    ImGui.SetDragDropPayload("NodeLib", (IntPtr)(&crc), sizeof(uint));
                    ImGui.Text($"{NodeIcon(n)} {n.NodeName}");
                    ImGui.EndDragDropSource();
                }
                ImGui.PopID();
            }
        }



        void EffectsPanel()
        {
            foreach (var fx in ParticleFile.Effects)
            {
                if (ImGui.Selectable($"{fx.Nickname} (0x{fx.CRC:X})", currentEffect == fx))
                    currentEffect = fx;
            }
        }

        void DrawGL(int renderWidth, int renderHeight)
        {
            var cam = new LookAtCamera();
            Matrix4x4 rot = Matrix4x4.CreateRotationX(aleViewport.CameraRotation.Y) *
                Matrix4x4.CreateRotationY(aleViewport.CameraRotation.X);
            var dir = Vector3.Transform(-Vector3.UnitZ, rot);
            var to = aleViewport.CameraOffset + (dir * 10);
            if (aleViewport.Mode == CameraModes.Arcball)
                to = Vector3.Zero;
            cam.Update(renderWidth, renderHeight, aleViewport.CameraOffset, to, rot);
            rstate.SetCamera(cam);
            buffer.StartFrame(rstate);
            debug.StartFrame(rstate);
            pool.StartFrame(cam, polyline);
            instance.Draw(transform, sparam);
            pool.EndFrame();
            buffer.DrawOpaque(rstate);
            rstate.DepthWrite = false;
            buffer.DrawTransparent(rstate);
            rstate.DepthWrite = true;
            debug.Render();
        }

        public override void DetectResources(List<MissingReference> missing, List<uint> matrefs, List<TextureReference> texrefs)
        {
            foreach (var reference in instance.Effect.Appearances)
            {
                if (reference.Node is FxBasicAppearance)
                {
                    var node = reference.Node;
                    var fx = (FxBasicAppearance)reference.Node;
                    if (fx.Texture == null || !ResourceDetection.HasTexture(texrefs, fx.Texture))
                        continue;
                    Texture tex = null;
                    if (fx.Texture != null &&  (tex = ParticleFile.Resources.FindTexture(fx.Texture)) == null &&
                        !ParticleFile.Resources.TryGetFrameAnimation(fx.Texture, out _))
                    {
                        var str = "Texture: " + fx.Texture; //TODO: This is wrong - handle properly
                        if (!ResourceDetection.HasMissing(missing, str)) missing.Add(new MissingReference(
                            str, $"{instance.Effect.Nickname}: {node.NodeName} ({TypeName(node)})"));
                        texrefs.Add(new TextureReference(fx.Texture, null));
                    }
                    if(tex != null)
                        texrefs.Add(new TextureReference(fx.Texture, tex));
                }
            }
        }

        public override void OnHotkey(Hotkeys hk, bool shiftPressed)
        {
            if (hk == Hotkeys.ResetViewport) aleViewport.ResetControls();
            if (hk == Hotkeys.Undo && undoBuffer.CanUndo) undoBuffer.Undo();
            if (hk == Hotkeys.Redo && undoBuffer.CanRedo) undoBuffer.Redo();
        }

        Matrix4x4 transform = Matrix4x4.Identity;
        public override void Update(double elapsed)
        {
            transform = Matrix4x4.CreateRotationX(aleViewport.ModelRotation.Y) * Matrix4x4.CreateRotationY(aleViewport.ModelRotation.X);
            instance.Update(elapsed, transform, sparam);
        }

        public override void Dispose()
        {
            aleViewport.Dispose();
            pool.Dispose();
        }
    }
}
