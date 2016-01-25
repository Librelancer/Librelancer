using System;
using System.Collections.Generic;
using LibreLancer.Utf.Vms;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Cmp;
namespace LibreLancer
{
	//TODO: Allow for disposing and all that Jazz
	public class ResourceManager : ILibFile
	{
		public FreelancerGame Game;

		Dictionary<string,VertexBuffer> meshes = new Dictionary<string, VertexBuffer>();
		Dictionary<uint, Material> materials = new Dictionary<uint, Material>();
		Dictionary<string, TextureData> textures = new Dictionary<string, TextureData>();

		Dictionary<string, CmpFile> cmps = new Dictionary<string, CmpFile>();
		Dictionary<string, ModelFile> models = new Dictionary<string, ModelFile>();
		Dictionary<string, SphFile> sphs = new Dictionary<string, SphFile>();

		List<string> loadedMatFiles = new List<string>();
		List<string> loadedTxmFiles = new List<string>();

		public ResourceManager(FreelancerGame g)
		{
			Game = g;
		}

		public TextureData FindTexture (string name)
		{
			return textures [name.ToLower()];
		}

		public Material FindMaterial (uint materialId)
		{
			return materials [materialId];
		}

		public VMeshData FindMesh (uint vMeshLibId)
		{
			throw new NotImplementedException ();
		}

		public void LoadMat(string filename)
		{
			if (loadedMatFiles.Contains (filename))
				return;
			var m = new MatFile (filename, this);
			AddMaterials (m);
			loadedMatFiles.Add (filename);
		}
			
		public void LoadTxm(string filename)
		{
			if (loadedTxmFiles.Contains (filename))
				return;
			var t = new TxmFile (filename);
			AddTextures (t);
			loadedTxmFiles.Add (filename);
		}

		void AddTextures(TxmFile t)
		{
			foreach (var tex in t.Textures) {
				if (!textures.ContainsKey (tex.Key.ToLower())) {
					tex.Value.Initialize ();
					textures.Add (tex.Key.ToLower(), tex.Value);
				}
			}
		}
		void AddMaterials(MatFile m)
		{
			if (m.TextureLibrary != null) {
				AddTextures (m.TextureLibrary);
			}
			foreach (var kv in m.Materials) {
				if(!materials.ContainsKey(kv.Key))
					materials.Add (kv.Key, kv.Value);
			}
		}

		public IDrawable GetDrawable(string filename)
		{
			if(filename.EndsWith(".cmp"))
				return GetCmp(filename);
			if (filename.EndsWith (".3db"))
				return GetModel (filename);
			if (filename.EndsWith (".sph"))
				return GetSph (filename);
			throw new NotSupportedException (filename);
		}

		public SphFile GetSph(string filename)
		{
			if (!sphs.ContainsKey (filename)) {
				var file = new SphFile (filename, this);
				sphs.Add (filename, file);
			}
			return sphs [filename];
		}

		public CmpFile GetCmp(string filename)
		{
			if (!cmps.ContainsKey (filename)) {
				var file = new CmpFile (filename, this);
				if (file.TextureLibrary != null) {
					AddTextures (file.TextureLibrary);
				}
				if (file.MaterialLibrary != null) {
					AddMaterials (file.MaterialLibrary);
				}
				file.Initialize (this);
				cmps.Add (filename, file);
			}
			return cmps [filename];
		}

		public ModelFile GetModel(string filename)
		{
			if(!models.ContainsKey(filename)) {
				var file = new ModelFile (filename, this);
				if (file.TextureLibrary != null) {
					AddTextures (file.TextureLibrary);
				}
				if (file.MaterialLibrary != null) {
					AddMaterials (file.MaterialLibrary);
				}
				file.Initialize (this);
				models.Add (filename, file);
			}
			return models [filename];
		}

	}
}

