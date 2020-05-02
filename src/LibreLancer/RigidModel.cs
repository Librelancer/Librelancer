// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Numerics;
using LibreLancer.Utf;
using LibreLancer.Utf.Anm;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Cmp;

namespace LibreLancer
{
    public struct MeshDrawcall
    {
        public VertexBuffer Buffer;
        public bool HasScale;
        public Matrix4x4 Scale;
        public int StartIndex;
        public int PrimitiveCount;
        public int BaseVertex;
        public uint MaterialCrc;

        private Material cached;
        private MaterialAnim cachedAnim;
        private Material cachedAnimMat;
        public Material GetMaterial(ILibFile lib)
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
    public class VisualMesh
    {
        public float Radius;
        public Vector3 Center;
        public BoundingBox BoundingBox;
        public MeshDrawcall[][] Levels;
        public float[] Switch2;

        public void UpdateCamera(ICamera camera, ResourceManager res)
        {
            for (int i = 0; i < Levels.Length; i++)
            {
                if (Levels[i] == null) continue;
                var l = Levels[i];
                for (int j = 0; j < l.Length; j++)
                {
                    var mat = l[j].GetMaterial(res);
                    if(mat == null) mat = res.DefaultMaterial;
                    mat?.Update(camera);
                }
            }
        }
        public void DrawBuffer(int level, ResourceManager res, CommandBuffer buffer, Matrix4x4 world, ref Lighting lights, MaterialAnimCollection mc, Material overrideMat = null)
        {
            if (Levels == null || Levels.Length <= level) return;
            var l = Levels[level];
            if (l == null) return;
            WorldMatrixHandle wm = buffer.WorldBuffer.SubmitMatrix(ref world);
            for (int i = 0; i < l.Length; i++)
            {
                var dc = l[i];
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
                    z = RenderHelpers.GetZ(world, mat.Render.Camera.Position, Center);
                WorldMatrixHandle worldHandle = wm;
                if (dc.HasScale)
                {
                    Matrix4x4 tr = dc.Scale * world;
                    worldHandle = buffer.WorldBuffer.SubmitMatrix(ref tr); 
                }
                buffer.AddCommand(mat.Render,
                    ma,
                    worldHandle,
                    lights,
                    dc.Buffer,
                    PrimitiveTypes.TriangleList,
                    dc.BaseVertex,
                    dc.StartIndex,
                    dc.PrimitiveCount,
                    SortLayers.OBJECT
                );
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Mat4Source
        {
            public Matrix4x4 World;
            public Matrix4x4 Normal;
        }
        public unsafe void DrawImmediate(int level, ResourceManager res, RenderState renderState, Matrix4x4 world, ref Lighting lights, MaterialAnimCollection mc, Material overrideMat = null)
        {
            if (Levels == null || Levels.Length < level) return;
            var l = Levels[level];
            if (l == null) return;
            Mat4Source src;
            Mat4Source world2;
            src.World = world;
            Matrix4x4.Invert(world, out src.Normal);
            src.Normal = Matrix4x4.Transpose(src.Normal);
            for (int i = 0; i < l.Length; i++)
            {
                var dc = l[i];
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
                handle.ID = (ulong) handle.Source + (ulong)i  * 37 + (ulong)dc.MaterialCrc * 13 + (ulong)dc.PrimitiveCount * 37;
                if (dc.HasScale)
                {
                    world2.World = dc.Scale * world;
                    Matrix4x4.Invert(world2.World, out world2.Normal);
                    world2.Normal = Matrix4x4.Transpose(world2.Normal);
                    handle.Source = (Matrix4x4*) &world2;
                    handle.ID = (ulong) handle.Source + (ulong)i  * 37 + (ulong)dc.MaterialCrc * 13 + (ulong)dc.PrimitiveCount * 37;
                }
                Matrix4x4 tr = world;
                if (dc.HasScale)
                    tr = dc.Scale * world;
                mat.Render.World = handle;
                mat.Render.MaterialAnim = ma;
                mat.Render.Use(renderState, dc.Buffer.VertexType, ref lights);
                dc.Buffer.Draw(PrimitiveTypes.TriangleList, dc.BaseVertex, dc.StartIndex, dc.PrimitiveCount);
                
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
        
        private Matrix4x4 localTransform = Matrix4x4.Identity;
        public Matrix4x4 LocalTransform => localTransform;

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
            if (Mesh == null) return 0;
            return Mesh.Radius;
        }
        public void UpdateTransform(Matrix4x4 parent)
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
    }

    public class RigidModel
    {
        public string Path;
        public MaterialAnimCollection MaterialAnims;
        public AnmFile Animation;
        public RigidModelPart Root;
        public RigidModelPart[] AllParts;
        //Used for collision purposes only
        //Sur models use hash value of 0 instead of "Root" hash for 3db files
        public bool From3db; 
        //Lookup for multipart - NULL on single-part
        public Dictionary<string, RigidModelPart> Parts;
        public void UpdateTransform()
        {
            Root?.UpdateTransform(Matrix4x4.Identity);
        }
        public void Update(ICamera camera, TimeSpan globalTime, ResourceManager res)
        {
            MaterialAnims?.Update((float)globalTime.TotalSeconds);
            for (int i = 0; i < AllParts.Length; i++) AllParts[i].Mesh?.UpdateCamera(camera, res);
        }
        
        public float GetRadius()
        {
            if (Root == null) return 0;
            if (AllParts.Length == 1) return Root.GetRadius();
            var f = float.MinValue;
            for (int i = 0; i < AllParts.Length; i++)
            {
                var p = AllParts[i];
                if (p.Mesh == null) continue;
                var d = Vector3.Transform(p.Mesh.Center, p.LocalTransform).Length();
                var r = p.GetRadius();
                f = Math.Max(f, d + r);
            }
            return f;
        }

        public void DrawImmediate(RenderState rstate, ResourceManager res, Matrix4x4 world, ref Lighting lights, Material overrideMat = null)
        {
            for (int i = 0; i < AllParts.Length; i++)
            {
                if (AllParts[i].Active && AllParts[i].Mesh != null)
                {
                    var w = AllParts[i].LocalTransform * world;
                    AllParts[i].Mesh.DrawImmediate(0, res, rstate, w, ref lights, MaterialAnims, overrideMat);
                }
            }
        }

        public void DrawBuffer(int level, CommandBuffer buffer, ResourceManager res, Matrix4x4 world, ref Lighting lights, Material overrideMat = null)
        {
            for (int i = 0; i < AllParts.Length; i++)
            {
                if (AllParts[i].Active && AllParts[i].Mesh != null)
                {
                    var w = AllParts[i].LocalTransform * world;
                    AllParts[i].Mesh.DrawBuffer(level, res, buffer, w, ref lights, MaterialAnims, overrideMat);
                }
            }
        }

        static int GetLevel(float[] switch2, float levelDistance)
        {
            if (switch2 == null) return 0;
            for (int i = 0; i < switch2.Length; i++)
            {
                if (levelDistance <= switch2[i])
                    return i;
            }
            return int.MaxValue;
        }
        
        public void DrawBufferSwitch2(float dist, CommandBuffer buffer, ResourceManager res, Matrix4x4 world, ref Lighting lights, Material overrideMat = null)
        {
            for (int i = 0; i < AllParts.Length; i++)
            {
                if (AllParts[i].Active && AllParts[i].Mesh != null)
                {
                    var w = AllParts[i].LocalTransform * world;
                    AllParts[i].Mesh.DrawBuffer(GetLevel(AllParts[i].Mesh.Switch2, dist), res, buffer, w, ref lights, MaterialAnims, overrideMat);
                }
            }
        }
    }
}