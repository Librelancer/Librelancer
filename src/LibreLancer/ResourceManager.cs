/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.IO;
using System.Collections.Generic;
using LibreLancer.Utf.Vms;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Dfm;
using LibreLancer.Sur;
namespace LibreLancer
{
	//TODO: Allow for disposing and all that Jazz
	public class ResourceManager : ILibFile
	{
		public FreelancerGame Game;

		Dictionary<uint, VMeshData> meshes = new Dictionary<uint, VMeshData>();
		Dictionary<uint, Material> materials = new Dictionary<uint, Material>();
		Dictionary<string, Texture> textures = new Dictionary<string, Texture>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> texturefiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, CmpFile> cmps = new Dictionary<string, CmpFile>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, DfmFile> dfms = new Dictionary<string, DfmFile>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, ModelFile> models = new Dictionary<string, ModelFile>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, SphFile> sphs = new Dictionary<string, SphFile>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, VmsFile> vmss = new Dictionary<string, VmsFile>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, TextureShape> shapes = new Dictionary<string, TextureShape>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, SurFile> surs = new Dictionary<string, SurFile>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, Cursor> cursors = new Dictionary<string, Cursor>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, TexFrameAnimation> frameanims = new Dictionary<string, TexFrameAnimation>(StringComparer.OrdinalIgnoreCase);
		List<string> loadedMatFiles = new List<string>();
		List<string> loadedTxmFiles = new List<string>();
		List<string> preloadFiles = new List<string>();

		public ResourceManager(FreelancerGame g) : this()
		{
			Game = g;
		}

		public Texture2D NullTexture;
		public Texture2D WhiteTexture;
		public const string NullTextureName = "$$LIBRELANCER.Null";
		public const string WhiteTextureName = "$$LIBRELANCER.White";
		public ResourceManager()
		{
			NullTexture = new Texture2D(1, 1, false, SurfaceFormat.Color);
			NullTexture.SetData(new byte[] { 0xFF, 0xFF, 0xFF, 0x0 });

			WhiteTexture = new Texture2D(1, 1, false, SurfaceFormat.Color);
			WhiteTexture.SetData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
		}

		public void Preload()
		{
			foreach (var file in preloadFiles)
			{
				if (file.ToLowerInvariant().EndsWith(".mat"))
					LoadMat(file);
				if (file.ToLowerInvariant().EndsWith(".txm"))
					LoadTxm(file);
			}
			preloadFiles = null;
		}

		public SurFile GetSur(string filename)
		{
			SurFile sur;
			if (!surs.TryGetValue(filename, out sur))
			{
				using (var stream = File.OpenRead(filename))
				{
					sur = new SurFile(stream);
				}
				surs.Add(filename, sur);
			}
			return sur;
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
		public bool TryGetShape(string name, out TextureShape shape)
		{
			return shapes.TryGetValue(name, out shape);
		}

		public bool TryGetFrameAnimation(string name, out TexFrameAnimation anim)
		{
			return frameanims.TryGetValue(name, out anim);
		}

		public void AddPreload(IEnumerable<string> files)
		{
			preloadFiles.AddRange(files);
		}

		public bool TextureExists(string name)
		{
			return texturefiles.ContainsKey(name) || name == NullTextureName;
		}

		public void AddTexture(string name,string filename)
		{
			var dat = ImageLib.Generic.FromFile(filename);
			textures.Add(name, dat);
			texturefiles.Add(name, filename);
		}

		public void ClearTextures()
		{
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

		public Texture FindTexture (string name)
		{
			if (name == NullTextureName)
				return NullTexture;
			if (name == WhiteTextureName)
				return WhiteTexture;
            Texture outtex;
			if ((outtex = textures[name]) == null)
			{
				var file = texturefiles[name];
				FLLog.Debug("Resources", string.Format("Reloading {0} from {1}", name, file));
				if (file.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
				{
					loadedMatFiles.Remove(file);
					LoadMat(file);
				}
				else if (file.EndsWith(".cmp", StringComparison.OrdinalIgnoreCase))
				{
					var c = new CmpFile(file, this);
					if (c.MaterialLibrary != null)
						AddMaterials(c.MaterialLibrary, file);
					AddTextures(c.TextureLibrary, file);
				}
				else if (file.EndsWith(".3db", StringComparison.OrdinalIgnoreCase))
				{
					var m = new ModelFile(file, this);
					if (m.MaterialLibrary != null)
						AddMaterials(m.MaterialLibrary, file);
					AddTextures(m.TextureLibrary, file);
				}
				else if (file.EndsWith(".txm", StringComparison.OrdinalIgnoreCase))
				{
					loadedTxmFiles.Remove(file);
					LoadTxm(file);
				}
				else
				{
					textures[name] = ImageLib.Generic.FromFile(file);
				}
                outtex = textures[name];
			}
            return outtex;
		}

		public Material FindMaterial (uint materialId)
		{
			Material m = null;
			materials.TryGetValue (materialId, out m);
			return m;
		}

		public VMeshData FindMesh (uint vMeshLibId)
		{
			return meshes [vMeshLibId];
		}

		public void LoadMat(string filename)
		{
			if (loadedMatFiles.Contains (filename))
				return;
			var m = new MatFile (filename, this);
			AddMaterials(m, filename);
			loadedMatFiles.Add (filename);
		}

		public void LoadTxm(string filename)
		{
			if (loadedTxmFiles.Contains (filename))
				return;
			var t = new TxmFile (filename);
			AddTextures(t, filename);
			loadedTxmFiles.Add (filename);
		}

		void AddTextures(TxmFile t, string filename)
		{
			foreach (var tex in t.Textures) {
				if (!textures.ContainsKey(tex.Key))
				{
					var v = tex.Value;
					v.Initialize();
					textures.Add(tex.Key, v.Texture);
					texturefiles.Add(tex.Key, filename);
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
				if(!materials.ContainsKey(kv.Key))
					materials.Add (kv.Key, kv.Value);
			}
		}
		void AddMeshes(VmsFile vms)
		{
			foreach (var kv in vms.Meshes) {
				if (!meshes.ContainsKey (kv.Key))
					meshes.Add (kv.Key, kv.Value);
			}
		}
		public IDrawable GetDrawable(string filename)
		{
			if (filename.EndsWith(".cmp", StringComparison.OrdinalIgnoreCase))
				return GetCmp(filename);
			if (filename.EndsWith (".3db", StringComparison.OrdinalIgnoreCase))
				return GetModel (filename);
			if (filename.EndsWith (".sph", StringComparison.OrdinalIgnoreCase))
				return GetSph (filename);
			if (filename.EndsWith(".dfm", StringComparison.OrdinalIgnoreCase))
				return GetDfm(filename);
			return GetCmp (filename + ".cmp");
		}

		public DfmFile GetDfm(string filename)
		{
			if(!dfms.ContainsKey(filename)) {
				var file = new DfmFile(filename, this);
				if (file.TextureLibrary != null)
				{
					AddTextures(file.TextureLibrary, filename);
				}
				if (file.MaterialLibrary != null)
				{
					AddMaterials(file.MaterialLibrary, filename);
				}
				file.Initialize(this);
				dfms.Add(filename, file);
			}
			return dfms[filename];
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
					AddTextures(file.TextureLibrary, filename);
				}
				if (file.MaterialLibrary != null) {
					AddMaterials(file.MaterialLibrary, filename);
				}
				if (file.VMeshLibrary != null) {
					AddMeshes (file.VMeshLibrary);
				}
				file.Initialize (this);
				cmps.Add (filename, file);
			}
			return cmps [filename];
		}
		public void LoadVms(string filename)
		{
			if (!vmss.ContainsKey (filename)) {
				var file = new VmsFile (filename, this);
				AddMeshes (file);
			}
		}
		public ModelFile GetModel(string filename)
		{
			if(!models.ContainsKey(filename)) {
				var file = new ModelFile (filename, this);
				if (file.TextureLibrary != null) {
					AddTextures(file.TextureLibrary, filename);
				}
				if (file.MaterialLibrary != null) {
					AddMaterials(file.MaterialLibrary, filename);
				}
				if (file.VMeshLibrary != null) {
					AddMeshes (file.VMeshLibrary);
				}
				file.Initialize (this);
				models.Add (filename, file);
			}
			return models [filename];
		}

	}
}

