using System;
using System.Collections.Generic;
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

    Dictionary<uint, VMeshResource> meshes = new();
    Dictionary<uint, VMeshData> meshDatas = new();
    Dictionary<uint, string> meshFiles = new();
    Dictionary<uint, Material> materials = new();
    Dictionary<uint, string> materialfiles = new();
    Dictionary<string, Texture> textures = new(StringComparer.OrdinalIgnoreCase);
    Dictionary<string, ImageResource> images = new(StringComparer.OrdinalIgnoreCase);
    Dictionary<string, string> texturefiles = new(StringComparer.OrdinalIgnoreCase);
    Dictionary<string, ModelResource> drawables = new(StringComparer.OrdinalIgnoreCase);
    Dictionary<string, TextureShape> shapes = new(StringComparer.OrdinalIgnoreCase);
    Dictionary<string, Cursor> cursors = new(StringComparer.OrdinalIgnoreCase);
    Dictionary<string, TexFrameAnimation> frameanims = new(StringComparer.OrdinalIgnoreCase);
    Dictionary<string, ParticleLibrary> particlelibs = new(StringComparer.OrdinalIgnoreCase);

    List<string> loadedResFiles = new List<string>();
    List<string> preloadFiles = new List<string>();

    Dictionary<int, QuadSphere> quadSpheres = new Dictionary<int, QuadSphere>();
    Dictionary<int, OpenCylinder> cylinders = new Dictionary<int, OpenCylinder>();

    private VertexResourceAllocator vertexResourceAllocator;

    public override VertexResource AllocateVertices(FVFVertex format, byte[] vertices, ushort[] indices)
    {
        if (!GLWindow.IsUiThread()) throw new InvalidOperationException();
        if (isDisposed) throw new ObjectDisposedException(nameof(GameResourceManager));
        return vertexResourceAllocator.Allocate(format, vertices, indices);
    }

    public override QuadSphere GetQuadSphere(int slices) {
        QuadSphere sph;
        if(!quadSpheres.TryGetValue(slices, out sph)) {
            sph = new QuadSphere(GLWindow.RenderContext, slices);
            quadSpheres.Add(slices, sph);
        }
        return sph;
    }

    public override OpenCylinder GetOpenCylinder(int slices)
    {
        OpenCylinder cyl;
        if (!cylinders.TryGetValue(slices, out cyl))
        {
            cyl = new OpenCylinder(GLWindow.RenderContext, slices);
            cylinders.Add(slices, cyl);
        }
        return cyl;
    }
    public override Dictionary<string, Texture> TextureDictionary
    {
        get
        {
            return textures;
        }
    }
    public override Dictionary<uint, Material> MaterialDictionary
    {
        get
        {
            return materials;
        }
    }
    public override Dictionary<string, TexFrameAnimation> AnimationDictionary => frameanims;

    public GameResourceManager(IGLWindow g, FileSystem vfs) : base(vfs)
    {
        GLWindow = g;
        vertexResourceAllocator = new VertexResourceAllocator(g.RenderContext);
        DefaultMaterial = new Material(this);
        DefaultMaterial.Name = "$LL_DefaultMaterialName";
        DefaultMaterial.Initialize(this);

        NullTexture = new Texture2D(g.RenderContext, 1, 1, false, SurfaceFormat.Bgra8);
        NullTexture.SetData(new byte[] { 0xFF, 0xFF, 0xFF, 0x0 });

        WhiteTexture = new Texture2D(g.RenderContext, 1, 1, false, SurfaceFormat.Bgra8);
        WhiteTexture.SetData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });

        GreyTexture = new Texture2D(g.RenderContext, 1,1, false, SurfaceFormat.Bgra8);
        GreyTexture.SetData(new byte[] { 128, 128, 128, 0xFF});

        ConvexCollection = new ConvexMeshCollection(GetSur);
    }

    public GameResourceManager(GameResourceManager src) : this(src.GLWindow, src.VFS)
    {
        texturefiles = new Dictionary<string, string>(src.texturefiles, StringComparer.OrdinalIgnoreCase);
        shapes = new Dictionary<string, TextureShape>(src.shapes, StringComparer.OrdinalIgnoreCase);
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

    public Cursor GetCursor(string name)
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
    public override bool TryGetShape(string name, out TextureShape shape)
    {
        return shapes.TryGetValue(name, out shape);
    }

    public override bool TryGetFrameAnimation(string name, out TexFrameAnimation anim)
    {
        return frameanims.TryGetValue(name, out anim);
    }

    public void AddPreload(IEnumerable<string> files)
    {
        preloadFiles.AddRange(files);
    }

    public bool TextureExists(string name)
    {
        return texturefiles.ContainsKey(name) || name == NullTextureName || name == WhiteTextureName;
    }

    public void AddTexture(string name,string filename)
    {
        using var stream = VFS.Open(filename);
        var dat = ImageLib.Generic.TextureFromStream(GLWindow.RenderContext, stream);
        textures.Add(name, dat);
        texturefiles.Add(name, filename);
        EstimatedTextureMemory += dat.EstimatedTextureMemory;
    }

    public void ClearTextures()
    {
        loadedResFiles = new List<string>();
        var keys = new string[textures.Count];
        textures.Keys.CopyTo(keys, 0);
        foreach (var k in keys)
        {
            if (textures[k] != null)
            {
                textures[k].Dispose();
                textures[k] = null;
            }
        }

        EstimatedTextureMemory = 0;
    }

    public void ClearMeshes()
    {
        loadedResFiles = new List<string>();
        var keys = new uint[meshes.Count];
        meshes.Keys.CopyTo(keys, 0);
        foreach (var k in keys)
        {
            if (meshes[k] != null)
            {
                meshes[k].Dispose();
                meshes[k] = null;
            }
            meshDatas[k] = null;
        }
    }

    public override ImageResource FindImage(string name)
    {
        images.TryGetValue(name, out var outimage);
        return outimage;
    }

    public override Texture FindTexture (string name)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(GameResourceManager));
        if (name == null) return null;
        if (name == NullTextureName)
            return NullTexture;
        if (name == WhiteTextureName)
            return WhiteTexture;
        if (name == GreyTextureName)
            return GreyTexture;
        Texture outtex;
        if (!textures.TryGetValue(name, out outtex))
            return null;
        if (outtex == null)
        {
            var file = texturefiles[name];
            FLLog.Debug("Resources", string.Format("Reloading {0} from {1}", name, file));
            LoadResourceFile(file);
            outtex = textures[name];
        }
        return outtex;
    }

    public override Material FindMaterial (uint materialId)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(GameResourceManager));
        Material m = null;
        materials.TryGetValue (materialId, out m);
        return m;
    }

    public override VMeshResource FindMesh (uint vMeshLibId)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(GameResourceManager));
        if (!meshes.TryGetValue(vMeshLibId, out var vms)){
            return null;
        }
        if (vms != null) return vms;
        if (meshDatas.TryGetValue(vMeshLibId, out var d) && d != null){
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

    public override VMeshData FindMeshData(uint vMeshLibId)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(GameResourceManager));
        if (!meshDatas.TryGetValue(vMeshLibId, out var vms)){
            return null;
        }
        if (vms != null) return vms;
        LoadResourceFile(meshFiles[vMeshLibId], MeshLoadMode.CPU);
        return meshDatas[vMeshLibId];
    }

    public void AddResources(Utf.IntermediateNode node, string id)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(GameResourceManager));
        MatFile mat;
        TxmFile txm;
        VmsFile vms;
        Utf.UtfLoader.LoadResourceNode(node, this, out mat, out txm, out vms);
        if (mat != null) AddMaterials(mat, id);
        if (txm != null)
        {
            AddTextures(txm, id);
            AddImages(txm, id);
        }
        if (vms != null) AddMeshes(vms, MeshLoadMode.All, id);
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
        if (isDisposed) throw new ObjectDisposedException(nameof(GameResourceManager));
        List<string> removeTex = new List<string>();
        foreach (var tex in textures)
        {
            if (texturefiles[tex.Key] == id)
            {
                texturefiles.Remove(tex.Key);
                tex.Value.Dispose();
                removeTex.Add(tex.Key);
            }
        }
        foreach (var key in removeTex)
        {
            textures.Remove(key);
            images.Remove(key);
        }
        List<uint> removeMats = new List<uint>();
        foreach (var mat in materials)
        {
            if (materialfiles[mat.Key] == id)
            {
                materialfiles.Remove(mat.Key);
                mat.Value.Loaded = false;
                removeMats.Add(mat.Key);
            }
        }
        foreach (var key in removeMats) materials.Remove(key);

        var removeMeshes = meshes.Where(x => meshFiles[x.Key] == id).ToArray();
        var removeMeshDatas = meshDatas.Where(x => meshFiles[x.Key] == id).ToArray();

        foreach (var m in removeMeshes)
        {
            m.Value.Dispose();
            meshFiles.Remove(m.Key);
            meshes.Remove(m.Key);
        }
        foreach (var m in removeMeshDatas)
        {
            meshFiles.Remove(m.Key);
            meshDatas.Remove(m.Key);
        }

    }

    public override Fx.ParticleLibrary GetParticleLibrary(string filename)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(GameResourceManager));
        Fx.ParticleLibrary lib;
        if (!particlelibs.TryGetValue(filename, out lib))
        {
            var ale = new Utf.Ale.AleFile(filename, VFS.Open(filename));
            lib = new Fx.ParticleLibrary(this, ale);
            particlelibs.Add(filename, lib);
        }
        return lib;
    }

    public override void LoadResourceFile(string filename, MeshLoadMode meshMode = MeshLoadMode.GPU)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(GameResourceManager));
        var fn = filename.ToLowerInvariant();
        if (loadedResFiles.Contains(fn)) return;
        using var stream = VFS.Open(filename);
        MatFile mat;
        TxmFile txm;
        VmsFile vms;
        Utf.UtfLoader.LoadResourceFile(stream, filename, this, out mat, out txm, out vms);
        if (mat != null) AddMaterials(mat, filename);
        if (txm != null) AddTextures(txm, filename);
        if (vms != null) AddMeshes(vms, meshMode, filename);
        if (vms == null && mat == null && txm == null)
            FLLog.Warning("Resources", $"Could not load resources from file '{filename}'");
        loadedResFiles.Add(fn);
    }

    void AddImages(TxmFile t, string filename)
    {
        foreach (var tex in t.Textures)
        {
            if (!images.TryGetValue(tex.Key, out var existing) || existing == null)
            {
                var img = tex.Value.GetImageResource();
                if(img != null)
                    images[tex.Key] = img;
            }
        }
    }

    void AddTextures(TxmFile t, string filename)
    {
        foreach (var tex in t.Textures) {
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

    void AddMaterials(MatFile m, string filename)
    {
        if (m.TextureLibrary != null) {
            AddTextures(m.TextureLibrary, filename);
        }
        foreach (var kv in m.Materials) {
            if (!materials.ContainsKey(kv.Key))
            {
                kv.Value.Initialize(this);
                materials.Add(kv.Key, kv.Value);
                materialfiles[kv.Key] = filename;
            }
        }
    }
    void AddMeshes(VmsFile vms, MeshLoadMode mode, string filename)
    {
        bool isGpu = (mode & MeshLoadMode.GPU) == MeshLoadMode.GPU;
        bool isCpu = (mode & MeshLoadMode.CPU) == MeshLoadMode.CPU;
        foreach (var kv in vms.Meshes) {
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
    public override ModelResource GetDrawable(string filename, MeshLoadMode loadMode = MeshLoadMode.GPU)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(GameResourceManager));
        ModelResource res;
        if (!drawables.TryGetValue(filename, out res))
        {
            using var stream = VFS.Open(filename);
            var drawable = Utf.UtfLoader.LoadDrawable(stream,filename, this);
            CollisionMeshHandle handle = default;
            if(drawable == null) {
                drawables.Add(filename, null);
                return null;
            }
            if (drawable is not DfmFile && (loadMode & MeshLoadMode.NoCollision) != MeshLoadMode.NoCollision)
            {
                var surpath = Path.ChangeExtension(filename, "sur");
                if (VFS.FileExists(surpath))
                {
                    handle = new CollisionMeshHandle()
                        { Sur = GetSur(surpath), FileId = ConvexCollection.UseFile(surpath) };
                }
            }
            if (drawable is CmpFile) /* Get Resources */
            {
                var cmp = (CmpFile)drawable;
                if (cmp.MaterialLibrary != null) AddMaterials(cmp.MaterialLibrary, filename);
                if (cmp.TextureLibrary != null) AddTextures(cmp.TextureLibrary, filename);
                if (cmp.VMeshLibrary != null) AddMeshes(cmp.VMeshLibrary, loadMode, filename);
                foreach (var mdl in cmp.Models.Values) {
                    if (mdl.MaterialLibrary != null) AddMaterials(mdl.MaterialLibrary, filename);
                    if (mdl.TextureLibrary != null) AddTextures(mdl.TextureLibrary, filename);
                    if (mdl.VMeshLibrary != null) AddMeshes(mdl.VMeshLibrary, loadMode, filename);
                }
            }
            if (drawable is ModelFile)
            {
                var mdl = (ModelFile)drawable;
                if (mdl.MaterialLibrary != null) AddMaterials(mdl.MaterialLibrary, filename);
                if (mdl.TextureLibrary != null) AddTextures(mdl.TextureLibrary, filename);
                if (mdl.VMeshLibrary != null) AddMeshes(mdl.VMeshLibrary, loadMode, filename);
            }
            if (drawable is DfmFile)
            {
                var dfm = (DfmFile)drawable;
                dfm.Initialize(this, GLWindow.RenderContext);
                if (dfm.MaterialLibrary != null) AddMaterials(dfm.MaterialLibrary, filename);
                if (dfm.TextureLibrary != null) AddTextures(dfm.TextureLibrary, filename);
            }
            if (drawable is SphFile)
            {
                var sph = (SphFile)drawable;
                if (sph.MaterialLibrary != null) AddMaterials(sph.MaterialLibrary, filename);
                if (sph.TextureLibrary != null) AddTextures(sph.TextureLibrary, filename);
                if (sph.VMeshLibrary != null) AddMeshes(sph.VMeshLibrary, loadMode, filename);
            }
            res = new ModelResource(drawable, handle);
            drawable.ClearResources();
            drawables.Add(filename, res);
        }
        else
        {
            res.Drawable.ClearResources();
            if (!res.Collision.Valid &&
                (loadMode & MeshLoadMode.NoCollision) != MeshLoadMode.NoCollision &&
                res.Drawable is not DfmFile)
            {
                var surpath = Path.ChangeExtension(filename, "sur");
                if (VFS.FileExists(surpath))
                {
                    res.Collision = new CollisionMeshHandle()
                        { Sur = GetSur(surpath), FileId = ConvexCollection.UseFile(surpath) };
                }
            }
        }
        return res;
    }

    private bool isDisposed = false;

    public void Dispose()
    {
        if (isDisposed) return;
        isDisposed = true;
        //Textures
        foreach (var v in textures.Values) {
            if(v != null)
                v.Dispose();
        }
        NullTexture.Dispose();
        WhiteTexture.Dispose();
        GreyTexture.Dispose();
        //Vertex buffers
        vertexResourceAllocator.Dispose();
        ConvexCollection.Dispose();
    }
}
