// MIT License - Copyright (c) Callum McGing
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
using LibreLancer.Data;
using LibreLancer.Render;
using LibreLancer.Render.Cameras;
using LibreLancer.Render.Materials;
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
            normalsDebugMaterial = new Material(res);
            normalsDebugMaterial.Type = "NormalDebugMaterial";
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
            if (drawable is DF.DfmFile)
                skel = new DfmSkeletonManager((DF.DfmFile)drawable);
            GizmoRender.Init(res);
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
                /*surs.Add(GetSurModel(surfile.GetMesh(0,false), null, surPart));
                foreach(var hpid in surfile.HardpointIds)
                {
                    surs.Add(GetSurModel(surfile.GetMesh(hpid, true), null, surHardpoint));
                }*/
                surs.Add(GetSurModel(surfile.GetMesh(0), null, surPart));
            }
            else
            {
                foreach (var kv in vmsModel.Parts)
                {
                    var crc = CrcTool.FLModelCrc(kv.Key);
                    if (!surfile.HasShape(crc))
                        continue;
                    surs.Add(GetSurModel(surfile.GetMesh(crc), kv.Value, surPart));
                }
                /*Dictionary<Part, SurPart> surParts;
                var surHierarchy = ((CmpFile) drawable).ToSurHierarchy(out surParts);
                surfile.FillMeshHierarchy(surHierarchy);
                foreach (var kv in surParts)
                {
                    var mdl = new SurModel() {Part = kv.Key};
                    foreach (var hp in kv.Key.Model.Hardpoints)
                    {
                        var crc = CrcTool.FLModelCrc(hp.Name);
                        if (surfile.HardpointIds.Contains(crc))
                            surs.Add(GetSurModel(surfile.GetMesh(crc, true), kv.Key, surHardpoint));
                    }
                    if (kv.Value.DisplayMeshes != null)
                    {
                        foreach (var msh in kv.Value.DisplayMeshes)
                            AddVertices(mdl, msh);
                    }
                    mdl.Vertices = new VertexBuffer(typeof(VertexPositionColor), mdl.BuildVertices.Count);
                    mdl.Vertices.SetData(mdl.BuildVertices.ToArray());
                    mdl.BuildVertices = null;
                    mdl.Elements = new ElementBuffer(mdl.BuildIndices.Count);
                    mdl.Elements.SetData(mdl.BuildIndices.ToArray());
                    mdl.Vertices.SetElementBuffer(mdl.Elements);
                    mdl.BuildIndices = null;
                    surs.Add(mdl);
                }*/
            }
        }
        void AddVertices(SurModel mdl, LibreLancer.Physics.ConvexMesh mesh)
        {
            var baseVertex = mdl.BuildVertices.Count;
            var index = mdl.BuildIndices.Count;
            mdl.BuildVertices.AddRange(mesh.Vertices.Select(x => new VertexPositionColor(x, surPart)));
            mdl.BuildIndices.AddRange(mesh.Indices.Select(x => (short)x));
            mdl.Draws.Add(new SurDrawCall() { BaseVertex = baseVertex, Start = index, Count = mesh.Indices.Length / 3 });
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

        unsafe void RenderSurs(ICamera cam)
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
                mat.Camera = cam;
                var transform =  world * ((mdl.Part?.LocalTransform) ?? Matrix4x4.Identity);
                var whandle = new WorldMatrixHandle()
                {
                    ID = x,
                    Source = &transform,
                };
                x++;
                mat.World = whandle;
                mat.Use(rstate, new VertexPositionColor(), ref Lighting.Empty);
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
            modelViewport.Begin();
            DrawGL(modelViewport.RenderWidth, modelViewport.RenderHeight, true, doBackground);
            modelViewport.End();
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

        long fR = 0;
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
            var lookAtCam = new LookAtCamera();
            var rot = Matrix4x4.CreateRotationX(modelViewport.CameraRotation.Y) *
                Matrix4x4.CreateRotationY(modelViewport.CameraRotation.X);
            var dir = Vector3.Transform(-Vector3.UnitZ,rot);
            var to = modelViewport.CameraOffset + (dir * 10);
            if (modelViewport.Mode == CameraModes.Arcball) to = Vector3.Zero;
            lookAtCam.Update(renderWidth, renderHeight, modelViewport.CameraOffset, to, rot);
            lookAtCam.FrameNumber = fR++;
            ThnCamera tcam = null;
            float znear = 0;
            float zfar = 0;
            if(modelViewport.Mode == CameraModes.Cockpit) {
                var vp = new Rectangle(0, 0, renderWidth, renderHeight);
                tcam = new ThnCamera(vp);
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
                tcam.frameNo = fR++;
                tcam.Update();
                cam = tcam;
            }
            else {
                cam = lookAtCam;
            }
            if (showGrid && viewport && 
                !(drawable is SphFile) &&
                modelViewport.Mode != CameraModes.Starsphere)
            {
                GridRender.Draw(rstate, cam, _window.Config.GridColor);
            }
            _window.LineRenderer.StartFrame(cam, rstate);
            if (drawable is DF.DfmFile dfm)
            {
                skel.UploadBoneData(buffer.BonesBuffer);
                dfm.SetSkinning(skel.BodySkinning);
                dfm.Update(cam, 0, _window.TotalTime);
            }
            if (vmsModel != null) {
                vmsModel.UpdateTransform();
                vmsModel.Update(cam, _window.TotalTime, _window.Resources);
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
                        tcam.frameNo = fR++;
                        tcam.Update();
                    }
                    DrawSimple(cam, false);
                    buffer.DrawOpaque(rstate);
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
                RenderSurs(cam);
            //Draw hardpoints
            DrawHardpoints(cam);
            if (drawSkeleton) DrawSkeleton(cam);
        }
        
        void DrawSkeleton(ICamera cam)
        {
            var matrix = GetModelMatrix();
            GizmoRender.Scale = gizmoScale;
            GizmoRender.Begin();

            var dfm = (DF.DfmFile) drawable;
            foreach (var b in dfm.Bones)
            {
                GizmoRender.AddGizmo(b.Value.BoneToRoot * matrix, Color4.Green);
            }
            GizmoRender.RenderGizmos(cam, rstate);
        }
        Matrix4x4 GetModelMatrix()
        {
            return Matrix4x4.CreateRotationX(rotation.Y) * Matrix4x4.CreateRotationY(rotation.X);
        }
                
        void ResetViewport()
        {

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
                var mat = NormalLines.GetMaterial(_window.Resources, normalLength * 0.1f * vmsModel.GetRadius());
                mat.Update(cam);
                var lvl = GetLevel(part.Mesh.Switch2);
                part.Mesh.DrawImmediate(lvl, _window.Resources, _window.RenderContext, part.LocalTransform * matrix,
                    ref Lighting.Empty,
                    null, mat);
            }
        }


        Vector3[] GetBoxMesh(BoundingBox box)
        {
            var a = new Vector3(box.Min.X, box.Max.Y, box.Max.Z); 
            var b = new Vector3(box.Max.X, box.Max.Y, box.Max.Z); 
            var c = new Vector3(box.Max.X, box.Min.Y, box.Max.Z);
            var d = new Vector3(box.Min.X, box.Min.Y, box.Max.Z); 
            var e = new Vector3(box.Min.X, box.Max.Y, box.Min.Z); 
            var f = new Vector3(box.Max.X, box.Max.Y, box.Min.Z); 
            var g = new Vector3(box.Max.X, box.Min.Y, box.Min.Z); 
            var h = new Vector3(box.Min.X, box.Min.Y, box.Min.Z); 
            return new[]
            {
                a,b,
                c,d,
                e,f,
                g,h,
                a,e,
                b,f,
                c,g,
                d,h,
                a,d,
                b,c,
                e,h,
                f,g
            };
        }

        void DrawBox(BoundingBox box, Matrix4x4 mat, int color)
        {
            var lines = GetBoxMesh(box);
            var c = _window.LineRenderer.Color;
            color %= initialCmpColors.Length;
            _window.LineRenderer.Color = initialCmpColors[color];
            for (int i = 0; i < lines.Length / 2; i++)
            {
                _window.LineRenderer.DrawLine(
                    Vector3.Transform(lines[i * 2],mat),
                    Vector3.Transform(lines[i * 2 + 1],mat)
                );
            }
            _window.LineRenderer.Color = c;
        }
        
        void DrawVMeshWire(VMeshWire wires, Matrix4x4 mat, int color)
        {
            var c = _window.LineRenderer.Color;
            color %= initialCmpColors.Length;
            _window.LineRenderer.Color = initialCmpColors[color];
            for (int i = 0; i < wires.Lines.Length / 2; i++)
            {
                _window.LineRenderer.DrawLine(
                    Vector3.Transform(wires.Lines[i * 2],mat),
                    Vector3.Transform(wires.Lines[i * 2 + 1],mat)
                );
            }
            _window.LineRenderer.Color = c;
        }

        void DrawHardpoints(ICamera cam)
        {
            var matrix = GetModelMatrix();
            GizmoRender.Scale = gizmoScale;
            GizmoRender.Begin();
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
                        GizmoRender.AddGizmoArc(transform * (tr.Parent == null ? Matrix4x4.Identity : tr.Parent.LocalTransform) * matrix, min,max);
                    }
                    //draw (red for editing, light pink for normal)
                    GizmoRender.AddGizmo(transform * (tr.Parent == null ? Matrix4x4.Identity : tr.Parent.LocalTransform) * matrix, tr.Override != null ? Color4.Red : Color4.LightPink);
                }
            }
            GizmoRender.RenderGizmos(cam, rstate);
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
                mat.Update(cam);
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
                            if (jColors >= initialCmpColors.Length) jColors = 0;
                            partMaterials.Add(i, mat);
                        }

                        mat.Update(cam);
                        var lvl = GetLevel(part.Mesh.Switch2);
                        part.Mesh.DrawBuffer(lvl, _window.Resources, buffer, part.LocalTransform * matrix,
                            ref Lighting.Empty,
                            null, mat);
                    }
                }
                else if (viewMode == M_LIT)
                {
                    if (useDistance)
                        vmsModel.DrawBufferSwitch2(levelDistance, buffer, _window.Resources, matrix, ref lighting);
                    else
                        vmsModel.DrawBuffer(level, buffer, _window.Resources, matrix, ref lighting);
                }
                else
                {
                    if(useDistance)
                        vmsModel.DrawBufferSwitch2(levelDistance, buffer, _window.Resources, matrix, ref Lighting.Empty);
                    else
                        vmsModel.DrawBuffer(level, buffer, _window.Resources, matrix, ref Lighting.Empty, mat);
                }
            }
        }
    }
}
