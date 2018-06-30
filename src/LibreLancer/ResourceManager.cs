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
namespace LibreLancer
{
	//TODO: Allow for disposing and all that Jazz
	public class ResourceManager : ILibFile
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
		List<string> loadedResFiles = new List<string>();
		List<string> preloadFiles = new List<string>();

		public Dictionary<string, Texture> TextureDictionary
		{
			get
			{
				return textures;
			}
		}
		public Dictionary<uint, Material> MaterialDictionary
		{
			get
			{
				return materials;
			}
		}
		public ResourceManager(Game g) : this()
		{
			Game = g;
			DefaultMaterial = new Material(this);
			DefaultMaterial.Name = "$LL_DefaultMaterialName";
		}

		public Material DefaultMaterial;
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



		public void LoadResourceFile(string filename)
		{
			MatFile mat;
			TxmFile txm;
			VmsFile vms;
			Utf.UtfLoader.LoadResourceFile(filename, this, out mat, out txm, out vms);
			if (mat != null) AddMaterials(mat, filename);
			if (txm != null) AddTextures(txm, filename);
			if (vms != null) AddMeshes(vms);
			if (vms == null && mat == null && txm == null) throw new Exception("Not a resource file " + filename);
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
		public IDrawable GetDrawable(string filename)
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
				drawables.Add(filename, drawable);
			}
			return drawable;
		}
	}
}