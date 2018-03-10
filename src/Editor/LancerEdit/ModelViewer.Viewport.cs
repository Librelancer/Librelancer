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
using LibreLancer;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Cmp;
using ImGuiNET;
namespace LancerEdit
{
    public partial class ModelViewer
	{
        RenderTarget2D renderTarget;
        int rw = -1, rh = -1;
        int rid = 0;

        void SetupViewport()
        {
            wireframeMaterial3db = new Material(res);
            wireframeMaterial3db.Dc = Color4.White;
            wireframeMaterial3db.DtName = ResourceManager.WhiteTextureName;
            normalsDebugMaterial = new Material(res);
            normalsDebugMaterial.Type = "NormalDebugMaterial";
            lighting = Lighting.Create();
            lighting.Enabled = true;
            lighting.Ambient = Color4.Black;
            var src = new SystemLighting();
            src.Lights.Add(new DynamicLight()
            {
                Light = new RenderLight()
                {
                    Kind = LightKind.Directional,
                    Direction = new Vector3(0, -1, 0),
                    Color = Color4.White
                }
            });
            src.Lights.Add(new DynamicLight()
            {
                Light = new RenderLight()
                {
                    Kind = LightKind.Directional,
                    Direction = new Vector3(0, 0, 1),
                    Color = Color4.White
                }
            });
            lighting.Lights.SourceLighting = src;
            lighting.Lights.SourceEnabled[0] = true;
            lighting.Lights.SourceEnabled[1] = true;
            lighting.NumberOfTilesX = -1;
            GizmoRender.Init(res);
        }
        void DoViewport()
        {
            var renderWidth = Math.Max(120, (int)ImGui.GetWindowWidth() - 15);
            var renderHeight = Math.Max(120, (int)ImGui.GetWindowHeight() - 40);
            //Generate render target
            if (rh != renderHeight || rw != renderWidth)
            {
                if (renderTarget != null)
                {
                    ImGuiHelper.DeregisterTexture(renderTarget);
                    renderTarget.Dispose();
                }
                renderTarget = new RenderTarget2D(renderWidth, renderHeight);
                rid = ImGuiHelper.RegisterTexture(renderTarget);
                rw = renderWidth;
                rh = renderHeight;
            }
            DrawGL(renderWidth, renderHeight);
            ImGui.ImageButton((IntPtr)rid, new Vector2(renderWidth, renderHeight),
                              Vector2.Zero, Vector2.One,
                              0,
                              Vector4.One, Vector4.One);
            if (ImGui.IsItemHovered(HoveredFlags.Default))
            {
                if (ImGui.IsMouseDragging(0, 1f))
                {
                    var delta = (Vector2)ImGui.GetMouseDragDelta(0, 1f);
                    rotation -= (delta / 64);
                    ImGui.ResetMouseDragDelta(0);
                }
                float wheel = ImGui.GetIO().MouseWheel;
                if (ImGui.GetIO().ShiftPressed)
                    zoom -= wheel * 10;
                else
                    zoom -= wheel * 40;
                if (zoom < 0) zoom = 0;
            }
        }
        void DrawGL(int renderWidth, int renderHeight)
        {
            //Set state
            renderTarget.BindFramebuffer();
            rstate.Cull = true;
            var cc = rstate.ClearColor;
            rstate.DepthEnabled = true;
            rstate.ClearColor = background;
            rstate.ClearAll();
            vps.Push(0, 0, renderWidth, renderHeight);
            //Draw Model
            var cam = new LookAtCamera();
            cam.Update(renderWidth, renderHeight, new Vector3(zoom, 0, 0), Vector3.Zero);
            drawable.Update(cam, TimeSpan.Zero, TimeSpan.Zero);
            if (viewMode != M_NONE)
            {
                buffer.StartFrame(rstate);
                if (drawable is CmpFile)
                    DrawCmp(cam, false);
                else
                    DrawSimple(cam, false);
                buffer.DrawOpaque(rstate);
                rstate.DepthWrite = false;
                buffer.DrawTransparent(rstate);
                rstate.DepthWrite = true;
            }
            if (doWireframe)
            {
                buffer.StartFrame(rstate);
                GL.PolygonOffset(1, 1);
                rstate.Wireframe = true;
                if (drawable is CmpFile)
                    DrawCmp(cam, true);
                else
                    DrawSimple(cam, false);
                GL.PolygonOffset(0, 0);
                buffer.DrawOpaque(rstate);
                rstate.Wireframe = false;
            }
            //Draw hardpoints
            DrawHardpoints(cam);
            //Restore state
            rstate.Cull = false;
            rstate.BlendMode = BlendMode.Normal;
            rstate.DepthEnabled = false;
            rstate.ClearColor = cc;
            RenderTarget2D.ClearBinding();
            vps.Pop();
        }

        void DrawHardpoints(ICamera cam)
        {
            var matrix = Matrix4.CreateRotationX(rotation.Y) * Matrix4.CreateRotationY(rotation.X);
            GizmoRender.Begin();
          
            foreach (var tr in gizmos)
            {
                if (tr.Enabled)
                    GizmoRender.AddGizmo((tr.Parent == null ? Matrix4.Identity : tr.Parent.Transform) * tr.Definition.Transform * matrix);
            }
            GizmoRender.RenderGizmos(cam, rstate);
        }

        void DrawSimple(ICamera cam, bool wireFrame)
        {
            Material mat = null;
            if (wireFrame || viewMode == M_FLAT)
            {
                mat = wireframeMaterial3db;
                mat.Update(cam);
            }
            else if (viewMode == M_NORMALS)
            {
                mat = normalsDebugMaterial;
                mat.Update(cam);
            }
            var matrix = Matrix4.CreateRotationX(rotation.Y) * Matrix4.CreateRotationY(rotation.X);
            ModelFile mdl = drawable as ModelFile;
            if (viewMode == M_LIT)
            {
                if (mdl != null)
                    mdl.DrawBufferLevel(mdl.Levels[GetLevel(mdl.Switch2, mdl.Levels.Length - 1)], buffer, matrix, ref lighting);
                else
                    drawable.DrawBuffer(buffer, matrix, ref lighting, mat);
            }
            else
            {
                if (mdl != null)
                    mdl.DrawBufferLevel(mdl.Levels[GetLevel(mdl.Switch2, mdl.Levels.Length - 1)], buffer, matrix, ref Lighting.Empty);
                else
                    drawable.DrawBuffer(buffer, matrix, ref Lighting.Empty, mat);
            }
        }

        int jColors = 0;
        void DrawCmp(ICamera cam, bool wireFrame)
        {
            var matrix = Matrix4.CreateRotationX(rotation.Y) * Matrix4.CreateRotationY(rotation.X);
            var cmp = (CmpFile)drawable;
            if (wireFrame || viewMode == M_FLAT)
            {
                foreach (var part in cmp.Parts)
                {
                    Material mat;
                    if (!partMaterials.TryGetValue(part.Key, out mat))
                    {
                        mat = new Material(res);
                        mat.DtName = ResourceManager.WhiteTextureName;
                        mat.Dc = initialCmpColors[jColors++];
                        if (jColors >= initialCmpColors.Length) jColors = 0;
                        partMaterials.Add(part.Key, mat);
                    }
                    mat.Update(cam);
                    part.Value.DrawBufferLevel(
                        GetLevel(part.Value.Model.Switch2, part.Value.Model.Levels.Length - 1),
                        buffer, matrix, ref Lighting.Empty, mat
                    );
                }
            }
            else if (viewMode == M_TEXTURED || viewMode == M_LIT)
            {
                if (viewMode == M_LIT)
                {
                    foreach(var part in cmp.Parts)
                        part.Value.DrawBufferLevel(
                            GetLevel(part.Value.Model.Switch2, part.Value.Model.Levels.Length - 1),
                            buffer, matrix, ref lighting
                        );
                }
                else
                {
                    foreach (var part in cmp.Parts)
                        part.Value.DrawBufferLevel(
                            GetLevel(part.Value.Model.Switch2, part.Value.Model.Levels.Length - 1),
                            buffer, matrix, ref Lighting.Empty
                        );
                }
            }
            else
            {
                normalsDebugMaterial.Update(cam);
                foreach (var part in cmp.Parts)
                    part.Value.DrawBufferLevel(
                        GetLevel(part.Value.Model.Switch2, part.Value.Model.Levels.Length - 1),
                        buffer, matrix, ref Lighting.Empty, normalsDebugMaterial
                    );
            }
        }
	}
}
