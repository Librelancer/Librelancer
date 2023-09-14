// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using LibreLancer.Physics;

namespace LibreLancer.Sur
{
	//TODO: Sur reader is VERY incomplete & undocumented
	public class SurFile : IConvexMeshProvider
    {
        public List<SurfacePart> Surfaces = new List<SurfacePart>();

		Dictionary<uint, ConvexMesh[]> shapes = new Dictionary<uint, ConvexMesh[]>();

        public bool TryGetHardpoint(uint meshId, uint hpId, out ConvexMesh[] mesh)
        {
            mesh = null;
            List<ConvexMesh> hull = new List<ConvexMesh>();
            foreach (var surface in Surfaces)
            {
                if (surface.Crc != meshId) continue;
                if (!surface.HardpointIds.Contains(hpId))
                    continue;
                var hulls = surface.GetHulls(false);
                for (int i = 0; i < hulls.Length; i++)
                {
                    var th = hulls[i];
                    if (th.HullId != hpId)
                        continue;
                    var verts = new List<Vector3>();
                    foreach (var v in surface.Points)
                        verts.Add(v.Point);
                    var indices = new List<int>();
                    foreach (var tri in th.Faces)
                    {
                        indices.Add(tri.Points.A);
                        indices.Add(tri.Points.B);
                        indices.Add(tri.Points.C);
                    }
                    hull.Add(new ConvexMesh() { Indices = indices.ToArray(), Vertices = verts.ToArray() });
                }
            }
            if (hull.Count > 0) {
                mesh = hull.ToArray();
                return true;
            }
            return false;
        }

        public ConvexMesh[] GetMesh(uint meshId)
        {
            List<ConvexMesh> hull = new List<ConvexMesh>();
            foreach (var surface in Surfaces)
            {
                if (surface.Crc != meshId) continue;
                var hulls = surface.GetHulls(false);
                for (int i = 0; i < hulls.Length; i++)
                {
                    var triHull = hulls[i];
                    if (triHull.Type == 5 ||
                        surface.HardpointIds.Contains(triHull.HullId) ||
                        Surfaces.Any(x => x != surface && x.Crc == triHull.HullId))
                        continue;
                    var verts = new List<Vector3>();
                    foreach (var v in surface.Points)
                        verts.Add(v.Point);
                    var indices = new List<int>();
                    foreach (var tri in triHull.Faces)
                    {
                        indices.Add(tri.Points.A);
                        indices.Add(tri.Points.B);
                        indices.Add(tri.Points.C);
                    }
                    hull.Add(new ConvexMesh() { Indices = indices.ToArray(), Vertices = verts.ToArray() });
                }
            }
            return hull.ToArray();
        }

        public bool HasShape(uint meshId)
		{
			return Surfaces.Any(x => x.Crc == meshId);
		}

        private const uint SUR_MAGIC = 0x73726576; //"vers"
        private const uint SUR_VERSION = 0x40000000; //2.0f

        public static SurFile Read(Stream stream)
        {
            var sur = new SurFile();
            using (var reader = new BinaryReader (stream)) {
                if (reader.ReadUInt32()  != SUR_MAGIC)
                    throw new Exception ("Not a sur file");
                if (reader.ReadUInt32() != SUR_VERSION)
                    throw new Exception("Incorrect sur version");
                while (stream.Position < stream.Length) {
                    sur.Surfaces.Add(SurfacePart.Read(reader));
                }
            }
            return sur;
        }

        public void Save(Stream stream, bool leaveOpen = true)
        {
            using (var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen))
            {
                writer.Write(SUR_MAGIC);
                writer.Write(SUR_VERSION);
                foreach (var s in Surfaces)
                    s.Write(writer);
            }
        }
    }
}

