// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Generic;
using LibreLancer.Utf.Vms;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Dfm;
using LibreLancer.Vertices;
using LibreLancer.Primitives;
using LibreLancer.Fx;
using LibreLancer.Render;
using LibreLancer.Sur;

namespace LibreLancer
{
    //TODO: Allow for disposing and all that Jazz
    public abstract class ResourceManager : ILibFile
    {
        Dictionary<string, SurFile> surs = new Dictionary<string, SurFile>(StringComparer.OrdinalIgnoreCase);

        public abstract void AllocateVertices<T>(T[] vertices, ushort[] indices, out int startIndex, out int baseVertex, out VertexBuffer vbo, out IndexResourceHandle index) where T : struct;
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
        public abstract Material FindMaterial(uint materialId);
        public abstract VMeshData FindMesh(uint vMeshLibId);
        public abstract IDrawable GetDrawable(string filename);
        public abstract void LoadResourceFile(string filename);
        public abstract Fx.ParticleLibrary GetParticleLibrary(string filename);

        public abstract bool TryGetShape(string name, out TextureShape shape);
        public abstract bool TryGetFrameAnimation(string name, out TexFrameAnimation anim);

        public SurFile GetSur(string filename)
        {
            SurFile sur;
            if (!surs.TryGetValue(filename, out sur))
            {
                using (var stream = File.OpenRead(filename))
                {
                    sur = SurFile.Read(stream);
                }
                surs.Add(filename, sur);
            }
            return sur;
        }
    }

    public class ServerResourceManager : ResourceManager
    {
        Dictionary<string, IDrawable> drawables = new Dictionary<string, IDrawable>(StringComparer.OrdinalIgnoreCase);
        public override Dictionary<string, Texture> TextureDictionary => throw new InvalidOperationException();
        public override Dictionary<uint, Material> MaterialDictionary => throw new InvalidOperationException();
        public override Dictionary<string, TexFrameAnimation> AnimationDictionary => throw new InvalidOperationException();

        public override void AllocateVertices<T>(T[] vertices, ushort[] indices, out int startIndex, out int baseVertex, out VertexBuffer vbo, out IndexResourceHandle index)
        {
            throw new InvalidOperationException();
        }

        public override OpenCylinder GetOpenCylinder(int slices) => throw new InvalidOperationException();
        public override ParticleLibrary GetParticleLibrary(string filename) => throw new InvalidOperationException();
        public override QuadSphere GetQuadSphere(int slices) => throw new InvalidOperationException();
        public override Material FindMaterial(uint materialId) => throw new InvalidOperationException();
        public override VMeshData FindMesh(uint vMeshLibId) => throw new InvalidOperationException();
        public override Texture FindTexture(string name) => throw new InvalidOperationException();

        public override bool TryGetShape(string name, out TextureShape shape) => throw new InvalidOperationException();
        public override bool TryGetFrameAnimation(string name, out TexFrameAnimation anim) => throw new InvalidOperationException();

        public override IDrawable GetDrawable(string filename)
        {
            IDrawable drawable;
            if (!drawables.TryGetValue(filename, out drawable))
            {
                drawable = Utf.UtfLoader.LoadDrawable(filename, this);
                drawable?.ClearResources();
                drawables.Add(filename, drawable);
            }
            return drawable;
        }

        public override void LoadResourceFile(string filename) { }
    }
    public class GameResourceManager : ResourceManager
	{
		public Game Game;

		Dictionary<uint, VMeshData> meshes = new Dictionary<uint, VMeshData>();
		Dictionary<uint, Material> materials = new Dictionary<uint, Material>();
		Dictionary<uint, string> materialfiles = new Dictionary<uint, string>();
		Dictionary<string, Texture> textures = new Dictionary<string, Texture>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> texturefiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, IDrawable> drawables = new Dictionary<string, IDrawable>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, TextureShape> shapes = new Dictionary<string, TextureShape>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, Cursor> cursors = new Dictionary<string, Cursor>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, TexFrameAnimation> frameanims = new Dictionary<string, TexFrameAnimation>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, Fx.ParticleLibrary> particlelibs = new Dictionary<string, Fx.ParticleLibrary>(StringComparer.OrdinalIgnoreCase);

		List<string> loadedResFiles = new List<string>();
		List<string> preloadFiles = new List<string>();

        Dictionary<int, QuadSphere> quadSpheres = new Dictionary<int, QuadSphere>();
        Dictionary<int, OpenCylinder> cylinders = new Dictionary<int, OpenCylinder>();

        VertexResource<VertexPosition> posResource = new VertexResource<VertexPosition>();
        VertexResource<VertexPositionColor> posColorResource = new VertexResource<VertexPositionColor>();
        VertexResource<VertexPositionNormal> posNormalResource = new VertexResource<VertexPositionNormal>();
        VertexResource<VertexPositionColorTexture> posColorTextureResource = new VertexResource<VertexPositionColorTexture>();
        VertexResource<VertexPositionNormalTexture> posNormalTextureResource = new VertexResource<VertexPositionNormalTexture>();
        VertexResource<VertexPositionNormalDiffuseTexture> posNormalColorTextureResource = new VertexResource<VertexPositionNormalDiffuseTexture>();
        VertexResource<VertexPositionNormalTextureTwo> posNormalTextureTwoResource = new VertexResource<VertexPositionNormalTextureTwo>();
        VertexResource<VertexPositionNormalDiffuseTextureTwo> posNormalDiffuseTextureTwoResource = new VertexResource<VertexPositionNormalDiffuseTextureTwo>();

        T[] As<T>(object input) => (T[])input;

        public override void AllocateVertices<T>(T[] vertices, ushort[] indices, out int startIndex, out int baseVertex, out VertexBuffer vbo, out IndexResourceHandle index)
        {
            if (!Game.IsUiThread()) throw new InvalidOperationException();
            vbo = null;
            index = null;
            startIndex = baseVertex = -1;
            if(typeof(T) == typeof(VertexPosition)) {
                posResource.Allocate(As<VertexPosition>(vertices), indices, out vbo, out startIndex, out baseVertex, out index);
            } else if (typeof(T) == typeof(VertexPositionColor)) {
                posColorResource.Allocate(As<VertexPositionColor>(vertices), indices, out vbo, out startIndex, out baseVertex, out index);
            } else if (typeof(T) == typeof(VertexPositionNormal)) {
                posNormalResource.Allocate(As<VertexPositionNormal>(vertices), indices, out vbo, out startIndex, out baseVertex, out index);
            } else if (typeof(T) == typeof(VertexPositionColorTexture)) {
                posColorTextureResource.Allocate(As<VertexPositionColorTexture>(vertices), indices, out vbo, out startIndex, out baseVertex, out index);
            } else if (typeof(T) == typeof(VertexPositionNormalTexture)) {
                posNormalTextureResource.Allocate(As<VertexPositionNormalTexture>(vertices), indices, out vbo, out startIndex, out baseVertex, out index);
            } else if (typeof(T) == typeof(VertexPositionNormalDiffuseTexture)) {
                posNormalColorTextureResource.Allocate(As<VertexPositionNormalDiffuseTexture>(vertices), indices, out vbo, out startIndex, out baseVertex, out index);
            } else if (typeof(T) == typeof(VertexPositionNormalTextureTwo)) {
                posNormalTextureTwoResource.Allocate(As<VertexPositionNormalTextureTwo>(vertices), indices, out vbo, out startIndex, out baseVertex, out index);
            } else if (typeof(T) == typeof(VertexPositionNormalDiffuseTextureTwo)) {
                posNormalDiffuseTextureTwoResource.Allocate(As<VertexPositionNormalDiffuseTextureTwo>(vertices), indices, out vbo, out startIndex, out baseVertex, out index);
            } else {
                throw new NotSupportedException("Allocate " + typeof(T).Name);
            }
        }

        public override QuadSphere GetQuadSphere(int slices) {
            QuadSphere sph;
            if(!quadSpheres.TryGetValue(slices, out sph)) {
                sph = new QuadSphere(slices);
                quadSpheres.Add(slices, sph);
            }
            return sph;
        }

        public override OpenCylinder GetOpenCylinder(int slices)
        {
            OpenCylinder cyl;
            if (!cylinders.TryGetValue(slices, out cyl))
            {
                cyl = new OpenCylinder(slices);
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

        public GameResourceManager(Game g) : this()
		{
			Game = g;
			DefaultMaterial = new Material(this);
			DefaultMaterial.Name = "$LL_DefaultMaterialName";
		}

		
		public GameResourceManager()
		{
			NullTexture = new Texture2D(1, 1, false, SurfaceFormat.Color);
			NullTexture.SetData(new byte[] { 0xFF, 0xFF, 0xFF, 0x0 });

			WhiteTexture = new Texture2D(1, 1, false, SurfaceFormat.Color);
			WhiteTexture.SetData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
            
            GreyTexture = new Texture2D(1,1, false, SurfaceFormat.Color);
            GreyTexture.SetData(new byte[] { 128, 128, 128, 0xFF});
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
			var dat = ImageLib.Generic.FromFile(filename);
			textures.Add(name, dat);
			texturefiles.Add(name, filename);
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
		}

		public override Texture FindTexture (string name)
        {
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
			Material m = null;
			materials.TryGetValue (materialId, out m);
			return m;
		}

     

        public override VMeshData FindMesh (uint vMeshLibId)
		{
            VMeshData vms;
            meshes.TryGetValue(vMeshLibId, out vms);
            if (vms == null) FLLog.Warning("ResourceManager", "Mesh " + vMeshLibId + " not found");
            return vms;
		}

		public void AddResources(Utf.IntermediateNode node, string id)
		{
			MatFile mat;
			TxmFile txm;
			VmsFile vms;
			Utf.UtfLoader.LoadResourceNode(node, this, out mat, out txm, out vms);
			if (mat != null) AddMaterials(mat, id);
			if (txm != null) AddTextures(txm, id);
			if (vms != null) AddMeshes(vms);
		}

		public void RemoveResourcesForId(string id)
		{
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
			foreach (var key in removeTex) textures.Remove(key);
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
		}

        public override Fx.ParticleLibrary GetParticleLibrary(string filename)
        {
            Fx.ParticleLibrary lib;
            if (!particlelibs.TryGetValue(filename, out lib))
            {
                var ale = new Utf.Ale.AleFile(filename);
                lib = new Fx.ParticleLibrary(this, ale);
                particlelibs.Add(filename, lib);
            }
            return lib;
        }


        public override void LoadResourceFile(string filename)
		{
            var fn = filename.ToLowerInvariant();
            if (!loadedResFiles.Contains(fn))
            {
                MatFile mat;
                TxmFile txm;
                VmsFile vms;
                Utf.UtfLoader.LoadResourceFile(filename, this, out mat, out txm, out vms);
                if (mat != null) AddMaterials(mat, filename);
                if (txm != null) AddTextures(txm, filename);
                if (vms != null) AddMeshes(vms);
                if (vms == null && mat == null && txm == null) throw new Exception("Not a resource file " + filename);
                loadedResFiles.Add(fn);
            }
		}

		void AddTextures(TxmFile t, string filename)
		{
			foreach (var tex in t.Textures) {
				if (!textures.ContainsKey(tex.Key))
				{
					var v = tex.Value;
					v.Initialize();
                    if (v.Texture != null)
                    {
                        textures.Add(tex.Key, v.Texture);
                        texturefiles.Add(tex.Key, filename);
                    }
				}
				else if (textures[tex.Key] == null)
				{
					var v = tex.Value;
					v.Initialize();
					textures[tex.Key] = v.Texture;
				}
			}
			foreach (var anim in t.Animations)
			{
				if (!frameanims.ContainsKey(anim.Key))
				{
					frameanims.Add(anim.Key, anim.Value);
				}
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
					materials.Add(kv.Key, kv.Value);
					materialfiles.Add(kv.Key, filename);
				}
			}
		}
		void AddMeshes(VmsFile vms)
		{
			foreach (var kv in vms.Meshes) {
				if (!meshes.ContainsKey (kv.Key))
					meshes.Add (kv.Key, kv.Value);
			}
		}
		public override IDrawable GetDrawable(string filename)
		{
			IDrawable drawable;
			if (!drawables.TryGetValue(filename, out drawable))
			{
				drawable = Utf.UtfLoader.LoadDrawable(filename, this);
                if(drawable == null) {
                    drawables.Add(filename, drawable);
                    return null;
                }
				drawable.Initialize(this);
				if (drawable is CmpFile) /* Get Resources */
				{
					var cmp = (CmpFile)drawable;
					if (cmp.MaterialLibrary != null) AddMaterials(cmp.MaterialLibrary, filename);
					if (cmp.TextureLibrary != null) AddTextures(cmp.TextureLibrary, filename);
					if (cmp.VMeshLibrary != null) AddMeshes(cmp.VMeshLibrary);
				}
				if (drawable is ModelFile)
				{
					var mdl = (ModelFile)drawable;
					if (mdl.MaterialLibrary != null) AddMaterials(mdl.MaterialLibrary, filename);
					if (mdl.TextureLibrary != null) AddTextures(mdl.TextureLibrary, filename);
					if (mdl.VMeshLibrary != null) AddMeshes(mdl.VMeshLibrary);
                }
				if (drawable is DfmFile)
				{
					var dfm = (DfmFile)drawable;
					if (dfm.MaterialLibrary != null) AddMaterials(dfm.MaterialLibrary, filename);
					if (dfm.TextureLibrary != null) AddTextures(dfm.TextureLibrary, filename);
                }
				if (drawable is SphFile)
				{
					var sph = (SphFile)drawable;
					if (sph.MaterialLibrary != null) AddMaterials(sph.MaterialLibrary, filename);
					if (sph.TextureLibrary != null) AddTextures(sph.TextureLibrary, filename);
					if (sph.VMeshLibrary != null) AddMeshes(sph.VMeshLibrary);
                }
                drawable.ClearResources();
				drawables.Add(filename, drawable);
			}
			return drawable;
		}
	}
}