// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Numerics;
using LibreLancer.Graphics;
using LibreLancer.Render;
using LibreLancer.Utf;
using LibreLancer.Utf.Anm;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Cmp;
using LibreLancer.World;
using SharpDX.Direct2D1.Effects;

namespace LibreLancer
{
    public struct MeshDrawcall
    {
        public int StartIndex;
        public int PrimitiveCount;
        public int BaseVertex;
        public uint MaterialCrc;

        private Material cached;
        private MaterialAnim cachedAnim;
        private Material cachedAnimMat;
        public Material GetMaterial(ResourceManager lib)
        {
            if (cached != null && cached.Loaded) return cached;
            cached = lib.FindMaterial(MaterialCrc);
            return cached;
        }
        public MaterialAnim GetMaterialAnim(MaterialAnimCollection mc)
        {
            if (mc == null) return null;
            if (cached == null || !cached.Loaded) return null;
            if (cached == cachedAnimMat && cachedAnim != null) return cachedAnim;
            if (mc.Anims.TryGetValue(cached.Name, out cachedAnim))
            {
                cachedAnimMat = cached;
                return cachedAnim;
            }
            return null;
        }
    }

    public class MeshLevel
    {
        public MeshDrawcall[] Drawcalls;
        public VMeshResource Resource;
        public VMeshOptimizeInfo Optimize;
        public float? Scale;
    }
    public class VisualMesh
    {
        public float Radius;
        public Vector3 Center;
        public BoundingBox BoundingBox;
        public MeshLevel[] Levels;
        public float[] Switch2;

        public void DrawBuffer(int level, ResourceManager res, CommandBuffer buffer, Matrix4x4 world, ref Lighting lights, MaterialAnimCollection mc, int userData = 0, Material overrideMat = null)
        {
            if (Levels == null || Levels.Length <= level) return;
            var l = Levels[level];
            if (l == null) return;
            if (l.Resource.IsDisposed) return;
            WorldMatrixHandle wm;
            if (l.Scale != null)
            {
                Matrix4x4 scaled = Matrix4x4.CreateScale(l.Scale.Value) * world;
                wm = buffer.WorldBuffer.SubmitMatrix(ref scaled);
            }
            else{
                wm = buffer.WorldBuffer.SubmitMatrix(ref world);
            }
            l.Resource.OptimizeIfNeeded(l.Optimize, res);
            for (int i = 0; i < l.Drawcalls.Length; i++)
            {
                var dc = l.Drawcalls[i];
                MaterialAnim ma = null;
                Material mat = overrideMat;
                if (mat == null)
                {
                    mat = dc.GetMaterial(res);
                    if (mat != null) ma = dc.GetMaterialAnim(mc);
                    else mat = res.DefaultMaterial;
                }
                float z = 0;
                if (mat.Render.IsTransparent)
                    z = RenderHelpers.GetZ(world, buffer.Camera.Position, Center);

                buffer.AddCommand(mat.Render,
                    ma,
                    wm,
                    lights,
                    l.Resource.VertexResource.VertexBuffer,
                    PrimitiveTypes.TriangleList,
                    l.Resource.VertexResource.BaseVertex + dc.BaseVertex,
                    l.Resource.VertexResource.StartIndex + dc.StartIndex,
                    dc.PrimitiveCount,
                    SortLayers.OBJECT,
                    z, null, 0, userData
                );
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Mat4Source
        {
            public Matrix4x4 World;
            public Matrix4x4 Normal;
        }

        private ulong drawN = ulong.MaxValue;
        public unsafe void DrawImmediate(int level, ResourceManager res, RenderContext renderContext, Matrix4x4 world, ref Lighting lights, MaterialAnimCollection mc, int userData = 0, Material overrideMat = null)
        {
            if (Levels == null || Levels.Length < level) return;
            var l = Levels[level];
            if (l == null) return;
            if (l.Resource.IsDisposed) return;
            Mat4Source src;
            if (l.Scale != null)
                src.World = Matrix4x4.CreateScale(l.Scale.Value) * world;
            else
                src.World = world;
            Matrix4x4.Invert(world, out src.Normal);
            src.Normal = Matrix4x4.Transpose(src.Normal);
            l.Resource.OptimizeIfNeeded(l.Optimize, res);
            for (int i = 0; i < l.Drawcalls.Length; i++)
            {
                var dc = l.Drawcalls[i];
                MaterialAnim ma = null;
                Material mat = overrideMat;
                if (mat == null)
                {
                    mat = dc.GetMaterial(res);
                    if (mat != null) ma = dc.GetMaterialAnim(mc);
                    else mat = res.DefaultMaterial;
                }
                WorldMatrixHandle handle;
                handle.Source = (Matrix4x4*) &src;
                handle.ID = ulong.MaxValue;
                mat.Render.World = handle;
                mat.Render.MaterialAnim = ma;
                mat.Render.Use(renderContext, l.Resource.VertexResource.VertexBuffer.VertexType, ref lights, userData);
                l.Resource.VertexResource.VertexBuffer.Draw(
                    PrimitiveTypes.TriangleList,
                    dc.BaseVertex + l.Resource.VertexResource.BaseVertex,
                    dc.StartIndex + l.Resource.VertexResource.StartIndex,
                    dc.PrimitiveCount);
            }
        }
    }

    public class RigidModelPart
    {
        public string Name;
        public string Path;
        public bool Active = true;
        public VisualMesh Mesh;
        public VMeshWire Wireframe;
        public List<RigidModelPart> Children;
        public List<Hardpoint> Hardpoints;
        public AbstractConstruct Construct;

        private Transform3D localTransform = Transform3D.Identity;
        public Transform3D LocalTransform => localTransform;

        public RigidModelPart Clone(bool withChildren = false)
        {
            var newp = new RigidModelPart()
            {
                Name = Name,
                Path = Path,
                Mesh = Mesh,
                Wireframe = Wireframe,
                Hardpoints = new List<Hardpoint>()
            };
            if (Construct != null) newp.Construct = Construct.Clone();
            foreach(var hp in Hardpoints)
                newp.Hardpoints.Add(new Hardpoint(hp.Definition, newp));
            if (withChildren && Children != null)
            {
                newp.Children = new List<RigidModelPart>();
                foreach(var c in Children) newp.Children.Add(c.Clone(true));
            }
            return newp;
        }
        public float GetRadius()
        {
            if (Mesh == null) return 1;
            return Mesh.Radius;
        }
        public void UpdateTransform(Transform3D parent)
        {
            if (Construct != null)
                localTransform = Construct.LocalTransform * parent;
            else
                localTransform = parent;
            if (Children != null)
            {
                foreach(var mp in Children)
                    mp.UpdateTransform(localTransform);
            }
        }

        internal void CalculateBoundingBox(ref Vector3 min, ref Vector3 max)
        {
            if (Mesh == null) return;
            var bmin = localTransform.Transform(Mesh.BoundingBox.Min);
            var bmax = localTransform.Transform(Mesh.BoundingBox.Max);
            min = Vector3.Min(min, bmin);
            max = Vector3.Max(max, bmax);
        }
    }

    public enum RigidModelSource
    {
        Compound,
        SinglePart,
        Sphere
    }

    public class RigidModel
    {
        public string Path;
        public MaterialAnimCollection MaterialAnims;
        public AnmFile Animation;
        public RigidModelPart Root;
        public RigidModelPart[] AllParts;
        //Sur models use hash value of 0 instead of "Root" hash for 3db files
        //Sphere models don't carry a VMeshWire
        public RigidModelSource Source;
        //Lookup for multipart - NULL on single-part
        public Dictionary<string, RigidModelPart> Parts;
        public void UpdateTransform()
        {
            Root?.UpdateTransform(Transform3D.Identity);
        }
        public void Update(double globalTime)
        {
            MaterialAnims?.Update((float)globalTime);
        }

        public BoundingBox GetBoundingBox()
        {
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);
            foreach(var p in AllParts)
                p.CalculateBoundingBox(ref min, ref max);
            return new BoundingBox(min, max);
        }

        public float GetRadius()
        {
            if (Root == null) return 1;
            if (AllParts.Length == 1) return Root.GetRadius();
            var f = float.MinValue;
            for (int i = 0; i < AllParts.Length; i++)
            {
                var p = AllParts[i];
                if (p.Mesh == null) continue;
                var d = p.LocalTransform.Transform(p.Mesh.Center).Length();
                var r = p.GetRadius();
                f = Math.Max(f, d + r);
            }
            return f;
        }

        public void DrawImmediate(RenderContext rstate, ResourceManager res, Matrix4x4 world, ref Lighting lights, int userData = 0, Material overrideMat = null)
        {
            for (int i = 0; i < AllParts.Length; i++)
            {
                if (AllParts[i].Active && AllParts[i].Mesh != null)
                {
                    var w = AllParts[i].LocalTransform.Matrix() * world;
                    AllParts[i].Mesh.DrawImmediate(0, res, rstate, w, ref lights, MaterialAnims, userData, overrideMat);
                }
            }
        }

        public void DrawBuffer(int level, CommandBuffer buffer, ResourceManager res, Matrix4x4 world, ref Lighting lights, int userData = 0, Material overrideMat = null)
        {
            for (int i = 0; i < AllParts.Length; i++)
            {
                if (AllParts[i].Active && AllParts[i].Mesh != null)
                {
                    var w = AllParts[i].LocalTransform.Matrix() * world;
                    AllParts[i].Mesh.DrawBuffer(level, res, buffer, w, ref lights, MaterialAnims, userData, overrideMat);
                }
            }
        }

        static int GetLevel(float[] switch2, float levelDistance)
        {
            if (switch2 == null) return 0;
            for (int i = 0; i < (switch2.Length - 1); i++)
            {
                if (levelDistance <= switch2[i + 1])
                    return i;
            }
            return int.MaxValue;
        }

        public void DrawBufferSwitch2(float dist, CommandBuffer buffer, ResourceManager res, Matrix4x4 world, ref Lighting lights, int userData = 0, Material overrideMat = null)
        {
            for (int i = 0; i < AllParts.Length; i++)
            {
                if (AllParts[i].Active && AllParts[i].Mesh != null)
                {
                    var w = AllParts[i].LocalTransform.Matrix() * world;
                    AllParts[i].Mesh.DrawBuffer(GetLevel(AllParts[i].Mesh.Switch2, dist), res, buffer, w, ref lights, MaterialAnims, userData, overrideMat);
                }
            }
        }
    }
}
