﻿// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using LibreLancer;
using LibreLancer.Vertices;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Cmp;
using DF = LibreLancer.Utf.Dfm;
using ImGuiNET;
using LancerEdit.Materials;
using LibreLancer.Data;
using LibreLancer.Render;
using LibreLancer.Render.Cameras;
using LibreLancer.Render.Materials;
using LibreLancer.Sur;
using LibreLancer.Thn;
using SharpDX.MediaFoundation;

namespace LancerEdit
{
    public partial class ModelViewer
	{
        bool showGrid = false;
        Viewport3D modelViewport;
        Viewport3D previewViewport;
        Viewport3D imageViewport;
        private DfmSkeletonManager skel;

        float gizmoScale;
        float normalLength = 1;
        const float RADIUS_ONE = 21.916825f;
        void SetupViewport()
        {
            modelViewport = new Viewport3D(_window);
            modelViewport.MarginH = 60;
            ResetCamera();
            previewViewport = new Viewport3D(_window);
            imageViewport = new Viewport3D(_window);
            gizmoScale = 5;
            if (vmsModel != null) {
                gizmoScale = vmsModel.GetRadius() / RADIUS_ONE;
            }
            else if (drawable is DF.DfmFile dfm) {
                gizmoScale = dfm.GetRadius() / RADIUS_ONE;
            }
            wireframeMaterial3db = new Material(res);
            wireframeMaterial3db.Dc = Color4.White;
            wireframeMaterial3db.DtName = ResourceManager.WhiteTextureName;
            wireframeMaterial3db.Initialize(res);
            normalsDebugMaterial = new Material(new NormalDebugMaterial(res));
            lighting = Lighting.Create();
            lighting.Enabled = true;
            lighting.Ambient = Color3f.Black;
            var src = new SystemLighting();
            src.Lights.Add(new DynamicLight()
            {
                Light = new RenderLight()
                {
                    Kind = LightKind.Directional,
                    Direction = new Vector3(0, -1, 0),
                    Color = Color3f.White
                }
            });
            src.Lights.Add(new DynamicLight()
            {
                Light = new RenderLight()
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
            if (drawable is DF.DfmFile d)
            {
                d.Initialize(res);
                skel = new DfmSkeletonManager(d);
            }
        }

        void ResetCamera()
        {
            if (vmsModel != null)
            {
                modelViewport.DefaultOffset =
                    modelViewport.CameraOffset = new Vector3(0, 0, vmsModel.GetRadius() * 2);
                modelViewport.ModelScale = vmsModel.GetRadius() / 2.6f;

            }
            else
            {
                var rad = (drawable as DF.DfmFile).GetRadius();
                modelViewport.CameraOffset = modelViewport.DefaultOffset = new Vector3(0,0, rad  * 2);
                modelViewport.ModelScale = rad / 2.6f;
            }
            modelViewport.ResetControls();
        }

        List<SurModel> surs;
        class SurModel
        {
            public VertexBuffer Vertices;
            public ElementBuffer Elements;
            public List<SurDrawCall> Draws = new List<SurDrawCall>();
            public RigidModelPart Part;
            public bool Hardpoint;

            public List<VertexPositionColor> BuildVertices = new List<VertexPositionColor>();
            public List<short> BuildIndices = new List<short>();
        }

        class SurDrawCall
        {
            public int BaseVertex;
            public int Start;
            public int Count;
        }

        Color4 surPart = new Color4(1, 0, 0, 0.4f);
        Color4 surHardpoint = new Color4(128, 0, 128, 80);
        Color4 surShield = new Color4(100, 149, 237, 32);
        private static int jk = 0;
        void ProcessSur(SurFile surfile)
        {
            if(surs != null) {
                foreach(var mdl in surs)
                {
                    mdl.Vertices.Dispose();
                    mdl.Elements.Dispose();
                }
            }
            surs = new List<SurModel>();
            if((drawable is ModelFile))
            {
                surs.Add(GetSurModel(surfile.GetMesh(0), null, surPart));
                foreach (var hp in vmsModel.Root.Hardpoints)
                {
                    if (surfile.TryGetHardpoint(0, CrcTool.FLModelCrc(hp.Name), out var meshes))
                    {
                        surs.Add(GetSurModel(meshes, null, surHardpoint));
                    }
                }
            }
            else
            {
                foreach (var kv in vmsModel.Parts)
                {
                    var crc = CrcTool.FLModelCrc(kv.Key);
                    if (!surfile.HasShape(crc))
                        continue;
                    surs.Add(GetSurModel(surfile.GetMesh(crc), kv.Value, surPart));
                    foreach (var hp in kv.Value.Hardpoints)
                    {
                        if (surfile.TryGetHardpoint(crc, CrcTool.FLModelCrc(hp.Name), out var meshes))
                        {
                            surs.Add(GetSurModel(meshes, kv.Value, surHardpoint));
                        }
                    }
                }
            }
        }

        SurModel GetSurModel(LibreLancer.Physics.ConvexMesh[] meshes, RigidModelPart part, Color4 color)
        {
            var mdl = new SurModel() { Part = part };
            if (color != surPart) mdl.Hardpoint = true;
            var verts = new List<VertexPositionColor>();
            var indices = new List<short>();
            foreach(var m in meshes) {
                mdl.Draws.Add(new SurDrawCall() { BaseVertex = verts.Count, Start = indices.Count, Count = m.Indices.Length / 3 });
                verts.AddRange(m.Vertices.Select(x => new VertexPositionColor(x, color)));
                indices.AddRange(m.Indices.Select(x => (short)x));
            }
            mdl.Vertices = new VertexBuffer(typeof(VertexPositionColor), verts.Count);
            mdl.Vertices.SetData(verts.ToArray());
            mdl.Elements = new ElementBuffer(indices.Count);
            mdl.Elements.SetData(indices.ToArray());
            mdl.Vertices.SetElementBuffer(mdl.Elements);
            return mdl;
        }

        unsafe void RenderSurs()
        {
            var mat = wireframeMaterial3db.Render;
            var world = GetModelMatrix();

            rstate.Cull = false;
            rstate.DepthWrite = false;
            var bm = ((BasicMaterial)mat);
            bm.Oc = 1;
            bm.OcEnabled = true;
            rstate.PolygonOffset = new Vector2(1, 1);
            var x = (ulong) Environment.TickCount << 8;
            foreach(var mdl in surs) {
                if (mdl.Hardpoint && !surShowHps) continue;
                if (!mdl.Hardpoint && !surShowHull) continue;
                var transform =  ((mdl.Part?.LocalTransform) ?? Matrix4x4.Identity) * world;
                var whandle = new WorldMatrixHandle()
                {
                    ID = x,
                    Source = &transform,
                };
                x++;
                mat.World = whandle;
                mat.Use(rstate, new VertexPositionColor(), ref Lighting.Empty, 0);
                foreach (var dc in mdl.Draws)
                    mdl.Vertices.Draw(PrimitiveTypes.TriangleList, dc.BaseVertex, dc.Start, dc.Count);
            }
            rstate.PolygonOffset = new Vector2(0, 0);
            bm.OcEnabled = false;
            rstate.DepthWrite = true;
            rstate.Cull = true;
        }

        void DoViewport()
        {
            modelViewport.Background = doBackground ? _window.Config.Background : Color4.Black;
            modelViewport.MarginH = (int) (4.25 * ImGui.GetFontSize());
            if (modelViewport.Begin())
            {
                DrawGL(modelViewport.RenderWidth, modelViewport.RenderHeight, true, doBackground);
                modelViewport.End();
            }
            rotation = modelViewport.ModelRotation;
        }

        void DoPreview(int width, int height)
        {
            previewViewport.Background = renderBackground ? _window.Config.Background : Color4.Black;
            previewViewport.Begin(width, height);
            DrawGL(width, height, false, renderBackground);
            previewViewport.End();
        }

        unsafe void RenderImage(string output)
        {
            imageViewport.Background = renderBackground ? _window.Config.Background : Color4.TransparentBlack;
            imageViewport.Begin(imageWidth, imageHeight);
            DrawGL(imageWidth, imageHeight, false, renderBackground);
            imageViewport.End(false);
            byte[] data = new byte[imageWidth * imageHeight * 4];
            imageViewport.RenderTarget.Texture.GetData(data);
            for (int i = 0; i < data.Length; i += 4)
            {
                //Swap channels
                var x = data[i + 2];
                data[i + 2] = data[i];
                data[i] = x;
            }
            LibreLancer.ImageLib.PNG.Save(output, imageWidth, imageHeight, data);
        }

        private LookAtCamera lookAtCam = new LookAtCamera();
        private ThnCamera tcam = new ThnCamera(new Rectangle(0, 0, 800, 600));

        void DrawGL(int renderWidth, int renderHeight, bool viewport, bool bkgG)
        {
            if (_window.Config.BackgroundGradient && bkgG)
            {
                _window.RenderContext.Renderer2D.DrawVerticalGradient(new Rectangle(0,0,renderWidth,renderHeight), _window.Config.Background, _window.Config.Background2);
            }
            rstate.DepthEnabled = true;
            rstate.Cull = true;
            ICamera cam;
            //Draw Model
            var rot = Matrix4x4.CreateRotationX(modelViewport.CameraRotation.Y) *
                Matrix4x4.CreateRotationY(modelViewport.CameraRotation.X);
            var dir = Vector3.Transform(-Vector3.UnitZ,rot);
            var to = modelViewport.CameraOffset + (dir * 10);
            if (modelViewport.Mode == CameraModes.Arcball) to = Vector3.Zero;
            lookAtCam.Update(renderWidth, renderHeight, modelViewport.CameraOffset, to, rot);
            float znear = 0;
            float zfar = 0;
            if(modelViewport.Mode == CameraModes.Cockpit) {
                var vp = new Rectangle(0, 0, renderWidth, renderHeight);
                tcam.SetViewport(vp);
                tcam.Transform.AspectRatio = renderWidth / (float)renderHeight;
                var tr = Matrix4x4.Identity;
                if (!string.IsNullOrEmpty(cameraPart.Construct?.ParentName))
                {
                    tr = cameraPart.Construct.LocalTransform *
                         vmsModel.Parts[cameraPart.Construct.ParentName].LocalTransform;
                } else if(cameraPart.Construct != null)
                    tr = cameraPart.Construct.LocalTransform;
                tcam.Transform.Orientation = Matrix4x4.CreateFromQuaternion(tr.ExtractRotation());
                tcam.Transform.Position = Vector3.Transform(Vector3.Zero, tr);
                znear = cameraPart.Camera.Znear;
                zfar = cameraPart.Camera.Zfar;
                tcam.Transform.Znear = 0.001f;
                tcam.Transform.Zfar = 1000;
                tcam.Transform.FovH = MathHelper.RadiansToDegrees(cameraPart.Camera.Fovx);
                tcam.frameNo++;
                tcam.Update();
                cam = tcam;
            }
            else {
                cam = lookAtCam;
            }
            buffer.Camera = cam;
            rstate.SetCamera(cam);
            _window.LineRenderer.StartFrame(rstate);
            if (drawable is DF.DfmFile dfm)
            {
                skel.UploadBoneData(buffer.BonesBuffer);
                dfm.SetSkinning(skel.BodySkinning);
            }
            if (vmsModel != null) {
                vmsModel.UpdateTransform();
                vmsModel.Update(_window.TotalTime);
            }
            if (viewMode != M_NONE)
            {
                int drawCount = (modelViewport.Mode == CameraModes.Cockpit) ? 2 : 1;
                for (int i = 0; i < drawCount; i++)
                {
                    buffer.StartFrame(rstate);
                    if (i == 1) {
                        rstate.ClearDepth();
                        tcam.Transform.Zfar = zfar;
                        tcam.Transform.Znear = znear;
                        tcam.frameNo++;
                        tcam.Update();
                    }
                    DrawSimple(cam, false);
                    buffer.DrawOpaque(rstate);
                    if (showGrid && viewport &&
                        !(drawable is SphFile) &&
                        modelViewport.Mode != CameraModes.Starsphere)
                    {
                        var d = Math.Abs(modelViewport.CameraOffset.Y);
                        GridRender.Draw(rstate, GridRender.DistanceScale(d), _window.Config.GridColor,lookAtCam.ZRange.X, lookAtCam.ZRange.Y);
                    }
                    rstate.DepthWrite = false;
                    buffer.DrawTransparent(rstate);
                    rstate.DepthWrite = true;
                }
            }
            if (doWireframe)
            {
                buffer.StartFrame(rstate);
                rstate.PolygonOffset = new Vector2(1, 1);
                rstate.Wireframe = true;
                DrawSimple(cam, true);
                rstate.PolygonOffset = new Vector2(0, 0);
                buffer.DrawOpaque(rstate);
                rstate.Wireframe = false;
            }
            if (drawNormals) DrawNormals(cam);
            if (drawVMeshWire) DrawWires();
            if (doBounds) DrawBounds();
            //Draw VMeshWire (if used)
            _window.LineRenderer.Render();
            //Draw Sur
            if (surs != null)
                RenderSurs();
            //Draw hardpoints
            DrawHardpoints(cam);
            if (drawSkeleton) DrawSkeleton(cam);
        }

        void DrawSkeleton(ICamera cam)
        {
            var matrix = GetModelMatrix();
            _window.LineRenderer.SkeletonHighlight = skeletonHighlight;
            _window.LineRenderer.StartFrame(_window.RenderContext);
            skel.DebugDraw(_window.LineRenderer, matrix, DfmDrawMode.DebugBonesHardpoints);
            _window.LineRenderer.Render();
        }

        Matrix4x4 GetModelMatrix()
        {
            return Matrix4x4.CreateRotationX(rotation.Y) * Matrix4x4.CreateRotationY(rotation.X);
        }

        void DrawWires()
        {
            var matrix = GetModelMatrix();
            int i = 0;
            foreach (var part in vmsModel.AllParts)
            {
                if (part.Wireframe != null)
                {
                    DrawVMeshWire(part.Wireframe, part.LocalTransform * matrix, i++);
                }
            }
        }

        void DrawBounds()
        {
            var matrix = GetModelMatrix();
            int i = 0;
            foreach (var part in vmsModel.AllParts)
            {
                if (part.Mesh != null)
                {
                    DrawBox(part.Mesh.BoundingBox, part.LocalTransform * matrix, i++);
                }
            }
        }

        void DrawNormals(ICamera cam)
        {
            var matrix = GetModelMatrix();
            for (int i = 0; i < vmsModel.AllParts.Length; i++)
            {
                var part = vmsModel.AllParts[i];
                if (part.Mesh == null) continue;
                if (!part.Active) continue;
                var mat = NormalLinesMaterial.GetMaterial(res, normalLength * 0.1f * vmsModel.GetRadius());
                var lvl = GetLevel(part.Mesh.Switch2);
                part.Mesh.DrawImmediate(lvl, res, _window.RenderContext, part.LocalTransform * matrix,
                    ref Lighting.Empty,
                    null, mat);
            }
        }

        void DrawBox(BoundingBox box, Matrix4x4 mat, int color)
        {
            EditorPrimitives.DrawBox(_window.LineRenderer, box, mat,
                initialCmpColors[color % initialCmpColors.Length]);
        }

        void DrawVMeshWire(VMeshWire wires, Matrix4x4 mat, int color)
        {
            color %= initialCmpColors.Length;
            var vms = res.FindMesh(wires.MeshCRC);
            if (vms != null)
            {
                _window.LineRenderer.DrawVWire(wires, vms.VertexResource, mat, initialCmpColors[color]);
            }
        }

        void DrawHardpoints(ICamera cam)
        {
            var matrix = GetModelMatrix();
            _window.LineRenderer.StartFrame(_window.RenderContext);
            foreach (var tr in gizmos)
            {
                if (tr.Enabled || tr.Override != null)
                {
                    var transform = tr.Override ?? tr.Hardpoint.HpTransformInfo;
                    //arc
                    if(tr.Hardpoint.Definition is RevoluteHardpointDefinition) {
                        var rev = (RevoluteHardpointDefinition)tr.Hardpoint.Definition;
                        var min = tr.Override == null ? rev.Min : tr.EditingMin;
                        var max = tr.Override == null ? rev.Max : tr.EditingMax;
                        GizmoRender.AddGizmoArc(_window.LineRenderer, gizmoScale,transform * (tr.Parent == null ? Matrix4x4.Identity : tr.Parent.LocalTransform) * matrix, min,max);
                    }
                    //draw (red for editing, light pink for normal)
                    GizmoRender.AddGizmo(_window.LineRenderer, gizmoScale,transform * (tr.Parent == null ? Matrix4x4.Identity : tr.Parent.LocalTransform) * matrix, tr.Override != null ? Color4.Red : Color4.LightPink);
                }
            }
            _window.LineRenderer.Render();
        }

        private int jColors = 0;

        void DrawSimple(ICamera cam, bool wireFrame)
        {
            Material mat = null;
            var matrix = Matrix4x4.Identity;
            if (modelViewport.Mode == CameraModes.Starsphere)
                matrix = Matrix4x4.CreateTranslation(cam.Position);
            else
                matrix = GetModelMatrix();
            if (viewMode == M_NORMALS)
            {
                mat = normalsDebugMaterial;
            }
            if (drawable is DF.DfmFile dfm)
            {
                if (viewMode == M_LIT)
                {
                    dfm.DrawBuffer(buffer, matrix, ref lighting);
                }
                else
                {
                    dfm.DrawBuffer(buffer, matrix, ref Lighting.Empty, mat);
                }
            }
            else
            {
                if (wireFrame || viewMode == M_FLAT)
                {
                    for (int i = 0; i < vmsModel.AllParts.Length; i++)
                    {
                        var part = vmsModel.AllParts[i];
                        if (part.Mesh == null) continue;
                        if (!part.Active) continue;
                        if (!partMaterials.TryGetValue(i, out mat))
                        {
                            mat = new Material(res);
                            mat.Name = "FLAT PART MATERIAL";
                            mat.DtName = ResourceManager.WhiteTextureName;
                            mat.Dc = initialCmpColors[jColors++];
                            mat.Initialize(res);
                            if (jColors >= initialCmpColors.Length) jColors = 0;
                            partMaterials.Add(i, mat);
                        }

                        var lvl = GetLevel(part.Mesh.Switch2);
                        part.Mesh.DrawBuffer(lvl, res, buffer, part.LocalTransform * matrix,
                            ref Lighting.Empty,
                            null, mat);
                    }
                }
                else if (viewMode == M_LIT)
                {
                    if (useDistance)
                        vmsModel.DrawBufferSwitch2(levelDistance, buffer, res, matrix, ref lighting);
                    else
                        vmsModel.DrawBuffer(level, buffer, res, matrix, ref lighting);
                }
                else
                {
                    if(useDistance)
                        vmsModel.DrawBufferSwitch2(levelDistance, buffer, res, matrix, ref Lighting.Empty);
                    else
                        vmsModel.DrawBuffer(level, buffer, res, matrix, ref Lighting.Empty, mat);
                }
            }
        }
    }
}
