// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Text;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.Data.Effects;
using LibreLancer.Data.Equipment;
using LibreLancer.ImUI;
using ImGuiNET;
using LibreLancer.Data.Universe;
using LibreLancer.Fx;

namespace LancerEdit
{
    public class ProjectileViewer : EditorTab
    {
        public static bool Create(MainWindow mw, out ProjectileViewer viewer)
        {
            viewer = null;
            string folder;
            if ((folder = FileDialog.ChooseFolder()) != null)
            {
                if (!GameConfig.CheckFLDirectory(folder)) return false;
                viewer = new ProjectileViewer(mw, folder);
                return true;
            }
            return false;
        }

        private FileSystem vfs;
        private EffectsIni effects;
        private EquipmentIni equipment;
        private Viewport3D viewport;
        private MainWindow mw;
        private LookAtCamera camera = new LookAtCamera();
        private Munition[] projectileList;
        internal ProjectileViewer(MainWindow mw, string folder)
        {
            this.mw = mw;
            Title = "Projectiles";
            vfs = FileSystem.FromFolder(folder);
            var flini = new FreelancerIni(vfs);
            var data = new FreelancerData(flini, vfs);
            effects = new EffectsIni();
            equipment = new EquipmentIni();
            data.Equipment = equipment;
            foreach (var path in flini.EffectPaths) {
                effects.AddIni(path, vfs);
            }
            foreach (var path in flini.EquipmentPaths)  {
                equipment.AddEquipmentIni(path, data);
            }
            projectileList = equipment.Munitions.Where(x => !string.IsNullOrWhiteSpace(x.ConstEffect)).OrderBy(x => x.Nickname).ToArray();
            var fxShapes =   new TexturePanels(flini.EffectShapesPath, vfs);
            foreach (var f in fxShapes.Files)
            {
                var path = vfs.Resolve(flini.DataPath + f);
                mw.Resources.LoadResourceFile(path);
            }
            viewport = new Viewport3D(mw);
            viewport.Background = Color4.Black;
            viewport.DefaultOffset = viewport.CameraOffset = new Vector3(0,0,20);
            viewport.ModelScale = 10f;
            fxPool = new ParticleEffectPool(mw.Commands);
            beams = new BeamsBuffer();
        }

        private Munition currentMunition;
        private Effect constEffect;
        private BeamBolt bolt;
        private BeamSpear beam;
        private ParticleEffectPool fxPool;
        private BeamsBuffer beams;
        public override void Draw()
        {
            ImGui.Columns(2);
            ImGui.BeginChild("##munitions");
            foreach (var m in projectileList)
            {
                if (ImGui.Selectable(m.Nickname, currentMunition == m))
                {
                    currentMunition = m;
                    constEffect = effects.FindEffect(m.ConstEffect);
                    bolt = effects.BeamBolts.FirstOrDefault(x =>
                        x.Nickname.Equals(constEffect.VisBeam, StringComparison.OrdinalIgnoreCase));
                    beam = effects.BeamSpears.FirstOrDefault(x =>
                        x.Nickname.Equals(constEffect.VisBeam, StringComparison.OrdinalIgnoreCase));
                    viewport.ResetControls();
                }
            }
            ImGui.EndChild();
            ImGui.NextColumn();
            ImGui.BeginChild("##rendering");
            viewport.Begin();
            Matrix4 rot = Matrix4.CreateRotationX(viewport.CameraRotation.Y) *
                          Matrix4.CreateRotationY(viewport.CameraRotation.X);
            var dirRot = Matrix4.CreateRotationX(viewport.Rotation.Y) * Matrix4.CreateRotationY(viewport.Rotation.X);
            var norm = Vector4.Transform(new Vector4(Vector3.Forward, 0), dirRot).Xyz;
            var dir = rot.Transform(Vector3.Forward);
            var to = viewport.CameraOffset + (dir * 10);
            camera.Update(viewport.RenderWidth, viewport.RenderHeight, viewport.CameraOffset, to, rot);
            mw.Commands.StartFrame(mw.RenderState);
            beams.Begin(mw.Commands, mw.Resources, camera);
            var position = Vector3.Zero;
            if (beam != null)
            {
                beams.AddBeamSpear(position, norm, beam);
            } 
            else if (bolt != null)
            {
                Vector2 tl, tr, bl, br;
                //CoordsFromTexture(bolt.HeadTexture, out tl, out tr, out bl, out br);
            }
            beams.End();
            fxPool.Draw(camera, null, mw.Resources, null);
            mw.Commands.DrawOpaque(mw.RenderState);
            mw.RenderState.DepthWrite = false;
            mw.Commands.DrawTransparent(mw.RenderState);
            mw.RenderState.DepthWrite = true;
            if (constEffect != null)
            {
                mw.Renderer2D.Start(viewport.RenderWidth, viewport.RenderHeight);
                var debugText = new StringBuilder();
                debugText.AppendLine($"ConstEffect: {constEffect.Nickname}");
                if (bolt != null) debugText.AppendLine($"Bolt: {bolt.Nickname}");
                if (beam != null) debugText.AppendLine($"Beam: {beam.Nickname}");
                mw.Renderer2D.DrawString("Arial", 10, debugText.ToString(), Vector2.One, Color4.White);
                mw.Renderer2D.Finish();
            }
            viewport.End();
            ImGui.EndChild();
        }
        
        
        
    }
}