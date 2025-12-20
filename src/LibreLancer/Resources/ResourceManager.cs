// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data;
using LibreLancer.Data.IO;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Primitives;
using LibreLancer.Physics;
using LibreLancer.Render;
using LibreLancer.Sur;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Vms;

namespace LibreLancer.Resources;

//TODO: Allow for disposing and all that Jazz
public abstract class ResourceManager
{
    Dictionary<string, SurFile> surs = new Dictionary<string, SurFile>(StringComparer.OrdinalIgnoreCase);

    public abstract VertexResource AllocateVertices(FVFVertex format, byte[] vertices, ushort[] indices);
    public abstract QuadSphere GetQuadSphere(int slices);
    public abstract OpenCylinder GetOpenCylinder(int slices);
    public abstract Dictionary<string, Texture> TextureDictionary { get; }
    public abstract Dictionary<uint, Material> MaterialDictionary { get; }
    public abstract Dictionary<string, TexFrameAnimation> AnimationDictionary { get; }
    public Material DefaultMaterial;
    public Texture2D NullTexture;
    public Texture2D WhiteTexture;
    public Texture2D GreyTexture;
    public const string NullTextureName = "$$LIBRELANCER.Null";
    public const string WhiteTextureName = "$$LIBRELANCER.White";
    public const string GreyTextureName = "$$LIBRELANCER.Grey";
    public abstract Texture FindTexture(string name);
    public abstract ImageResource FindImage(string name);
    public abstract Material FindMaterial(uint materialId);
    public abstract VMeshResource FindMesh(uint vMeshLibId);
    public abstract VMeshData FindMeshData(uint vMeshLibId);
    public abstract ModelResource GetDrawable(string filename, MeshLoadMode loadMode = MeshLoadMode.GPU);
    public abstract void LoadResourceFile(string filename, MeshLoadMode loadMode = MeshLoadMode.GPU);
    public abstract Fx.ParticleLibrary GetParticleLibrary(string filename);

    public abstract bool TryGetShape(string name, out TextureShape shape);
    public abstract bool TryGetFrameAnimation(string name, out TexFrameAnimation anim);

    public ConvexMeshCollection ConvexCollection { get; protected set; }

    protected FileSystem VFS;
    protected ResourceManager(FileSystem vfs)
    {
        VFS = vfs;
    }

    protected SurFile GetSur(string filename)
    {
        // This shouldn't be needed?
        lock (surs)
        {
            SurFile sur;
            if (!surs.TryGetValue(filename, out sur))
            {
                using (var stream = VFS.Open(filename))
                {
                    sur = SurFile.Read(stream);
                }
                surs.Add(filename, sur);
            }
            return sur;
        }
    }
}
