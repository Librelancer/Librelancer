using System;
using System.Collections.Generic;
namespace LibreLancer
{
	//TODO: Allow for disposing and all that Jazz
	public class ResourceCache
	{
		Dictionary<string,Texture> textures = new Dictionary<string, Texture>();
		Dictionary<string,VertexBuffer> meshes = new Dictionary<string, VertexBuffer>();

		public bool TryGetTexture(string name, out Texture value)
		{
			return textures.TryGetValue (name, out value);
		}

		public void AddTexture(string name, Texture texture)
		{
			textures.Add (name, texture);
		}

		public int TextureCount {
			get {
				return textures.Count;
			}
		}

		public bool TryGetMesh(string name, out VertexBuffer value)
		{
			return meshes.TryGetValue (name, out value);
		}

		public void AddMesh(string mesh, VertexBuffer buffer)
		{
			meshes.Add (mesh, buffer);
		}

		public int MeshCount {
			get {
				return meshes.Count;
			}
		}
	}
}

