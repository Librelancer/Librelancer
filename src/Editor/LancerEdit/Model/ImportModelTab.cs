// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.ContentEdit.Model;
using LibreLancer.ImUI;
using LibreLancer.Render;
using LibreLancer.Render.Cameras;
using LibreLancer.Render.Materials;
using LibreLancer.Shaders;
using LibreLancer.Sur;
using LibreLancer.Vertices;
using SimpleMesh;
using Material = LibreLancer.Utf.Mat.Material;

namespace LancerEdit;

public class ImportModelTab : EditorTab
{
    private int curTab;
    private Model editModel;

    private float fl_h1 = 200, fl_h2 = 200;

    private long fR;
    private bool generateMaterials = true;

    private Lighting lighting;
    private string[] lods;
    private Model model;
    private string modelNameDefault;
    private Viewport3D modelViewport;

    private ImportedModel output;
    private PreviewModel[] preview;
    private int previewLod;

    private ImportedModelNode selected;

    private int tabNo;

    private MainWindow win;

    private Material wireframeMaterial3db;

    private string outputPath;

    private bool generateSur = true;
    private bool canGenerateSur = false;

    private bool importTextures = true;

    public ImportModelTab(Model model, string fname, MainWindow win)
    {
        this.model = model;
        Title = string.Format("Model Importer ({0})", fname);
        modelNameDefault = Path.GetFileNameWithoutExtension(fname);
        this.win = win;
        outputPath = win.Config.LastExportPath;
        modelViewport = new Viewport3D(win);
        modelViewport.MarginH = 60;
        wireframeMaterial3db = new Material(win.Resources);
        wireframeMaterial3db.Dc = Color4.White;
        wireframeMaterial3db.DtName = ResourceManager.WhiteTextureName;
        lighting = Lighting.Create();
        lighting.Enabled = true;
        lighting.Ambient = Color3f.Black;
        var src = new SystemLighting();
        src.Lights.Add(new DynamicLight
        {
            Light = new RenderLight
            {
                Kind = LightKind.Directional,
                Direction = new Vector3(0, -1, 0),
                Color = Color3f.White
            }
        });
        src.Lights.Add(new DynamicLight
        {
            Light = new RenderLight
            {
                Kind = LightKind.Directional,
                Direction = new Vector3(0, 0, 1),
                Color = Color3f.White
            }
        });
        lighting.Lights.SourceLighting = src;
        lighting.Lights.SourceEnabled[0] = true;
        lighting.Lights.SourceEnabled[1] = true;
        lighting.NumberOfTilesX = -1;
        CloneModel();
        Import();
        Basic_PositionNormalColorTexture.Compile();
    }

    private void CloneModel()
    {
        editModel = model.Clone();
    }

    private void DrawFLNodesPanel()
    {
        ImGui.Text("Tree");
        ImGui.SameLine(ImGui.GetWindowWidth() - 60);
        if (ImGui.Button("Finish"))
        {
            win.StartLoadingSpinner();
            Task.Run(() =>
            {
                var result = output.CreateModel(new ModelImporterSettings
                {
                    GenerateMaterials = generateMaterials,
                    ImportTextures = importTextures,
                });
                win.QueueUIThread(() =>
                {
                    win.ResultMessages(result);
                    EditResult<SurFile> sur = null;
                    if (SurfaceBuilder.HasHulls(output))
                    {
                        sur = SurfaceBuilder.CreateSur(output);
                    }

                    if (sur != null) win.ResultMessages(sur);
                    if (result.IsSuccess &&
                        ((sur?.IsSuccess) ?? true))
                    {
                        var ext = output.Root.Children.Count > 0 ? ".cmp" : ".3db";
                        var modelPath = Path.Combine(outputPath, output.Name + ext);
                        var saved = result.Data.Save(modelPath, 0);
                        win.ResultMessages(saved);
                        if (saved.IsSuccess)
                        {
                            win.AddTab(new UtfTab(win, result.Data, output.Name + ext)
                                {FilePath = modelPath});
                            if (sur != null)
                            {
                                using (var surOut = File.Create(Path.Combine(outputPath, output.Name + ".sur")))
                                {
                                    sur.Data.Save(surOut);
                                }
                            }
                            win.Config.LastExportPath = outputPath;
                        }
                    }
                    win.FinishLoadingSpinner();
                });
            });

        }
        ImGui.BeginChild("##fl");
        FLPane();
        ImGui.EndChild();
    }

    private void FLPane()
    {
        var totalH = ImGui.GetWindowHeight();
        ImGuiExt.SplitterV(2f, ref fl_h1, ref fl_h2, 8, 8, -1);
        fl_h1 = totalH - fl_h2 - 6f;
        ImGui.BeginChild("1", new Vector2(-1, fl_h1), false, ImGuiWindowFlags.None);
        ImGui.Separator();
        //3DB list
        if (ImGui.TreeNodeEx("Model/"))
        {
            var i = 0;
            if (output.Root != null)
                FLTree(output.Root, ref i);
        }

        ImGui.EndChild();
        ImGui.BeginChild("2", new Vector2(-1, fl_h2), false, ImGuiWindowFlags.None);
        if (ImGuiExt.ToggleButton("Options", curTab == 0)) curTab = 0;
        ImGui.SameLine();
        if (ImGuiExt.ToggleButton("Materials", curTab == 1)) curTab = 1;
        ImGui.Separator();
        switch (curTab)
        {
            case 0: //OPTIONS
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Model Name:");
                ImGui.SameLine();
                ImGui.InputText("##mdlname", ref output.Name, 1000);
                ImGui.Checkbox("Generate Materials", ref generateMaterials);
                ImGui.BeginDisabled(model.Images == null || model.Images.Count == 0);
                ImGui.Checkbox("Import Textures", ref importTextures);
                ImGui.EndDisabled();
                ImGui.BeginDisabled(!canGenerateSur);
                ImGui.Checkbox("Generate Sur", ref generateSur);
                ImGui.EndDisabled();
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Output Path:");
                ImGui.SameLine();
                ImGui.InputText("##mdlpath", ref outputPath, 1000);
                ImGui.SameLine();
                if (ImGui.Button("..")) {
                    outputPath = FileDialog.ChooseFolder() ?? outputPath;
                }
                break;
            case 1: //MATERIALS
                MatNameEdit();
                break;
        }

        ImGui.EndChild();
    }

    private void DoModelNode(ModelNode n, ref int i, Action deleteAction)
    {
        var toRemove = new List<ModelNode>();
        var text = ImGuiExt.IDWithExtra(n.Name, i++);
        if (n.Geometry == null &&
            n.Children.Count == 0)
        {
            ImGui.Text($"  {Icons.BulletEmpty}");
            ImGui.SameLine();
            ImGui.Selectable(text);
        }
        else if (n.Geometry != null &&
                 n.Children.Count == 0)
        {
            ImGui.Text($"  {Icons.Cube_Coral}");
            ImGui.SameLine();
            ImGui.Selectable(text);
        }
        else if (n.Geometry == null &&
                 n.Children.Count > 0)
        {
            if (Theme.IconTreeNode(Icons.BulletEmpty, text))
            {
                foreach (var child in n.Children)
                    DoModelNode(child, ref i, () => toRemove.Add(child));
                ImGui.TreePop();
            }
        }
        else if (n.Geometry != null &&
                 n.Children.Count > 0)
        {
            if (Theme.IconTreeNode(Icons.Cube_Coral, text))
            {
                foreach (var child in n.Children)
                    DoModelNode(child, ref i, () => toRemove.Add(child));
                ImGui.TreePop();
            }
        }

        foreach (var t in toRemove)
            n.Children.Remove(t);
    }

    private void DrawInputPanel()
    {
        ImGui.Text("Input Tree");
        ImGui.SameLine(ImGui.GetWindowWidth() - 125);
        if (ImGui.Button("Reset"))
            CloneModel();
        ImGui.SameLine(ImGui.GetWindowWidth() - 70);
        if (ImGui.Button("Import"))
            Import();
        ImGui.Separator();
        if (ImGui.TreeNode("Source/"))
        {
            var i = 0;
            for (var j = 0; j < editModel.Roots.Length; j++)
                DoModelNode(editModel.Roots[j], ref i, () =>
                    editModel.Roots = editModel.Roots.Where(x => x != editModel.Roots[j]).ToArray()
                );

            ImGui.TreePop();
        }
    }

    private void DisposePreview()
    {
        if (preview != null)
            foreach (var p in preview)
                p.Dispose();
        preview = null;
    }

    private bool lit = false;

    private void DrawModel(RenderContext rstate, ICamera camera)
    {
        var mat = wireframeMaterial3db.Render;
        rstate.Cull = true;
        rstate.DepthWrite = true;
        var bm = (BasicMaterial) mat;
        bm.Oc = 1;
        bm.OcEnabled = true;
        mat.Camera = camera;
        uint i = 0;
        if(lit)
            preview[previewLod].Draw(rstate, Matrix4x4.Identity, mat, ref lighting, ref i);
        else
            preview[previewLod].Draw(rstate, Matrix4x4.Identity, mat, ref Lighting.Empty, ref i);
    }

    private void DrawPreviewPanel()
    {
        if (preview == null)
        {
            ImGui.Text("Preview not available");
            return;
        }

        ImGui.Combo("LOD", ref previewLod, lods, lods.Length);
        ImGui.SameLine();
        ImGui.Checkbox("Lit", ref lit);
        modelViewport.Begin();
        var lookAtCam = new LookAtCamera();
        var rot = Matrix4x4.CreateRotationX(modelViewport.CameraRotation.Y) *
                  Matrix4x4.CreateRotationY(modelViewport.CameraRotation.X);
        var dir = Vector3.Transform(-Vector3.UnitZ, rot);
        var to = modelViewport.CameraOffset + dir * 10;
        if (modelViewport.Mode == CameraModes.Arcball) to = Vector3.Zero;
        lookAtCam.Update(modelViewport.RenderWidth, modelViewport.RenderHeight, modelViewport.CameraOffset, to, rot);
        lookAtCam.FrameNumber = fR++;
        win.RenderContext.ClearColor = Color4.Black;
        win.RenderContext.ClearAll();
        DrawModel(win.RenderContext, lookAtCam);
        modelViewport.End();
    }

    private void BuildPreview()
    {
        DisposePreview();
        float radius = 1;
        var lI = 0;
        var mdls = new List<PreviewModel>();
        PreviewModel p;
        while ((p = BuildPreviewNode(output.Root, Matrix4x4.Identity, ref radius, lI)) != null)
        {
            mdls.Add(p);
            lI++;
        }

        lods = new string[lI];
        for (var i = 0; i < lods.Length; i++) lods[i] = i.ToString();
        preview = mdls.ToArray();
        previewLod = 0;
        modelViewport.DefaultOffset =
            modelViewport.CameraOffset = new Vector3(0, 0, radius * 2);
        modelViewport.ModelScale = radius / 2.6f;
        modelViewport.ResetControls();
    }

   

    private PreviewModel BuildPreviewNode(ImportedModelNode n, Matrix4x4 parent, ref float radius, int lodIndex)
    {
        var pm = new PreviewModel();
        pm.Transform = Matrix4x4.Identity;
        Geometry g;
        if (n.LODs.Count == 0 && n.Def != null && lodIndex == 0)
            g = n.Def.Geometry;
        else if (n.LODs.Count > lodIndex)
            g = n.LODs[lodIndex].Geometry;
        else
            return null;
        pm.Drawcalls = g.Groups;
        pm.Transform = n.Transform;
        var d = Vector3.Transform(Vector3.Zero, pm.Transform * parent).Length();
        var r = g.Radius;
        if (d + r > radius) radius = d + r;
        var vertices = g.Vertices.Select(x =>
            new VertexPositionNormalDiffuseTexture(
                x.Position, 
                x.Normal, 
                (uint)(new Color4(x.Diffuse).ToAbgr()), 
                Vector2.One)
        ).ToArray();
        var elements = g.Indices.Indices16;
        pm.Vertices = new VertexBuffer(typeof(VertexPositionNormalDiffuseTexture), vertices.Length);
        pm.Elements = new ElementBuffer(elements.Length);
        pm.Vertices.SetData(vertices);
        pm.Elements.SetData(elements);
        pm.Vertices.SetElementBuffer(pm.Elements);
        foreach (var child in n.Children)
        {
            var x = BuildPreviewNode(child, pm.Transform * parent, ref radius, lodIndex);
            if (x != null)
                pm.Children.Add(x);
        }

        return pm;
    }

    private List<SimpleMesh.Material> allMaterials = new();

    void AddMaterials(ModelNode mn)
    {
        foreach (var tg in mn.Geometry.Groups)
        {
            if(allMaterials.All(x => x != tg.Material))
                allMaterials.Add(tg.Material);
        }
    }
    void FindMaterials(ImportedModelNode n)
    {
        foreach (var l in n.LODs)
        {
            AddMaterials(l);
        }
        if(n.Def != null)
            AddMaterials(n.Def);
        foreach(var c in n.Children)
            FindMaterials(c);
    }
    

    private void Import()
    {
        var o = ImportedModel.FromSimpleMesh(Path.GetFileNameWithoutExtension(modelNameDefault), editModel.Clone());
        win.ResultMessages(o);
        if (o.IsError)
        {
            output = new ImportedModel {Name = Path.GetFileNameWithoutExtension(modelNameDefault)};
            DisposePreview();
        }
        else
        {
            output = o.Data;
            tabNo = 1;
            BuildPreview();
            allMaterials = new ();
            FindMaterials(output.Root);
            canGenerateSur = SurfaceBuilder.HasHulls(output);
        }
    }

    public override void Draw()
    {
        ImGui.BeginGroup();
        if (TabHandler.VerticalTab("Input Nodes", tabNo == 0))
            tabNo = 0;
        if (preview != null)
        {
            if (TabHandler.VerticalTab("Output Nodes", tabNo == 1))
                tabNo = 1;
            if (TabHandler.VerticalTab("Preview", tabNo == 2))
                tabNo = 2;
        }
        ImGui.EndGroup();
        ImGui.SameLine();
        ImGui.BeginChild("##importwin");
        if (tabNo == 0)
            DrawInputPanel();
        else if (tabNo == 1)
            DrawFLNodesPanel();
        else
            DrawPreviewPanel();

        ImGui.EndChild();
    }

    private void MatNameEdit()
    {
        ImGui.BeginChild("##materialNames");
        for (int i = 0; i < allMaterials.Count; i++)
        {
            ImGui.InputText("##" + i, ref allMaterials[i].Name, 1024);
        }
        ImGui.EndChild();
    }

    private void FLTree(ImportedModelNode mdl, ref int i)
    {
        var flags = ImGuiTreeNodeFlags.OpenOnDoubleClick |
                    ImGuiTreeNodeFlags.DefaultOpen |
                    ImGuiTreeNodeFlags.OpenOnArrow;
        if (mdl == selected) flags |= ImGuiTreeNodeFlags.Selected;
        var open = Theme.IconTreeNode(Icons.Cube_LightPink, $"{mdl.Name}##{i++}", flags);
        if (ImGui.IsItemClicked(0)) selected = mdl;

        if (open)
        {
            if (ImGui.TreeNodeEx("LODs"))
            {
                for (var j = 0; j < mdl.LODs.Count; j++)
                    ImGui.Selectable(string.Format("{0}: {1}", j, mdl.LODs[j].Name));
                ImGui.TreePop();
            }
            foreach (var child in mdl.Children)
                FLTree(child, ref i);
            ImGui.TreePop();
        }

        i += 500;
    }


    public override void Dispose()
    {
        DisposePreview();
        modelViewport.Dispose();
    }

    private class PreviewModel
    {
        public readonly List<PreviewModel> Children = new();
        public TriangleGroup[] Drawcalls;
        public ElementBuffer Elements;
        public Matrix4x4 Transform;
        public VertexBuffer Vertices;

        public void Dispose()
        {
            Vertices?.Dispose();
            Elements?.Dispose();
            foreach (var c in Children)
                c.Dispose();
        }

        public unsafe void Draw(RenderContext rstate, Matrix4x4 parentTransform, RenderMaterial mat, ref Lighting lighting, ref uint i)
        {
            Matrix4x4* trs = stackalloc Matrix4x4[2];
            trs[0] = Transform * parentTransform;
            Matrix4x4.Invert(trs[0], out var normal);
            trs[1] = Matrix4x4.Transpose(normal);
            i++;
            if (Vertices != null)
            {
                var whandle = new WorldMatrixHandle
                {
                    ID = ulong.MaxValue,
                    Source = trs
                };
                mat.World = whandle;
                mat.Use(rstate, new VertexPositionNormalDiffuseTexture(), ref lighting);
                foreach (var dc in Drawcalls)
                    Vertices.Draw(PrimitiveTypes.TriangleList, dc.BaseVertex, dc.StartIndex, dc.IndexCount / 3);
            }

            foreach (var c in Children)
                c.Draw(rstate, trs[0], mat, ref lighting, ref i);
        }
    }
}