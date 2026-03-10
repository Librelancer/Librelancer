using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using LibreLancer.Data;
using LibreLancer.Data.IO;
using LibreLancer.Data.Schema.Universe;
using LibreLancer.Fx;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Primitives;
using LibreLancer.Physics;
using LibreLancer.Render;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Dfm;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Vms;

namespace LibreLancer.Resources;

public class GameResourceManager : ResourceManager, IDisposable
{
    public IGLWindow GLWindow;
    public long EstimatedTextureMemory { get; private set; }

    private Dictionary<uint, VMeshResource?> meshes = new();
    private Dictionary<uint, VMeshData?> meshDatas = new();
    private Dictionary<uint, string> meshFiles = new();
    private Dictionary<uint, Material?> materials = new();
    private Dictionary<uint, string> materialfiles = new();
    private Dictionary<string, Texture?> textures = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, ImageResource?> images = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string> texturefiles = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, ModelResource?> drawables = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, TextureShape?> shapes = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, Cursor?> cursors = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, TexFrameAnimation?> frameanims = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, ParticleLibrary?> particlelibs = new(StringComparer.OrdinalIgnoreCase);

    private List<string> loadedResFiles = [];
    private List<string>? preloadFiles = [];

    private Dictionary<int, QuadSphere> quadSpheres = new();
    private Dictionary<int, OpenCylinder> cylinders = new();

    private VertexResourceAllocator vertexResourceAllocator;

    public override VertexResource AllocateVertices(FVFVertex format, byte[] vertices, ushort[] indices)
    {
        if (!GLWindow.IsUiThread()) throw new InvalidOperationException();
        if (isDisposed) throw new ObjectDisposedException(nameof(GameResourceManager));
        return vertexResourceAllocator.Allocate(format, vertices, indices);
    }

    public override QuadSphere GetQuadSphere(int slices)
    {
        if (quadSpheres.TryGetValue(slices, out var sph))
        {
            return sph;
        }

        sph = new QuadSphere(GLWindow.RenderContext, slices);
        quadSpheres.Add(slices, sph);

        return sph;
    }

    public override OpenCylinder GetOpenCylinder(int slices)
    {
        if (cylinders.TryGetValue(slices, out var cyl))
        {
            return cyl;
        }

        cyl = new OpenCylinder(GLWindow.RenderContext, slices);
        cylinders.Add(slices, cyl);

        return cyl;
    }

    public override Dictionary<string, Texture?> TextureDictionary => textures;
    public override Dictionary<uint, Material?> MaterialDictionary => materials;

    public override Dictionary<string, TexFrameAnimation?> AnimationDictionary => frameanims;

    public GameResourceManager(IGLWindow g, FileSystem vfs) : base(vfs)
    {
        GLWindow = g;
        vertexResourceAllocator = new VertexResourceAllocator(g.RenderContext);
        DefaultMaterial = new Material(this)
        {
            Name = "$LL_DefaultMaterialName"
        };
        DefaultMaterial.Initialize(this);

        NullTexture = new Texture2D(g.RenderContext, 1, 1, false, SurfaceFormat.Bgra8);
        NullTexture.SetData(new byte[] { 0xFF, 0xFF, 0xFF, 0x0 });

        WhiteTexture = new Texture2D(g.RenderContext, 1, 1, false, SurfaceFormat.Bgra8);
        WhiteTexture.SetData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });

        GreyTexture = new Texture2D(g.RenderContext, 1, 1, false, SurfaceFormat.Bgra8);
        GreyTexture.SetData(new byte[] { 128, 128, 128, 0xFF });

        ConvexCollection = new ConvexMeshCollection(GetSur);
    }

    public GameResourceManager(GameResourceManager src) : this(src.GLWindow, src.VFS)
    {
        texturefiles = new Dictionary<string, string>(src.texturefiles, StringComparer.OrdinalIgnoreCase);
        shapes = new Dictionary<string, TextureShape?>(src.shapes, StringComparer.OrdinalIgnoreCase);
        materialfiles = new Dictionary<uint, string>(src.materialfiles);
        foreach (var mat in src.materials.Keys)
            materials[mat] = null;
        foreach (var tex in src.textures.Keys)
            textures[tex] = null;
    }

    public void Preload()
    {
        foreach (var file in preloadFiles)
        {
            LoadResourceFile(file);
        }

        preloadFiles = null;
    }

    public Cursor? GetCursor(string name)
    {
        return cursors[name];
    }

    public void AddCursor(Cursor c, string name)
    {
        c.Resources = this;
        cursors.Add(name, c);
    }

    public void AddShape(string name, TextureShape shape)
    {
        shapes.Add(name, shape);
    }

    public override bool TryGetShape(string name, out TextureShape? shape)
    {
        return shapes.TryGetValue(name, out shape);
    }

    public override bool TryGetFrameAnimation(string name, [MaybeNullWhen(false)] out TexFrameAnimation anim)
    {
        return frameanims.TryGetValue(name, out anim);
    }

    public void AddPreload(IEnumerable<string> files)
    {
        preloadFiles?.AddRange(files);
    }

    public bool TextureExists(string name)
    {
        return texturefiles.ContainsKey(name) || name == NullTextureName || name == WhiteTextureName;
    }

    public void AddTexture(string name, string filename)
    {
        using var stream = VFS.Open(filename);
        var dat = ImageLib.Generic.TextureFromStream(GLWindow.RenderContext, stream);
        textures.Add(name, dat);
        texturefiles.Add(name, filename);
        EstimatedTextureMemory += dat.EstimatedTextureMemory;
    }

    public void ClearTextures()
    {
        loadedResFiles = [];
        var keys = new string[textures.Count];
        textures.Keys.CopyTo(keys, 0);

        foreach (var k in keys)
        {
            if (textures[k] != null)
            {
                textures[k]!.Dispose();
                textures[k] = null;
            }
        }

        EstimatedTextureMemory = 0;
    }

    public void ClearMeshes()
    {
        loadedResFiles = [];
        var keys = new uint[meshes.Count];
        meshes.Keys.CopyTo(keys, 0);

        foreach (var k in keys)
        {
            if (meshes[k] != null)
            {
                meshes[k]!.Dispose();
                meshes[k] = null;
            }

            meshDatas[k] = null;
        }
    }

    public override ImageResource? FindImage(string name)
    {
        images.TryGetValue(name, out var outImage);
        return outImage;
    }

    public override Texture? FindTexture(string? name)
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException(nameof(GameResourceManager));
        }

        switch (name)
        {
            case null:
                return null;
            case NullTextureName:
                return NullTexture;
            case WhiteTextureName:
                return WhiteTexture;
            case GreyTextureName:
                return GreyTexture;
        }

        if (!textures.TryGetValue(name, out var outTexture))
        {
            return null;
        }

        if (outTexture != null)
        {
            return outTexture;
        }

        var file = texturefiles[name];
        FLLog.Debug("Resources", $"Reloading {name} from {file}");
        LoadResourceFile(file);
        outTexture = textures[name];
        return outTexture;
    }

    public override Material? FindMaterial(uint materialId)
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException(nameof(GameResourceManager));
        }

        materials.TryGetValue(materialId, out var m);
        return m;
    }

    public override VMeshResource? FindMesh(uint vMeshLibId)
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException(nameof(GameResourceManager));
        }

        if (!meshes.TryGetValue(vMeshLibId, out var vms))
        {
            return null;
        }

        if (vms != null) return vms;

        if (meshDatas.TryGetValue(vMeshLibId, out var d) && d != null)
        {
            d.Initialize(this);
            meshes[vMeshLibId] = d.Resource;
            vms = d.Resource;
        }
        else
        {
            FLLog.Debug("Resources", $"Reloading meshes from {meshFiles[vMeshLibId]}");
            LoadResourceFile(meshFiles[vMeshLibId], MeshLoadMode.GPU);
            vms = meshes[vMeshLibId];
        }

        return vms;
    }

    public override VMeshData? FindMeshData(uint vMeshLibId)
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException(nameof(GameResourceManager));
        }

        if (!meshDatas.TryGetValue(vMeshLibId, out var vms))
        {
            return null;
        }

        if (vms != null)
        {
            return vms;
        }

        LoadResourceFile(meshFiles[vMeshLibId], MeshLoadMode.CPU);
        return meshDatas[vMeshLibId];
    }

    public void AddResources(Utf.IntermediateNode node, string id)
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException(nameof(GameResourceManager));
        }

        Utf.UtfLoader.LoadResourceNode(node, this, out var mat, out var txm, out var vms);
        if (mat != null)
        {
            AddMaterials(mat, id);
        }

        if (txm != null)
        {
            AddTextures(txm, id);
            AddImages(txm, id);
        }

        if (vms != null)
        {
            AddMeshes(vms, MeshLoadMode.All, id);
        }
    }

    public IEnumerable<string> TexturesInFile(string file)
    {
        foreach (var tex in textures)
        {
            if (texturefiles[tex.Key] == file)
            {
                yield return tex.Key;
            }
        }
    }

    public void RemoveResourcesForId(string id)
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException(nameof(GameResourceManager));
        }

        List<string> removeTex = [];

        foreach (var tex in textures.Where(tex => texturefiles[tex.Key] == id))
        {
            texturefiles.Remove(tex.Key);
            tex.Value?.Dispose();
            removeTex.Add(tex.Key);
        }

        foreach (var key in removeTex)
        {
            textures.Remove(key);
            images.Remove(key);
        }

        List<uint> removeMats = [];

        foreach (var mat in materials.Where(mat => materialfiles[mat.Key] == id))
        {
            materialfiles.Remove(mat.Key);
            mat.Value?.Loaded = false;
            removeMats.Add(mat.Key);
        }

        foreach (var key in removeMats) materials.Remove(key);

        var removeMeshes = meshes.Where(x => meshFiles[x.Key] == id).ToArray();
        var removeMeshDatas = meshDatas.Where(x => meshFiles[x.Key] == id).ToArray();

        foreach (var m in removeMeshes)
        {
            m.Value?.Dispose();
            meshFiles.Remove(m.Key);
            meshes.Remove(m.Key);
        }

        foreach (var m in removeMeshDatas)
        {
            meshFiles.Remove(m.Key);
            meshDatas.Remove(m.Key);
        }

    }

    public override ParticleLibrary? GetParticleLibrary(string? filename)
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException(nameof(GameResourceManager));
        }

        if (string.IsNullOrEmpty(filename))
        {
            return null;
        }

        if (particlelibs.TryGetValue(filename, out var lib))
        {
            return lib;
        }

        var ale = new Utf.Ale.AleFile(filename, VFS.Open(filename));
        lib = new Fx.ParticleLibrary(this, ale);
        particlelibs.Add(filename, lib);

        return lib;
    }

    public override void LoadResourceFile(string? filename, MeshLoadMode meshMode = MeshLoadMode.GPU)
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException(nameof(GameResourceManager));
        }

        if (string.IsNullOrEmpty(filename))
        {
            return;
        }

        var fn = filename.ToLowerInvariant();
        if (loadedResFiles.Contains(fn)) return;
        using var stream = VFS.Open(filename);

        Utf.UtfLoader.LoadResourceFile(stream, filename, this, out var mat, out var txm, out var vms);

        if (mat != null) AddMaterials(mat, filename);
        if (txm != null) AddTextures(txm, filename);
        if (vms != null) AddMeshes(vms, meshMode, filename);
        if (vms == null && mat == null && txm == null)
            FLLog.Warning("Resources", $"Could not load resources from file '{filename}'");

        loadedResFiles.Add(fn);
    }

    private void AddImages(TxmFile t, string filename)
    {
        foreach (var tex in t.Textures)
        {
            if (!images.TryGetValue(tex.Key, out var existing) || existing == null)
            {
                var img = tex.Value.GetImageResource();
                if (img != null)
                    images[tex.Key] = img;
            }
        }
    }

    private void AddTextures(TxmFile t, string filename)
    {
        foreach (var tex in t.Textures)
        {
            if (!textures.TryGetValue(tex.Key, out var existing) || existing == null)
            {
                var v = tex.Value;
                v.Initialize(GLWindow.RenderContext);

                if (v.Texture != null)
                {
                    EstimatedTextureMemory += v.Texture.EstimatedTextureMemory;
                    textures[tex.Key] = v.Texture;
                    texturefiles.TryAdd(tex.Key, filename);
                }
            }
        }

        foreach (var anim in t.Animations)
        {
            frameanims.TryAdd(anim.Key, anim.Value);
        }
    }

    private void AddMaterials(MatFile m, string filename)
    {
        if (m.TextureLibrary != null)
        {
            AddTextures(m.TextureLibrary, filename);
        }

        foreach (var kv in m.Materials)
        {
            if (!materials.ContainsKey(kv.Key))
            {
                kv.Value.Initialize(this);
                materials.Add(kv.Key, kv.Value);
                materialfiles[kv.Key] = filename;
            }
        }
    }

    private void AddMeshes(VmsFile vms, MeshLoadMode mode, string filename)
    {
        bool isGpu = (mode & MeshLoadMode.GPU) == MeshLoadMode.GPU;
        bool isCpu = (mode & MeshLoadMode.CPU) == MeshLoadMode.CPU;

        foreach (var kv in vms.Meshes)
        {
            if (!meshes.TryGetValue(kv.Key, out var existingGpu) || (isGpu && existingGpu == null))
            {
                if (isGpu)
                {
                    kv.Value.Initialize(this);
                    meshes[kv.Key] = kv.Value.Resource;
                }
                else
                {
                    kv.Value.Resource = null;
                    meshes[kv.Key] = null;
                }

                meshFiles.TryAdd(kv.Key, filename);
            }

            if (!meshDatas.TryGetValue(kv.Key, out var existingCpu) || (isCpu && existingCpu == null))
            {
                meshDatas[kv.Key] = isCpu ? kv.Value : null;
                meshFiles.TryAdd(kv.Key, filename);
            }
        }
    }

    public override ModelResource? GetDrawable(string? filename, MeshLoadMode loadMode = MeshLoadMode.GPU)
    {
        if (filename is null)
        {
            return null;
        }

        if (isDisposed)
        {
            throw new ObjectDisposedException(nameof(GameResourceManager));
        }

        if (drawables.TryGetValue(filename, out var res))
        {
            res!.Drawable.ClearResources();

            if (res.Collision.Valid || (loadMode & MeshLoadMode.NoCollision) == MeshLoadMode.NoCollision ||
                res.Drawable is DfmFile)
            {
                return res;
            }

            var surPath = Path.ChangeExtension(filename, "sur");

            if (VFS.FileExists(surPath))
            {
                res.Collision = new CollisionMeshHandle()
                    { Sur = GetSur(surPath), FileId = ConvexCollection.UseFile(surPath) };
            }

            return res;
        }

        using var stream = VFS.Open(filename);
        var drawable = Utf.UtfLoader.LoadDrawable(stream, filename, this);
        CollisionMeshHandle handle = default;

        if (drawable == null)
        {
            drawables.Add(filename, null);
            return null;
        }

        if (drawable is not DfmFile && (loadMode & MeshLoadMode.NoCollision) != MeshLoadMode.NoCollision)
        {
            var surPath = Path.ChangeExtension(filename, "sur");

            if (VFS.FileExists(surPath))
            {
                handle = new CollisionMeshHandle()
                    { Sur = GetSur(surPath), FileId = ConvexCollection.UseFile(surPath) };
            }
        }

        switch (drawable)
        {
            /* Get Resources */
            case CmpFile file:
            {
                if (file.MaterialLibrary != null) AddMaterials(file.MaterialLibrary, filename);
                if (file.TextureLibrary != null) AddTextures(file.TextureLibrary, filename);
                if (file.VMeshLibrary != null) AddMeshes(file.VMeshLibrary, loadMode, filename);

                foreach (var mdl in file.Models.Values)
                {
                    if (mdl.MaterialLibrary != null) AddMaterials(mdl.MaterialLibrary, filename);
                    if (mdl.TextureLibrary != null) AddTextures(mdl.TextureLibrary, filename);
                    if (mdl.VMeshLibrary != null) AddMeshes(mdl.VMeshLibrary, loadMode, filename);
                }

                break;
            }
            case ModelFile file:
            {
                if (file.MaterialLibrary != null) AddMaterials(file.MaterialLibrary, filename);
                if (file.TextureLibrary != null) AddTextures(file.TextureLibrary, filename);
                if (file.VMeshLibrary != null) AddMeshes(file.VMeshLibrary, loadMode, filename);
                break;
            }
            case DfmFile dfm:
            {
                dfm.Initialize(this, GLWindow.RenderContext);
                if (dfm.MaterialLibrary != null) AddMaterials(dfm.MaterialLibrary, filename);
                if (dfm.TextureLibrary != null) AddTextures(dfm.TextureLibrary, filename);
                break;
            }
            case SphFile sph:
            {
                if (sph.MaterialLibrary != null) AddMaterials(sph.MaterialLibrary, filename);
                if (sph.TextureLibrary != null) AddTextures(sph.TextureLibrary, filename);
                if (sph.VMeshLibrary != null) AddMeshes(sph.VMeshLibrary, loadMode, filename);
                break;
            }
        }

        res = new ModelResource(drawable, handle);
        drawable.ClearResources();
        drawables.Add(filename, res);
        return res;
    }

    private bool isDisposed = false;

    public void Dispose()
    {
        if (isDisposed) return;
        isDisposed = true;

        // Textures
        foreach (var v in textures.Values)
        {
            if (v != null)
                v.Dispose();
        }

        NullTexture.Dispose();
        WhiteTexture.Dispose();
        GreyTexture.Dispose();
        // Vertex buffers
        vertexResourceAllocator.Dispose();
        ConvexCollection.Dispose();
    }
}
