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
        Viewport3D modelViewport;

        void SetupViewport()
        {
            modelViewport = new Viewport3D(rstate, vps);
            modelViewport.Zoom = drawable.GetRadius() * 2;
            modelViewport.ZoomStep = modelViewport.Zoom / 3.26f;
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
            modelViewport.Background = background;
            modelViewport.Begin();
            DrawGL(modelViewport.RenderWidth, modelViewport.RenderHeight);
            modelViewport.End();
            rotation = modelViewport.Rotation;
        }
        void DrawGL(int renderWidth, int renderHeight)
        {
            //Draw Model
            var cam = new LookAtCamera();
            cam.Update(renderWidth, renderHeight, new Vector3(modelViewport.Zoom, 0, 0), Vector3.Zero);
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
                    DrawSimple(cam, true);
                GL.PolygonOffset(0, 0);
                buffer.DrawOpaque(rstate);
                rstate.Wireframe = false;
            }
            //Draw hardpoints
            DrawHardpoints(cam);
        }

        void DrawHardpoints(ICamera cam)
        {
            var matrix = Matrix4.CreateRotationX(rotation.Y) * Matrix4.CreateRotationY(rotation.X);
            GizmoRender.Begin();
          
            foreach (var tr in gizmos)
            {
                if (tr.Enabled)
                    GizmoRender.AddGizmo(tr.Definition.Transform * (tr.Parent == null ? Matrix4.Identity : tr.Parent.Transform) * matrix);
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
                    mdl.DrawBufferLevel(mdl.Levels[GetLevel(mdl.Switch2, mdl.Levels.Length - 1)], buffer, matrix, ref Lighting.Empty, mat);
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
