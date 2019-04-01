// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Collections.Generic;
using LibreLancer;
using LibreLancer.Vertices;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Cmp;
using DF = LibreLancer.Utf.Dfm;
using ImGuiNET;
namespace LancerEdit
{
    public partial class ModelViewer
	{
        Viewport3D modelViewport;
        Viewport3D previewViewport;
        Viewport3D imageViewport;

        float gizmoScale;
        const float RADIUS_ONE = 21.916825f;
        void SetupViewport()
        {
            modelViewport = new Viewport3D(_window);
            modelViewport.MarginH = 60;
            modelViewport.DefaultOffset =
            modelViewport.CameraOffset = new Vector3(0, 0, drawable.GetRadius() * 2);
            modelViewport.ModelScale = drawable.GetRadius() / 2.6f;
            previewViewport = new Viewport3D(_window);
            imageViewport = new Viewport3D(_window);

            gizmoScale = drawable.GetRadius() / RADIUS_ONE;
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
            GizmoRender.Init(res);
        }

        List<SurModel> surs;
        class SurModel
        {
            public VertexBuffer Vertices;
            public ElementBuffer Elements;
            public List<SurDrawCall> Draws = new List<SurDrawCall>();
            public Part Part;
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
        void ProcessSur(LibreLancer.Physics.Sur.SurFile surfile)
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
                surs.Add(GetSurModel(surfile.GetMesh(0,false), null, surPart));
                foreach(var hpid in surfile.HardpointIds)
                {
                    surs.Add(GetSurModel(surfile.GetMesh(hpid, true), null, surHardpoint));
                }
            }
            else
            {
                Dictionary<uint, SurModel> crcLookup = new Dictionary<uint, SurModel>();
                foreach (var part in ((CmpFile)drawable).Parts) crcLookup.Add(CrcTool.FLModelCrc(part.ObjectName), new SurModel() { Part = part });
                foreach (var part in ((CmpFile)drawable).Parts)
                {
                    var crc = CrcTool.FLModelCrc(part.ObjectName);
                    foreach (var msh in surfile.GetMesh(crc, false)) AddVertices(crcLookup[msh.ParentCrc], msh);
                    foreach (var hp in part.Model.Hardpoints)
                    {
                        crc = CrcTool.FLModelCrc(hp.Name);
                        Color4 c = surHardpoint;
                        if (hp.Name.Equals("hpmount", StringComparison.OrdinalIgnoreCase))
                            c = surShield;
                        if (surfile.HardpointIds.Contains(crc))
                            surs.Add(GetSurModel(surfile.GetMesh(crc, true), null, c));
                    }
                }
                foreach(var mdl in crcLookup.Values) {
                    mdl.Vertices = new VertexBuffer(typeof(VertexPositionColor), mdl.BuildVertices.Count);
                    mdl.Vertices.SetData(mdl.BuildVertices.ToArray());
                    mdl.BuildVertices = null;
                    mdl.Elements = new ElementBuffer(mdl.BuildIndices.Count);
                    mdl.Elements.SetData(mdl.BuildIndices.ToArray());
                    mdl.Vertices.SetElementBuffer(mdl.Elements);
                    mdl.BuildIndices = null;
                    surs.Add(mdl);
                }
            }
        }
        void AddVertices(SurModel mdl, LibreLancer.Physics.Sur.ConvexMesh mesh)
        {
            var baseVertex = mdl.BuildVertices.Count;
            var index = mdl.BuildIndices.Count;
            mdl.BuildVertices.AddRange(mesh.Vertices.Select(x => new VertexPositionColor(x, surPart)));
            mdl.BuildIndices.AddRange(mesh.Indices.Select(x => (short)x));
            mdl.Draws.Add(new SurDrawCall() { BaseVertex = baseVertex, Start = index, Count = mesh.Indices.Length / 3 });
        }
        SurModel GetSurModel(LibreLancer.Physics.Sur.ConvexMesh[] meshes, Part part, Color4 color)
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

        void RenderSurs(ICamera cam)
        {
            var mat = wireframeMaterial3db.Render;
            var world = GetModelMatrix();
            rstate.Cull = false;
            rstate.DepthWrite = false;
            var bm = ((BasicMaterial)mat);
            bm.Oc = 1;
            bm.OcEnabled = true;
            GL.PolygonOffset(1, 1);
            foreach(var mdl in surs) {
                if (mdl.Hardpoint && !surShowHps) continue;
                if (!mdl.Hardpoint && !surShowHull) continue;
                mat.Camera = cam;
                if (mdl.Part != null) mat.World = mdl.Part.GetTransform(world);
                else mat.World = world;
                mat.Use(rstate, new VertexPositionColor(), ref Lighting.Empty);
                foreach (var dc in mdl.Draws)
                    mdl.Vertices.Draw(PrimitiveTypes.TriangleList, dc.BaseVertex, dc.Start, dc.Count);
            }
            GL.PolygonOffset(0, 0);
            bm.OcEnabled = false;
            rstate.DepthWrite = true;
            rstate.Cull = true;
        }

        void DoViewport()
        {
            modelViewport.Background = background;
            modelViewport.Begin();
            DrawGL(modelViewport.RenderWidth, modelViewport.RenderHeight);
            modelViewport.End();
            rotation = modelViewport.Rotation;
        }

        void DoPreview(int width, int height)
        {
            previewViewport.Background = renderBackground ? background : Color4.Black;
            previewViewport.Begin(width, height);
            DrawGL(width, height);
            previewViewport.End();
        }

        unsafe void RenderImage(string output)
        {
            try
            {
                TextureImport.LoadLibraries();
            }
            catch (Exception ex)
            {
                FLLog.Error("Render", "Could not load FreeImage");
                FLLog.Info("Exception Info", ex.Message + "\n" + ex.StackTrace);
                return;
            }
            imageViewport.Background = renderBackground ? background : Color4.TransparentBlack;
            imageViewport.Begin(imageWidth, imageHeight);
            DrawGL(imageWidth, imageHeight);
            imageViewport.End(false);
            byte[] data = new byte[imageWidth * imageHeight * 4];
            imageViewport.RenderTarget.GetData(data);
            using (var sfc = new TeximpNet.Surface(imageWidth, imageHeight, true))
            {
                byte* sfcData = (byte*)sfc.DataPtr;
                for (int i = 0; i < data.Length; i++) sfcData[i] = data[i];
                sfc.SaveToFile(TeximpNet.ImageFormat.PNG, output);
            }

        }

        long fR = 0;
        void DrawGL(int renderWidth, int renderHeight)
        {
            ICamera cam;
            //Draw Model
            var lookAtCam = new LookAtCamera();
            Matrix4 rot = Matrix4.CreateRotationX(modelViewport.CameraRotation.Y) *
                Matrix4.CreateRotationY(modelViewport.CameraRotation.X);
            var dir = rot.Transform(Vector3.Forward);
            var to = modelViewport.CameraOffset + (dir * 10);
            lookAtCam.Update(renderWidth, renderHeight, modelViewport.CameraOffset, to, rot);
            ThnCamera tcam = null;
            float znear = 0;
            float zfar = 0;
            if(doCockpitCam) {
                var vp = new Viewport(0, 0, renderWidth, renderHeight);
                tcam = new ThnCamera(vp);
                tcam.Transform.AspectRatio = renderWidth / (float)renderHeight;
                var tr = cameraPart.GetTransform(Matrix4.Identity);
                tcam.Transform.Orientation = Matrix4.CreateFromQuaternion(tr.ExtractRotation());
                tcam.Transform.Position = tr.Transform(Vector3.Zero);
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
            _window.DebugRender.StartFrame(cam, rstate);

            drawable.Update(cam, TimeSpan.Zero, TimeSpan.FromSeconds(_window.TotalTime));
            if (viewMode != M_NONE)
            {
                int drawCount = doCockpitCam ? 2 : 1;
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
                    if (drawable is CmpFile)
                        DrawCmp(cam, false);
                    else
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
            if(drawVMeshWire)
            {
                if (drawable is CmpFile)
                    WireCmp();
                else if (drawable is ModelFile)
                    Wire3db();
            }
            //Draw VMeshWire (if used)
            _window.DebugRender.Render();
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
            var df = (DF.DfmFile)drawable;
            foreach(var c in df.Constructs.Constructs)
            {
                var b1 = c.BoneA;
                var b2 = c.BoneB;
                if (string.IsNullOrEmpty(c.BoneB))
                {
                    b2 = c.ParentName;
                    b1 = c.BoneA;
                }
                var conMat = c.Rotation * Matrix4.CreateTranslation(c.Origin);
                DF.Bone bone1 = null;
                DF.Bone bone2 = null;
                foreach(var k in df.Parts.Values)
                {
                    if (k.objectName == b1) bone1 = k.Bone;
                    if (k.objectName == b2) bone2 = k.Bone;
                }
                if (bone1 == null || bone2 == null) continue;
                GizmoRender.AddGizmo(bone2.BoneToRoot * bone1.BoneToRoot * conMat * matrix);
            }
            /*foreach(var b in df.Bones.Values)
            {
                var tr = b.BoneToRoot;
                tr.Transpose();
                if (b.Construct != null)
                    tr *= b.Construct.Transform;
                tr *= matrix;
               
                GizmoRender.AddGizmo(tr, true, true);
            }*/
            GizmoRender.RenderGizmos(cam, rstate);
        }

        Matrix4 GetModelMatrix()
        {
            return Matrix4.CreateRotationX(rotation.Y) * Matrix4.CreateRotationY(rotation.X);
        }
                
        void ResetViewport()
        {

        }
        void WireCmp()
        {
            var cmp = (CmpFile)drawable;
            var matrix = GetModelMatrix();
            foreach (var part in cmp.Parts)
            {
                if(part.Model.VMeshWire != null) DrawVMeshWire(part.Model.VMeshWire, part.GetTransform(matrix));
            }
        }
        void Wire3db()
        {
            var model = (ModelFile)drawable;
            if (model.VMeshWire == null) return;
            var matrix = GetModelMatrix();
            DrawVMeshWire(model.VMeshWire, matrix);
        }
        void DrawVMeshWire(VMeshWire wires, Matrix4 mat)
        {
            var c = _window.DebugRender.Color;
            _window.DebugRender.Color = Color4.White;
            for (int i = 0; i < wires.Lines.Length / 2; i++)
            {
                _window.DebugRender.DrawLine(
                    mat.Transform(wires.Lines[i * 2]),
                    mat.Transform(wires.Lines[i * 2 + 1])
                );
            }
            _window.DebugRender.Color = c;
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
                    var transform = tr.Override ?? tr.Definition.Transform;
                    //highlight edited cube
                    if(tr.Override != null) {
                        GizmoRender.CubeColor = Color4.CornflowerBlue;
                        GizmoRender.CubeAlpha = 0.5f;
                    } else {
                        GizmoRender.CubeColor = Color4.Purple;
                        GizmoRender.CubeAlpha = 0.3f;
                    }
                    //arc
                    if(tr.Definition is RevoluteHardpointDefinition) {
                        var rev = (RevoluteHardpointDefinition)tr.Definition;
                        var min = tr.Override == null ? rev.Min : tr.EditingMin;
                        var max = tr.Override == null ? rev.Max : tr.EditingMax;
                        GizmoRender.AddGizmoArc(transform * (tr.Parent == null ? Matrix4.Identity : tr.Parent.Transform) * matrix, min,max);
                    }
                    //
                    GizmoRender.AddGizmo(transform * (tr.Parent == null ? Matrix4.Identity : tr.Parent.Transform) * matrix);
                }
            }
            GizmoRender.RenderGizmos(cam, rstate);
        }

        void DrawSimple(ICamera cam, bool wireFrame)
        {
            Material mat = null;
            var matrix = Matrix4.Identity;
            if (isStarsphere)
                matrix = Matrix4.CreateTranslation(cam.Position);
            else
                matrix = GetModelMatrix();
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
            var matrix = GetModelMatrix();
            if (isStarsphere)
                matrix = Matrix4.CreateTranslation(cam.Position);
            var cmp = (CmpFile)drawable;
            if (wireFrame || viewMode == M_FLAT)
            {
                for (int i = 0; i < cmp.Parts.Count; i++)
                {
                    var part = cmp.Parts[i];
                    if (part.Camera != null) continue;
                    Material mat;
                    if (!partMaterials.TryGetValue(i, out mat))
                    {
                        mat = new Material(res);
                        mat.DtName = ResourceManager.WhiteTextureName;
                        mat.Dc = initialCmpColors[jColors++];
                        if (jColors >= initialCmpColors.Length) jColors = 0;
                        partMaterials.Add(i, mat);
                    }
                    mat.Update(cam);
                    part.DrawBufferLevel(
                        GetLevel(part.Model.Switch2, part.Model.Levels.Length - 1),
                        buffer, matrix, ref Lighting.Empty, mat
                    );
                }
            }
            else if (viewMode == M_TEXTURED || viewMode == M_LIT)
            {
                if (viewMode == M_LIT)
                {
                    foreach (var part in cmp.Parts)
                    {
                        if (part.Camera == null)
                            part.DrawBufferLevel(
                                GetLevel(part.Model.Switch2, part.Model.Levels.Length - 1),
                                buffer, matrix, ref lighting
                            );
                    }
                }
                else
                {
                    foreach (var part in cmp.Parts)
                    {
                        if(part.Camera == null)
                        part.DrawBufferLevel(
                                GetLevel(part.Model.Switch2, part.Model.Levels.Length - 1),
                                buffer, matrix, ref Lighting.Empty
                        );
                    }
                }
            }
            else
            {
                normalsDebugMaterial.Update(cam);
                foreach (var part in cmp.Parts)
                {
                    if (part.Camera == null)
                        part.DrawBufferLevel(
                            GetLevel(part.Model.Switch2, part.Model.Levels.Length - 1),
                            buffer, matrix, ref Lighting.Empty, normalsDebugMaterial
                        );
                }
            }
        }


    }
}
