using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent.Popups;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.Data;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Ini;
using LibreLancer.ImUI;
using LibreLancer.Render;
using LibreLancer.Render.Cameras;
using LibreLancer.Render.Materials;
using LibreLancer.Resources;
using LibreLancer.World;

namespace LancerEdit.GameContent;

public class AsteroidFieldEdit
{
    public AsteroidField Target;
    public AsteroidField Field;

    private Viewport3D viewport;
    private SystemRenderer renderer;
    private GameWorld world;
    private LookAtCamera camera;

    private GameObject[] asteroids;

    private SystemEditorTab parent;

    private Vector4 tintSelector;
    private Vector4 diffuseSelector;
    private Vector4 ambientSelector;
    private Vector4 intensitySelector;

    private MainWindow mw;
    private uint matCrc;

    public AsteroidFieldEdit(AsteroidField field, MainWindow mw, SystemEditorTab parent)
    {
        this.mw = mw;

        Target = field;
        Field = field.Clone(parent.ZoneList.ZonesByName);

        diffuseSelector = field.DiffuseColor;
        ambientSelector = field.AmbientColor;
        intensitySelector = field.AmbientIncrease;
        camera = new LookAtCamera()
        {
            GameFOV = true,
            ZRange = new Vector2(3f, 10000000f)
        };
        viewport = new Viewport3D(mw);
        viewport.EnableMSAA = false; //MSAA handled by SystemRenderer
        viewport.DefaultOffset = new Vector3(0, 0, field.CubeSize * 1.2f);
        viewport.ModelScale = 8;
        viewport.Mode = CameraModes.Walkthrough;
        viewport.Background = new Vector4(0.12f, 0.12f, 0.12f, 1f);
        viewport.Draw3D = DrawGL;
        viewport.ResetControls();
        renderer = new SystemRenderer(camera, mw.OpenDataContext.Resources, mw);
        renderer.SystemLighting.Ambient = Color4.White;
        renderer.SystemLighting.NumberOfTilesX = -1;
        renderer.SystemLighting.Lights.Add(new DynamicLight()
        {
            Active = true,
            Light = new()
            {
                Ambient = Color3f.Black,
                Color = Color3f.White,
                Direction = Vector3.UnitZ,
                Kind = LightKind.Directional,
                Range = 100000000
            }
        });
        renderer.StarSphereModels = parent.Renderer.StarSphereModels;
        renderer.StarSphereLightings = parent.Renderer.StarSphereLightings;
        renderer.StarSphereWorlds = parent.Renderer.StarSphereWorlds;
        renderer.BackgroundOverride = parent.CurrentSystem.BackgroundColor;

        this.parent = parent;

        world = new GameWorld(renderer, mw.OpenDataContext.Resources, null);
        if (field.Cube.Count > 0) {
            matCrc = GetFirstMaterial(field.Cube[0].Archetype.ModelFile, mw.OpenDataContext.Resources);
        }
        asteroids = new GameObject[field.Cube.Count];
        for(int i = 0; i < field.Cube.Count; i++)
        {
            var c = field.Cube[i];
            var res = CreatePatchedModel(c.Archetype.ModelFile, mw.OpenDataContext.Resources, matCrc);
            var obj = new GameObject(res, mw.OpenDataContext.Resources);
            obj.World = world;
            world.AddObject(obj);
            obj.Register(world.Physics);
            asteroids[i] = obj;
        }
        renderer.PhysicsHook = () =>
        {
            renderer.DebugRenderer.DrawCube(Matrix4x4.Identity, field.CubeSize, Color4.Yellow);
        };
        SetAstPositions();
        if (world.Objects.Count > 0)
        {
            MoveCameraTo(world.Objects[0]);
        }
    }

    class PatchedModel : IRigidModelFile
    {
        public RigidModel Patched;
        public void ClearResources() { }
        public RigidModel CreateRigidModel(bool drawable, ResourceManager resources) => Patched;
    }

    static uint GetFirstMaterial(ResolvedModel src, ResourceManager res)
    {
        var mdl = src.LoadFile(res);
        var rm = ((IRigidModelFile)mdl.Drawable).CreateRigidModel(true, res);
        foreach (var p in rm.AllParts)
        {
            if (p.Mesh == null) continue;
            foreach (var l in p.Mesh.Levels)
            {
                if (l == null || l.Drawcalls == null) continue;
                foreach (var dc in l.Drawcalls)
                {
                    return dc.MaterialCrc;
                }
            }
        }
        return 0;
    }
    static ModelResource CreatePatchedModel(ResolvedModel src, ResourceManager res, uint materialCrc)
    {
        var mdl = src.LoadFile(res);
        var rm = ((IRigidModelFile)mdl.Drawable).CreateRigidModel(true, res);
        foreach (var p in rm.AllParts)
        {
            if (p.Mesh == null) continue;
            foreach (var l in p.Mesh.Levels)
            {
                if (l == null || l.Drawcalls == null) continue;
                for (int i = 0; i < l.Drawcalls.Length; i++) {
                    l.Drawcalls[i].MaterialCrc = materialCrc;
                }
            }
        }
        return new ModelResource(new PatchedModel() { Patched = rm }, mdl.Collision);
    }

    void SetAstPositions()
    {
        for (int i = 0; i < Field.Cube.Count; i++)
        {
            var c = Field.Cube[i];
            asteroids[i].SetLocalTransform(new Transform3D(c.Position * Field.CubeSize, c.Rotation));
        }
    }

    private int lastW = 100;
    private int lastH = 100;
    public void Update(double elapsed)
    {
        var rot = Matrix4x4.CreateRotationX(viewport.CameraRotation.Y) *
                  Matrix4x4.CreateRotationY(viewport.CameraRotation.X);
        var dir = Vector3.Transform(-Vector3.UnitZ, rot);
        var to = viewport.CameraOffset + (dir * 10);
        camera.Update(lastW, lastH, viewport.CameraOffset, to, rot);
        world.Update(elapsed);
        world.RenderUpdate(elapsed);
    }

    void MoveCameraTo(GameObject obj)
    {
        var r = (obj.RenderComponent as ModelRenderer)?.Model?.GetRadius() ?? 10f;
        viewport.CameraOffset = obj.LocalTransform.Position + new Vector3(0, 0, -r * 3.5f);
        viewport.CameraRotation = new Vector2(-MathF.PI, 0);
    }

    private float fl_h1 = 200, fl_h2 = 200;

    void DrawGL(int w, int h)
    {
        lastW = w;
        lastH = h;
        var mat = (renderer.ResourceManager.FindMaterial(matCrc)?.Render as BasicMaterial);
        var restoreDc = mat?.Dc ?? Color4.Black;
        if (mat != null)
            mat.Dc = Field.DiffuseColor;
        renderer.Draw(w, h);
        if (mat != null)
            mat.Dc = restoreDc;
    }

    void Cube()
    {
        ImGui.Columns(2, "cubeProps", true);
        ImGui.BeginChild("##Props");
        var sz = Field.CubeSize;
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Cube Size");
        ImGui.SameLine();
        ImGui.InputInt("##cubesize", ref sz);
        if (sz != Field.CubeSize)
        {
            SetAstPositions();
            Field.CubeSize = sz;
        }

        var x = Field.CubeRotation.AxisX;
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Rotate X");
        ImGui.SameLine();
        ImGui.InputFloat4("##rotateX", ref x);
        if(x != Field.CubeRotation.AxisX)
            Field.CubeRotation.AxisX = x;

        var y = Field.CubeRotation.AxisY;
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Rotate Y");
        ImGui.SameLine();
        ImGui.InputFloat4("##rotateZ", ref y);
        if (y != Field.CubeRotation.AxisY)
            Field.CubeRotation.AxisY = y;

        var z = Field.CubeRotation.AxisZ;
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Rotate Z");
        ImGui.SameLine();
        ImGui.InputFloat4("##rotateZ", ref z);
        if (z != Field.CubeRotation.AxisZ)
            Field.CubeRotation.AxisZ = z;

        //Colour
        ImGui.SeparatorText("Color");
        if (ImGui.BeginTable("##color", 2, ImGuiTableFlags.Borders))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.ColorPicker4("Ambient Color", ref ambientSelector, ImGuiColorEditFlags.NoAlpha);
            Field.AmbientColor = ambientSelector;
            ImGui.TableNextColumn();
            ImGui.ColorPicker4("Ambient Increase", ref intensitySelector, ImGuiColorEditFlags.NoAlpha);
            Field.AmbientIncrease = intensitySelector;
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.ColorPicker4("Diffuse", ref diffuseSelector, ImGuiColorEditFlags.NoAlpha);
            Field.DiffuseColor = diffuseSelector;
            ImGui.TableNextColumn();
            ImGui.TextWrapped("Ambient = (System Ambient + Ambient Increase) * Ambient Color");
            ImGui.Text("Final Ambient: ");
            ImGui.ColorButton("##famb", (new Color4(parent.CurrentSystem.AmbientColor, 1) + Field.AmbientIncrease) * Field.AmbientColor,
                ImGuiColorEditFlags.NoAlpha);
            ImGui.EndTable();
        }



        /*if (ImGui.BeginTable("##color", 2, ImGuiTableFlags.Borders))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            if (ImGui.Selectable("Tint", Field.TintField != null, ImGuiSelectableFlags.DontClosePopups)) {
                Field.TintField = tintSelector;
            }
            ImGui.SetItemTooltip("Set field to one color");
            ImGui.TableNextColumn();
            if (ImGui.Selectable("Diffuse+Ambient", Field.TintField == null, ImGuiSelectableFlags.DontClosePopups)) {
                Field.TintField = null;
            }
            ImGui.SetItemTooltip("Specify diffuse and ambient colors separately");
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            //Tint control
            ImGui.BeginDisabled(Field.TintField == null);
            ImGui.ColorPicker4("Tint", ref tintSelector, ImGuiColorEditFlags.NoAlpha);
            if (Field.TintField != null)
                Field.TintField = tintSelector;
            ImGui.EndDisabled();
            ImGui.TableNextColumn();
            ImGui.BeginDisabled(Field.TintField != null);
            ImGui.ColorPicker4("Diffuse", ref diffuseSelector, ImGuiColorEditFlags.NoAlpha);
            if (diffuseSelector != (Vector4)Field.DiffuseColor)
                Field.DiffuseColor = diffuseSelector;
            //Ambient vs Intensity
            if (ImGui.RadioButton("Ambient", Field.AmbientColor != null))
                Field.AmbientColor = ambientSelector;
            ImGui.SetItemTooltip("Multiply color with system ambient light");
            if (ImGui.RadioButton("Intensity", Field.AmbientColor == null))
                Field.AmbientColor = null;
            ImGui.SetItemTooltip("Add color to system ambient light");
            if (Field.AmbientColor != null)
            {

            }
            ImGui.EndDisabled();
            ImGui.EndTable();
        }*/

        ImGui.EndChild();
        ImGui.NextColumn();
        ImGui.BeginChild("##viewport");

        var totalH = ImGui.GetWindowHeight();
        ImGuiExt.SplitterV(2f, ref fl_h1, ref fl_h2, 8, 8, -1);
        fl_h1 = totalH - fl_h2 - 6f;
        ImGui.BeginChild("1", new Vector2(-1, fl_h1));
        ImGuiHelper.AnimatingElement();
        var vpSize = ImGui.GetColumnWidth() - 15 * ImGuiHelper.Scale;
        //Set ambient color
        renderer.SystemLighting.Ambient = (new Color4(parent.CurrentSystem.AmbientColor, 1) + Field.AmbientIncrease)* Field.AmbientColor;
        viewport.Draw((int)vpSize, (int)(fl_h1 - 15 * ImGuiHelper.Scale));
        ImGui.EndChild();
        ImGui.BeginChild("2", new Vector2(-1, fl_h2));
        ImGui.Text("Asteroids");
        ImGui.SameLine();
        if (ImGui.Button($"{Icons.PlusCircle}")) {
            parent.Popups.OpenPopup(new AsteroidSelection(sel =>
            {

            }, parent.Data, matCrc));
        }
        for (int i = 0; i < world.Objects.Count; i++)
        {
            if (ImGui.Button(i.ToString()))
            {
                MoveCameraTo(world.Objects[i]);
            }
        }
        ImGui.EndChild();
        ImGui.EndChild();
        ImGui.Columns(1);
    }
    void Properties()
    {
        foreach (var x in Enum.GetValues<FieldFlags>()) {
            if (Controls.Flag(x.ToString(), Field.Flags, x, out var set))
            {
                if (set) Field.Flags |= x;
                else Field.Flags &= ~x;
            }
        }
    }

    string GenText()
    {
        var ms = new MemoryStream();
        IniWriter.WriteIni(ms, IniSerializer.SerializeAsteroidField(Field));
        ms.Position = 0;
        return new StreamReader(ms).ReadToEnd();
    }

    private TextDisplayWindow td = null;
    public void Draw()
    {
        if (td != null)
        {
            td.Draw();
        }
        ImGui.Text($"Editing Asteroid Field: {Field.SourceFile}");
        ImGui.SameLine();
        if (ImGui.Button("Apply Changes"))
        {
            parent.UndoBuffer.Commit(GetUpdateAction());
            parent.AsteroidFieldClose();
        }

        ImGui.SameLine();
        if(ImGui.Button("Discard"))
            parent.AsteroidFieldClose();
        ImGui.SameLine();
        if (ImGui.Button("Generate Ini"))
        {
            td = new TextDisplayWindow(GenText(), Field.SourceFile, mw);
        }
        if (ImGui.BeginTabBar("##tabs"))
        {
            if (ImGui.BeginTabItem("Cube"))
            {
                Cube();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Properties"))
            {
                Properties();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    public EditorAction GetUpdateAction()
    {
        // ReSharper disable CompareOfFloatsByEqualityOperator
        List<EditorAction> actions = new List<EditorAction>();

        if (Target.DiffuseColor != Field.DiffuseColor) {
            actions.Add(new AsteroidFieldSetDiffuseColor(Target, Target.DiffuseColor, Field.DiffuseColor));
        }
        if (Target.AmbientColor != Field.AmbientColor) {
            actions.Add(new AsteroidFieldSetAmbientColor(Target, Target.AmbientColor, Field.AmbientColor));
        }
        if (Target.AmbientIncrease != Field.AmbientIncrease) {
            actions.Add(new AsteroidFieldSetAmbientIncrease(Target, Target.AmbientIncrease, Field.AmbientIncrease));
        }
        if (Target.FillDist != Field.FillDist) {
            actions.Add(new AsteroidFieldSetFillDist(Target, Target.FillDist, Field.FillDist));
        }
        if (Target.EmptyCubeFrequency != Field.EmptyCubeFrequency) {
            actions.Add(new AsteroidFieldSetEmptyCubeFrequency(Target, Target.EmptyCubeFrequency, Field.EmptyCubeFrequency));
        }
        if (Target.CubeSize != Field.CubeSize) {
            actions.Add(new AsteroidFieldSetCubeSize(Target, Target.CubeSize, Field.CubeSize));
        }
        if (Target.CubeRotation != Field.CubeRotation) {
            actions.Add(new AsteroidFieldSetCubeRotation(Target, Target.CubeRotation, Field.CubeRotation));
        }
        if (!DataEquality.ListEquals(Target.Cube, Field.Cube)) {
            actions.Add(new AsteroidFieldSetCube(Target ,Target.Cube, Field.Cube));
        }
        if (Target.BillboardCount != Field.BillboardCount) {
            actions.Add(new AsteroidFieldSetBillboardCount(Target, Target.BillboardCount, Field.BillboardCount));
        }
        if (Target.BillboardDistance != Field.BillboardDistance) {
            actions.Add(new AsteroidFieldSetBillboardDistance(Target, Target.BillboardDistance, Field.BillboardDistance));
        }
        if (Target.BillboardFadePercentage != Field.BillboardFadePercentage) {
            actions.Add(new AsteroidFieldSetBillboardFadePercentage(Target, Target.BillboardFadePercentage, Field.BillboardFadePercentage));
        }
        if (Target.BillboardShape != Field.BillboardShape) {
            actions.Add(new AsteroidFieldSetBillboardShape(Target, Target.BillboardShape, Field.BillboardShape));
        }
        if (Target.BillboardSize != Field.BillboardSize) {
            actions.Add(new AsteroidFieldSetBillboardSize(Target, Target.BillboardSize, Field.BillboardSize));
        }
        if (Target.BillboardTint != Field.BillboardTint) {
            actions.Add(new AsteroidFieldSetBillboardTint(Target, Target.BillboardTint, Field.BillboardTint));
        }
        if (!DataEquality.ObjectEquals(Target.Band, Field.Band)) {
            actions.Add(new AsteroidFieldSetBand(Target, Target.Band, Field.Band));
        }
        if (!DataEquality.ListEquals(Target.DynamicAsteroids, Field.DynamicAsteroids)) {
            actions.Add(new AsteroidFieldSetDynamicAsteroids(Target, Target.DynamicAsteroids, Field.DynamicAsteroids));
        }
        if (!DataEquality.ListEquals(Target.ExclusionZones, Field.ExclusionZones)) {
            actions.Add(new AsteroidFieldSetExclusionZones(Target, Target.ExclusionZones, Field.ExclusionZones));
        }
        if (!DataEquality.ObjectEquals(Target.FieldLoot, Field.FieldLoot)) {
            actions.Add(new AsteroidFieldSetFieldLoot(Target, Target.FieldLoot, Field.FieldLoot));
        }
        if (!DataEquality.ListEquals(Target.LootZones, Field.LootZones)) {
            actions.Add(new AsteroidFieldSetLootZones(Target, Target.LootZones, Field.LootZones));
        }
        if (actions.Count == 0)
        {
            return new EditorNopAction("Asteroid Field Edit (No change)");
        }
        actions.Add(new AsteroidFieldRefresh(parent));
        return EditorAggregateAction.Create(actions.ToArray());
        // ReSharper restore CompareOfFloatsByEqualityOperator
    }
    public void Closed()
    {
        world.Dispose();
        renderer.Dispose();
        viewport.Dispose();
    }
}
