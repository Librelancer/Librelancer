// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Linq;
using System.Numerics;
using System.Text;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data.Schema.Effects;
using LibreLancer.Data.GameData.Items;
using LibreLancer.ImUI;
using LibreLancer.Render;
using LibreLancer.Render.Cameras;

namespace LancerEdit.GameContent;

public class ProjectileViewerTab : GameContentTab
{
    private static readonly DropdownOption[] camModes =
    {
        new DropdownOption("Arcball", Icons.Globe, CameraModes.Arcball),
        new DropdownOption("Walkthrough", Icons.StreetView, CameraModes.Walkthrough)
    };

    private readonly BeamsBuffer beams;
    private BeamBolt bolt;
    private readonly LookAtCamera camera = new();

    private int cameraMode;
    private string constEffect;
    private readonly GameDataContext context;

    private MunitionEquip currentMunition;
    private readonly MainWindow mw;
    private readonly MunitionEquip[] projectileList;
    private BeamSpear spear;
    private readonly Viewport3D viewport;

    public ProjectileViewerTab(MainWindow mw, GameDataContext context)
    {
        this.mw = mw;
        this.context = context;
        Title = "Projectiles";

        projectileList = context.GameData.Items.Equipment.OfType<MunitionEquip>().ToArray();
        viewport = new Viewport3D(mw);
        viewport.Background = Color4.Black;
        viewport.DefaultOffset = viewport.CameraOffset = new Vector3(0, 0, 20);
        viewport.ModelScale = 10f;
        viewport.Draw3D = DrawGL;
        viewport.ResetControls();
        beams = new BeamsBuffer(context.Resources, mw.RenderContext);
    }

    void DrawGL(int w, int h)
    {
        var rot = Matrix4x4.CreateRotationX(viewport.CameraRotation.Y) *
                  Matrix4x4.CreateRotationY(viewport.CameraRotation.X);
        var dirRot = Matrix4x4.CreateRotationX(viewport.ModelRotation.Y) *
                     Matrix4x4.CreateRotationY(viewport.ModelRotation.X);
        var norm = Vector3.TransformNormal(-Vector3.UnitZ, dirRot);
        var dir = Vector3.Transform(-Vector3.UnitZ, rot);
        var to = viewport.CameraOffset + dir * 10;
        if (viewport.Mode == CameraModes.Arcball)
            to = Vector3.Zero;
        camera.Update(w, h, viewport.CameraOffset, to, rot);
        mw.Commands.StartFrame(mw.RenderContext);
        beams.Begin(mw.Commands, context.Resources, camera);
        var position = Vector3.Zero;
        if (spear != null)
            beams.AddBeamSpear(position, norm, spear, float.MaxValue);
        else if (bolt != null) beams.AddBeamBolt(position, norm, bolt, float.MaxValue);
        beams.End();
        mw.Commands.DrawOpaque(mw.RenderContext);
        mw.RenderContext.DepthWrite = false;
        mw.Commands.DrawTransparent(mw.RenderContext);
        mw.RenderContext.DepthWrite = true;
        if (constEffect != null)
        {
            var debugText = new StringBuilder();
            debugText.AppendLine($"ConstEffect: {constEffect}");
            if (bolt != null) debugText.AppendLine($"Bolt: {bolt.Nickname}");
            if (spear != null) debugText.AppendLine($"Beam: {spear.Nickname}");
            mw.RenderContext.Renderer2D.DrawString("Arial", 10, debugText.ToString(), Vector2.One, Color4.White);
        }
    }

    public override void Draw(double elapsed)
    {
        ImGui.Columns(2);
        ImGui.BeginChild("##munitions");
        foreach (var m in projectileList)
            if (ImGui.Selectable(m.Nickname, currentMunition == m))
            {
                currentMunition = m;
                constEffect = m.Def.ConstEffect;
                bolt = m.ConstEffect_Bolt;
                spear = m.ConstEffect_Spear;
            }

        ImGui.EndChild();
        ImGui.NextColumn();
        ImGui.BeginChild("##rendering");
        ImGuiExt.DropdownButton("Camera Mode", ref cameraMode, camModes);
        viewport.Mode = (CameraModes) camModes[cameraMode].Tag;
        ImGui.SameLine();
        if (ImGui.Button("Reset Camera (Ctrl+R)"))
            viewport.ResetControls();
        viewport.Draw();

        ImGui.EndChild();
    }
}
