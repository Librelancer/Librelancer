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

		Dictionary<uint,VMeshData> meshes = new Dictionary<uint, VMeshData> ();
		Dictionary<uint, Material> materials = new Dictionary<uint, Material>();
		Dictionary<string, TextureData> textures = new Dictionary<string, TextureData>();

		Dictionary<string, CmpFile> cmps = new Dictionary<string, CmpFile>();
		Dictionary<string, ModelFile> models = new Dictionary<string, ModelFile>();
		Dictionary<string, SphFile> sphs = new Dictionary<string, SphFile>();
		Dictionary<string, VmsFile> vmss = new Dictionary<string, VmsFile>();

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
			return meshes [vMeshLibId];
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
		void AddMeshes(VmsFile vms)
		{
			foreach (var kv in vms.Meshes) {
				if (!meshes.ContainsKey (kv.Key))
					meshes.Add (kv.Key, kv.Value);
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
					AddTextures (file.TextureLibrary);
				}
				if (file.MaterialLibrary != null) {
					AddMaterials (file.MaterialLibrary);
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

