// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data;
using LibreLancer.Graphics;
using LibreLancer.Utf.Vms;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.World;

namespace LibreLancer.Utf.Mat
{
    /// <summary>
    /// Represents a UTF Sphere File (.sph)
    /// </summary>
    public class SphFile : UtfFile, IRigidModelFile
    {
        private ResourceManager library;

        public MatFile? MaterialLibrary;
        public TxmFile? TextureLibrary;
        public VmsFile? VMeshLibrary;
        public float Radius { get; private set; }

        private Material?[] sideMaterials = null!;

        public class SphMaterials
        {
            private SphFile sph;

            internal SphMaterials(SphFile sph)
            {
                this.sph = sph;
            }

            // TODO: Is there an attribute we can do to avoid casting to/from null?
            private void CheckNullArray()
            {
                if ((Material?[]?) sph.sideMaterials != null)
                {
                    return;
                }

                sph.sideMaterials = new Material[sph.SideMaterialNames.Count];

                for (int i = 0; i < sph.SideMaterialNames.Count; i++)
                {
                    sph.sideMaterials[i] = sph.library.FindMaterial(CrcTool.FLModelCrc(sph.SideMaterialNames[i]));
                }
            }

            public int Length
            {
                get
                {
                    CheckNullArray();
                    return sph.sideMaterials.Length;
                }
            }

            public Material? this[int i]
            {
                get
                {
                    CheckNullArray();

                    if (sph.sideMaterials[i] != null)
                    {
                        return sph.sideMaterials?[i];
                    }

                    var crc = CrcTool.FLModelCrc(sph.SideMaterialNames[i]);
                    sph.sideMaterials[i] = sph.library.FindMaterial(crc);

                    return sph.sideMaterials[i];
                }
                set
                {
                    CheckNullArray();
                    sph.sideMaterials?[i] = value;
                }
            }
        }

        public SphMaterials SideMaterials { get; }
        public List<string> SideMaterialNames { get; } = [];

        public SphFile(IntermediateNode root, ResourceManager library, string path = "/")
        {
            SideMaterials = new SphMaterials(this);

            this.library = library;

            var sphereSet = false;

            foreach (IntermediateNode node in root.Children.OfType<IntermediateNode>())
            {
                switch (node.Name.ToLowerInvariant())
                {
                    case "sphere":
                        if (sphereSet) throw new Exception("Multiple sphere nodes");
                        sphereSet = true;

                        foreach (var sphereSubNode in node.Children.OfType<LeafNode>())
                        {
                            var name = sphereSubNode.Name.ToLowerInvariant();

                            if (name.StartsWith("m", StringComparison.OrdinalIgnoreCase))
                            {
                                SideMaterialNames.Add(sphereSubNode.StringData);
                            }

                            else
                                switch (name)
                                {
                                    case "radius":
                                        Radius = sphereSubNode.SingleArrayData[0];
                                        break;
                                    case "sides":
                                    {
                                        int count = sphereSubNode.Int32ArrayData[0];

                                        if (count != SideMaterialNames.Count)
                                        {
                                            throw new Exception(
                                                "Invalid number of sides in " + node.Name + ": " + count);
                                        }

                                        break;
                                    }
                                    default:
                                        throw new Exception("Invalid node in " + node.Name + ": " + sphereSubNode.Name);
                                }
                        }

                        break;
                    case "vmeshlibrary":
                        VMeshLibrary = VMeshLibrary == null
                            ? new VmsFile(node)
                            : throw new Exception("Multiple vmeshlibrary nodes in 3db root");
                        break;
                    case "material library":
                        MaterialLibrary = MaterialLibrary == null
                            ? new MatFile(node)
                            : throw new Exception("Multiple material library nodes in 3db root");
                        break;
                    case "texture library":
                        TextureLibrary = TextureLibrary == null
                            ? new TxmFile(node)
                            : throw new Exception("Multiple texture library nodes in 3db root");
                        break;
                }
            }

            if (SideMaterialNames.Count < 6)
            {
                FLLog.Warning("Sph", $"Sph {path} does not contain all 6 sides and will not render");
            }
        }

        private static CubeMapFace[] faces =
        [
            CubeMapFace.PositiveZ,
            CubeMapFace.PositiveX,
            CubeMapFace.NegativeZ,
            CubeMapFace.NegativeX,
            CubeMapFace.PositiveY,
            CubeMapFace.NegativeY
        ];

        public RigidModel CreateRigidModel(bool drawable, ResourceManager resources)
        {
            var model = new RigidModel() { Source = RigidModelSource.Sphere };
            var dcs = new List<MeshDrawcall>();

            var vmesh = new VisualMesh
            {
                Radius = Radius,
                BoundingBox = BoundingBox.CreateFromSphere(new BoundingSphere(Vector3.Zero, Radius))
            };

            if (drawable && SideMaterials.Length >= 6)
            {
                var sphere = resources.GetQuadSphere(26);

                for (int i = 0; i < 6; i++)
                {
                    Vector3 pos;
                    sphere.GetDrawParameters(faces[i], out var start, out var count, out pos);
                    var dc = new MeshDrawcall
                    {
                        MaterialCrc = CrcTool.FLModelCrc(SideMaterialNames[i]),
                        BaseVertex = 0,
                        StartIndex = start,
                        PrimitiveCount = count
                    };
                    dcs.Add(dc);
                }

                if (SideMaterials.Length > 6)
                {
                    var crc = CrcTool.FLModelCrc(SideMaterialNames[6]);

                    for (int i = 0; i < 6; i++)
                    {
                        Vector3 pos;
                        sphere.GetDrawParameters(faces[i], out var start, out var count, out pos);
                        var dc = new MeshDrawcall
                        {
                            MaterialCrc = crc,
                            BaseVertex = 0,
                            StartIndex = start,
                            PrimitiveCount = count
                        };
                        dcs.Add(dc);
                    }
                }

                vmesh.Levels =
                [
                    new MeshLevel(dcs.ToArray(),
                        new VMeshResource { VertexResource = new VertexResource(sphere.VertexBuffer) }, default)
                    {
                        Scale = Radius,
                    }
                ];
            }

            var part = new RigidModelPart
            {
                Hardpoints = [],
                Mesh = vmesh
            };

            model.Root = part;
            model.AllParts = [part];
            return model;
        }

        public void ClearResources()
        {
            MaterialLibrary = null;
            TextureLibrary = null;
        }
    }
}
