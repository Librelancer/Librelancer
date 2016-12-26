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
using System.IO;
using Jitter.LinearMath;
using Jitter.Collision.Shapes;
namespace LibreLancer.Sur
{
	//TODO: Sur reader is VERY incomplete & undocumented
	public class SurFile
	{
		const string VERS_TAG = "vers";
		Dictionary<uint, Surface> surfaces = new Dictionary<uint, Surface>();
		Dictionary<uint, ConvexHullShape[]> shapes = new Dictionary<uint, ConvexHullShape[]>();
		//I'm assuming this gives me some sort of workable mesh
		public ConvexHullShape[] GetShape(uint meshId)
		{
			if (!shapes.ContainsKey(meshId))
			{
				List<ConvexHullShape> hull = new List<ConvexHullShape>();
				var surface = surfaces[meshId];
				for (int i = 0; i < surface.Groups.Length; i++)
				{
					var th = surface.Groups[i];
					if (th.MeshID != meshId)
						continue;
					var verts = new List<JVector>();
					if (th.VertexArrayOffset != 0)
						throw new Exception("tgroupheader vertexarrayoffset wrong");
					List<ushort> vertsAdded = new List<ushort>();
					foreach (var tri in th.Triangles)
					{
						if (!vertsAdded.Contains(tri.Vertices[0].Vertex)) {
							vertsAdded.Add(tri.Vertices[0].Vertex);
							verts.Add(surface.Vertices[tri.Vertices[0].Vertex].Point);
						}
						if (!vertsAdded.Contains(tri.Vertices[1].Vertex)) {
							vertsAdded.Add(tri.Vertices[1].Vertex);
							verts.Add(surface.Vertices[tri.Vertices[1].Vertex].Point);
						}
						if (!vertsAdded.Contains(tri.Vertices[0].Vertex))
						{
							vertsAdded.Add(tri.Vertices[2].Vertex);
							verts.Add(surface.Vertices[tri.Vertices[2].Vertex].Point);
						}
					}
					hull.Add(new ConvexHullShape(verts));
				}
				shapes.Add(meshId, hull.ToArray());
			}
			return shapes[meshId];
		}
		public bool HasShape(uint meshId)
		{
			return surfaces.ContainsKey(meshId);
		}
		public SurFile (Stream stream)
		{
			using (var reader = new BinaryReader (stream)) {
				if (reader.ReadTag () != VERS_TAG)
					throw new Exception ("Not a sur file");
				if (reader.ReadSingle() != 2.0)
				{
					throw new Exception("Incorrect sur version");
				}
				while (stream.Position < stream.Length) {
					uint meshid = reader.ReadUInt32 ();
					uint tagcount = reader.ReadUInt32 ();
					while (tagcount-- > 0) {
						var tag = reader.ReadTag ();
						if (tag == "surf") {
							uint size = reader.ReadUInt32 (); //TODO: SUR - What is this?
							var surf = new Surface(reader);
							surfaces.Add(meshid, surf);
						} else if (tag == "exts") {
							//TODO: SUR - What are exts used for?
							/*var min = new JVector (
								          reader.ReadSingle (),
								          reader.ReadSingle (),
								          reader.ReadSingle ()
							          );
							var max = new JVector (
								          reader.ReadSingle (),
								          reader.ReadSingle (),
								          reader.ReadSingle ()
							          );*/
							reader.BaseStream.Seek(6 * sizeof(float), SeekOrigin.Current);
						} else if (tag == "!fxd") {
							//TODO: SUR - WTF is this?!
						} else if (tag == "hpid") {
							//TODO: SUR - hpid. What does this do?
							uint count2 = reader.ReadUInt32 ();
							while (count2-- > 0) {
								//uint mesh2 = reader.ReadUInt32 ();
								reader.BaseStream.Seek(sizeof(uint), SeekOrigin.Current);
							}
						}
					}
				}
			}
		}


	}
}

