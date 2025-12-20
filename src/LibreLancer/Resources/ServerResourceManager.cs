using System;
using System.Collections.Generic;
using System.IO;
using LibreLancer.Data;
using LibreLancer.Data.IO;
using LibreLancer.Fx;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Primitives;
using LibreLancer.Physics;
using LibreLancer.Render;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Vms;

namespace LibreLancer.Resources;

public class ServerResourceManager : ResourceManager
{
    Dictionary<string, ModelResource> drawables = new Dictionary<string, ModelResource>(StringComparer.OrdinalIgnoreCase);
    public override Dictionary<string, Texture> TextureDictionary => throw new InvalidOperationException();
    public override Dictionary<uint, Material> MaterialDictionary => throw new InvalidOperationException();
    public override Dictionary<string, TexFrameAnimation> AnimationDictionary => throw new InvalidOperationException();

    public override VertexResource AllocateVertices(FVFVertex format, byte[] vertices, ushort[] indices)
    {
        throw new InvalidOperationException();
    }

    public ServerResourceManager(ConvexMeshCollection collection, FileSystem vfs) : base(vfs)
    {
        ConvexCollection = collection ?? new ConvexMeshCollection(GetSur);
    }


    public override OpenCylinder GetOpenCylinder(int slices) => throw new InvalidOperationException();
    public override ParticleLibrary GetParticleLibrary(string filename) => throw new InvalidOperationException();
    public override QuadSphere GetQuadSphere(int slices) => throw new InvalidOperationException();
    public override Material FindMaterial(uint materialId) => throw new InvalidOperationException();
    public override VMeshResource FindMesh(uint vMeshLibId) => throw new InvalidOperationException();
    public override VMeshData FindMeshData(uint vMeshLibId) => throw new InvalidOperationException();
    public override Texture FindTexture(string name) => throw new InvalidOperationException();
    public override ImageResource FindImage(string name) => throw new InvalidOperationException();

    public override bool TryGetShape(string name, out TextureShape shape) => throw new InvalidOperationException();
    public override bool TryGetFrameAnimation(string name, out TexFrameAnimation anim) => throw new InvalidOperationException();

    public override ModelResource GetDrawable(string filename, MeshLoadMode loadMode = MeshLoadMode.GPU)
    {
        if (!drawables.TryGetValue(filename, out var item))
        {
            using var stream = VFS.Open(filename);
            var drawable = Utf.UtfLoader.LoadDrawable(stream, filename, this);
            drawable?.ClearResources();
            CollisionMeshHandle handle = default;
            var surPath = Path.ChangeExtension(filename, "sur");
            if (VFS.FileExists(surPath))
                handle = new CollisionMeshHandle()
                    { Sur = GetSur(surPath), FileId = ConvexCollection.UseFile(surPath) };
            item = new ModelResource(drawable, handle);
            drawables.Add(filename, item);
        }
        return item;
    }

    public override void LoadResourceFile(string filename, MeshLoadMode loadMode = MeshLoadMode.GPU) { }
}
