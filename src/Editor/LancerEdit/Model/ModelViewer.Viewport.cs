// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using LibreLancer;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Cmp;
using DF = LibreLancer.Utf.Dfm;
using ImGuiNET;
using LancerEdit.Materials;
using LibreLancer.Data;
using LibreLancer.Data.GameData;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.ImUI;
using LibreLancer.Physics;
using LibreLancer.Render;
using LibreLancer.Render.Cameras;
using LibreLancer.Render.Materials;
using LibreLancer.Resources;
using LibreLancer.Sur;
using LibreLancer.Thn;

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
        void SetupViewport()
        {
            modelViewport = new Viewport3D(_window);
            modelViewport.MarginH = modelViewport.MarginW = 0;
            modelViewport.Draw3D = MainVpDraw;
            lookAtCam.ZRange.Y = 150000;

            ResetCamera();
            previewViewport = new Viewport3D(_window);
            previewViewport.Draw3D = ImageDraw;
            imageViewport = new Viewport3D(_window);
            imageViewport.Draw3D = ImageDraw;
            gizmoScale = DisplayRadius() / GizmoRender.ScaleFactor;
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
                d.Initialize(res, _window.RenderContext);
                skel = new DfmSkeletonManager(d);
            }
        }

        float DisplayRadius()
        {
            var r = vmsModel != null
                ? vmsModel.GetRadius()
                : (drawable as DF.DfmFile).GetRadius();
            if (r <= 0.00001f)
                r = 0.1f;
            return r;
        }

        void ResetCamera()
        {
            var rad = DisplayRadius();
            modelViewport.CameraOffset = modelViewport.DefaultOffset = new Vector3(0,0, rad  * 2);
            modelViewport.ModelScale = rad / 2.6f;
            // This calculation could be better
            if (modelViewport.ModelScale > 750f)
            {
                lookAtCam.ZRange.X = 0.3f;
            }
            if (modelViewport.ModelScale > 2000f)
            {
                lookAtCam.ZRange.X = 0.6f;
            }
            if (modelViewport.ModelScale > 8000f)
            {
                lookAtCam.ZRange.X = 0.9f;
            }
            if (modelViewport.ModelScale > 13000f)
            {
                lookAtCam.ZRange.X = 3f;
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
                surs.Add(GetSurModel(surfile.GetMesh(new ConvexMeshId(0,0)), null, surPart));
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
                    surs.Add(GetSurModel(surfile.GetMesh(new ConvexMeshId(crc,0)), kv.Value, surPart));
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
            mdl.Vertices = new VertexBuffer(_window.RenderContext,typeof(VertexPositionColor), verts.Count);
            mdl.Vertices.SetData<VertexPositionColor>(verts.ToArray());
            mdl.Elements = new ElementBuffer(_window.RenderContext, indices.Count);
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
            bm.Dc = Color4.Red;
            bm.Oc = 1;
            bm.OcEnabled = true;
            var x = (ulong) Environment.TickCount << 8;
            foreach(var mdl in surs) {
                if (mdl.Hardpoint && !surShowHps) continue;
                if (!mdl.Hardpoint && !surShowHull) continue;
                var transform =  ((mdl.Part?.LocalTransform) ?? Transform3D.Identity).Matrix() * world;
                var whandle = new WorldMatrixHandle()
                {
                    ID = x,
                    Source = &transform,
                };
                x++;
                mat.World = whandle;
                mat.Use(rstate, new VertexPositionColor(), ref Lighting.Empty, 0);
                rstate.PolygonOffset = new Vector2(1, 1);
                foreach (var dc in mdl.Draws)
                    mdl.Vertices.Draw(PrimitiveTypes.TriangleList, dc.BaseVertex, dc.Start, dc.Count);
                if (rstate.SupportsWireframe)
                {
                    rstate.PolygonOffset = new Vector2(2, 2);
                    rstate.Wireframe = true;
                    bm.Dc = Color4.Black;
                    mat.Use(rstate, new VertexPosition(), ref Lighting.Empty, 0);
                    foreach (var dc in mdl.Draws)
                        mdl.Vertices.Draw(PrimitiveTypes.TriangleList, dc.BaseVertex, dc.Start, dc.Count);
                }
                rstate.Wireframe = false;
            }
            rstate.PolygonOffset = new Vector2(0, 0);
            bm.Dc = Color4.Red;
            bm.OcEnabled = false;
            rstate.DepthWrite = true;
            rstate.Cull = true;
        }

        private ICamera lastCamera = null;

        void DoViewport()
        {
            modelViewport.Background = doBackground ? _window.Config.Background : Color4.Black;
            modelViewport.Draw();

            if (lastCamera != null && ManipulateHardpoint(lastCamera))
            {
                modelViewport.SetInputsEnabled(false);
            }
            else
            {
                modelViewport.SetInputsEnabled(true);
            }
            rotation = modelViewport.ModelRotation;
        }

        void MainVpDraw(int w, int h)
        {
            lastCamera = DrawGL(w,h, true, doBackground);
        }

        void ImageDraw(int w, int h)
        {
            DrawGL(w, h, false, renderBackground);
        }

        void DoPreview(int width, int height)
        {
            previewViewport.Background = renderBackground ? _window.Config.Background : Color4.Black;
            previewViewport.Draw(width, height);
        }

        unsafe void RenderImage(string output)
        {
            imageViewport.Background = renderBackground ? _window.Config.Background : Color4.TransparentBlack;
            imageViewport.DrawRenderTarget(imageWidth, imageHeight);
            var data = new Bgra8[imageWidth * imageHeight];
            imageViewport.RenderTarget.Texture.GetData(data);
            using var of = File.Create(output);
            LibreLancer.ImageLib.PNG.Save(of, imageWidth, imageHeight, data, true);
        }

        private LookAtCamera lookAtCam = new LookAtCamera();
        private ThnCamera tcam = new ThnCamera(new Rectangle(0, 0, 800, 600));

        ICamera DrawGL(int renderWidth, int renderHeight, bool viewport, bool bkgG)
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
                tcam.SetViewport(vp, (float)renderWidth / renderHeight);
                var tr = Transform3D.Identity;
                if (!string.IsNullOrEmpty(cameraPart.Construct?.ParentName))
                {
                    tr = cameraPart.Construct.LocalTransform *
                          vmsModel.Parts[cameraPart.Construct.ParentName].LocalTransform;
                } else if(cameraPart.Construct != null)
                    tr = cameraPart.Construct.LocalTransform;

                tcam.Object.Rotate = tr.Orientation;
                tcam.Object.Translate = tr.Position;
                znear = cameraPart.Camera.Znear;
                zfar = cameraPart.Camera.Zfar;
                tcam.Object.Camera.Znear = 0.001f;
                tcam.Object.Camera.Zfar = 1000;
                tcam.Object.Camera.FovH = MathHelper.RadiansToDegrees(cameraPart.Camera.Fovx);
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
                buffer.BonesBuffer.BeginStreaming();
                int a = 0, b = 0;
                skel.UploadBoneData(buffer.BonesBuffer, ref a, ref b);
                buffer.BonesBuffer.EndStreaming(b);
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
                        tcam.Object.Camera.Zfar = zfar;
                        tcam.Object.Camera.Znear = znear;
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
                        GridRender.Draw(rstate, lookAtCam, GridRender.DistanceScale(d), _window.Config.GridColor,lookAtCam.ZRange.X, lookAtCam.ZRange.Y);
                    }
                    rstate.DepthWrite = false;
                    buffer.DrawTransparent(rstate);
                    rstate.DepthWrite = true;
                }
            }
            else
            {
                if (showGrid && viewport &&
                    !(drawable is SphFile) &&
                    modelViewport.Mode != CameraModes.Starsphere)
                {
                    var d = Math.Abs(modelViewport.CameraOffset.Y);
                    GridRender.Draw(rstate, lookAtCam, GridRender.DistanceScale(d), _window.Config.GridColor,lookAtCam.ZRange.X, lookAtCam.ZRange.Y);
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
            return cam;
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
                    DrawVMeshWire(part.Wireframe, part.LocalTransform.Matrix() * matrix, i++);
                }
            }
        }

        void DrawBounds()
        {
            var matrix = GetModelMatrix();
            if (skel != null)
            {
                DrawBox(skel.Bounds, matrix, 0);
                return;
            }
            int i = 0;
            foreach (var part in vmsModel.AllParts)
            {
                if (part.Mesh != null)
                {
                    DrawBox(part.Mesh.BoundingBox, part.LocalTransform.Matrix() * matrix, i++);
                }
            }
        }

        private NormalsView normalVis;

        private float builtLength = 0;
        void BuildNormalVis()
        {
            var len = normalLength * 0.1f * vmsModel.GetRadius();
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (builtLength == len)
            {
                return;
            }
            normalVis?.Dispose();
            normalVis = new NormalsView(_window.RenderContext, drawable, res, len);
            builtLength = len;
        }


        unsafe void DrawNormals(ICamera cam)
        {
            BuildNormalVis();
            var matrix = GetModelMatrix();
            var x = (ulong) Environment.TickCount << 8;

            for (int i = 0; i < vmsModel.AllParts.Length; i++)
            {
                var part = vmsModel.AllParts[i];
                if (part.Mesh == null) continue;
                if (!part.Active) continue;
                var lvl = GetLevel(part.Mesh.Switch2);
                var n = drawable is ModelFile ? "ROOT" : part.Name;
                if (!normalVis.TryGet(n, lvl, out var start, out var len))
                    continue;
                var transform =  part.LocalTransform.Matrix() * matrix;
                var whandle = new WorldMatrixHandle()
                {
                    ID = x,
                    Source = &transform,
                };
                x++;
                wireframeMaterial3db.Render.World = whandle;
                wireframeMaterial3db.Render.Use(rstate, new VertexPositionColor(), ref Lighting.Empty, 0);
                normalVis.VertexBuffer.Draw(PrimitiveTypes.LineList, start, len / 2);
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
                    var transform = tr.Override ?? (tr.Hardpoint.HpTransformInfo.Matrix());
                    //arc
                    if(tr.Hardpoint.Definition is RevoluteHardpointDefinition) {
                        var rev = (RevoluteHardpointDefinition)tr.Hardpoint.Definition;
                        var min = tr.Override == null ? rev.Min : tr.EditingMin;
                        var max = tr.Override == null ? rev.Max : tr.EditingMax;
                        GizmoRender.AddGizmoArc(_window.LineRenderer, gizmoScale,transform * (tr.Parent?.LocalTransform ?? Transform3D.Identity).Matrix() * matrix, min,max);
                    }
                    //draw (red for editing, light pink for normal)
                    GizmoRender.AddGizmo(_window.LineRenderer, gizmoScale,transform * (tr.Parent?.LocalTransform ?? Transform3D.Identity).Matrix() * matrix, tr.Override != null ? Color4.Red : Color4.LightPink);
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
            int userData = modelViewport.Mode == CameraModes.Starsphere &&
                           (viewMode == M_TEXTURED || viewMode == M_LIT)
                ? BasicMaterial.ForceAlpha
                : 0;
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
                        part.Mesh.DrawBuffer(lvl, res, buffer, part.LocalTransform.Matrix() * matrix,
                            ref Lighting.Empty,
                            null, userData, mat);
                    }
                }
                else if (viewMode == M_LIT)
                {
                    if (useDistance)
                        vmsModel.DrawBufferSwitch2(levelDistance, buffer, res, matrix, ref lighting, userData);
                    else
                        vmsModel.DrawBuffer(level, buffer, res, matrix, ref lighting, userData);
                }
                else
                {
                    if(useDistance)
                        vmsModel.DrawBufferSwitch2(levelDistance, buffer, res, matrix, ref Lighting.Empty, userData);
                    else
                        vmsModel.DrawBuffer(level, buffer, res, matrix, ref Lighting.Empty, userData, mat);
                }
            }
        }
    }
}
