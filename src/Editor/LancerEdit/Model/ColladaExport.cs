// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using CL = Collada141;
using LibreLancer;
using LibreLancer.Utf.Cmp;
using LibreLancer.Vertices;
using LibreLancer.Utf.Vms;

namespace LancerEdit
{
    public class ColladaExport
    {
        public static void ExportCollada(ModelFile mdl, ResourceManager resources, string output)
        {
            var dae = NewCollada();
            var mats = new CL.library_materials();
            var efx = new CL.library_effects();
            var geos = new CL.library_geometries();
            var scenes = new CL.library_visual_scenes();
            var vscene = new CL.visual_scene();
            vscene.name = vscene.id = "main-scene";
            scenes.visual_scene = new CL.visual_scene[] { vscene };
            dae.scene = new CL.COLLADAScene();
            dae.scene.instance_visual_scene = new CL.InstanceWithExtra()
            {
                url = "#main-scene"
            };
            var exported = ProcessModel(mdl, resources);
            geos.geometry = exported.Geometries.ToArray();
            mats.material = exported.Materials.Select((x) => new CL.material()
            {
                name = x.Name,
                id = x.Name + "-material",
                instance_effect = new CL.instance_effect() { url = "#" + x.Name + "-effect" }
            }).ToArray();
            efx.effect = exported.Materials.Select((x) => new CL.effect()
            {
                id = x.Name + "-effect",
                Items = new[]
                {
                    new CL.effectFx_profile_abstractProfile_COMMON()
                    {
                        technique = new CL.effectFx_profile_abstractProfile_COMMONTechnique()
                        {
                            id = "common",
                            Item = new CL.effectFx_profile_abstractProfile_COMMONTechniquePhong()
                            {
                                ambient = ColladaColor("ambient",Color4.Black),
                                emission = ColladaColor("emmision",Color4.Black),
                                diffuse = ColladaColor("diffuse",x.Dc),
                                specular = ColladaColor("specular",new Color4(0.25f,0.25f,0.25f,1f)),
                                shininess = ColladaFloat("shininess",50),
                                index_of_refraction = ColladaFloat("index_of_refraction",1)
                            }
                        }
                    }
                }
            }).ToArray();
            var nodes = new List<CL.node>();
            for (int i = 0; i < exported.Geometries.Count; i++) {
                nodes.Add(exported.GetNode(i, Matrix4.Identity, mdl.Path));
            }
            vscene.node = nodes.ToArray();
            dae.Items = new object[] { efx, mats, geos, scenes };
            using (var stream = File.Create(output))
                ColladaSupport.XML.Serialize(stream, dae);
        }
        class InputModel
        {
            public Matrix4 Transform;
            public string Con;
            public ModelFile Model;
            public List<InputModel> Children = new List<InputModel>();
            public ExportModel Export;
        }
        public static void ExportCollada(CmpFile cmp, ResourceManager resources, string output)
        {
            //Build tree
            InputModel rootModel = null;
            List<InputModel> parentModels = new List<InputModel>();
            foreach (var p in cmp.Parts)
            {
                if (p.Construct == null) {
                    rootModel = new InputModel()
                    {
                        Transform = Matrix4.Identity,
                        Model = p.Model,
                        Con = "Root"
                    };
                    break;
                }
            }
            parentModels.Add(rootModel);
            var q = new Queue<Part>(cmp.Parts);
            int infiniteDetect = 0;
            while(q.Count > 0) {
                var part = q.Dequeue();
                if(part.Construct == null) {
                    continue;
                }
                bool enqueue = true;
                foreach(var mdl in parentModels) {
                    if(part.Construct.ParentName == mdl.Con) {
                        var child = new InputModel()
                        {
                            Transform = part.Construct.Rotation * Matrix4.CreateTranslation(part.Construct.Origin),
                            Model = part.Model,
                            Con = part.Construct.ChildName
                        };
                        mdl.Children.Add(child);
                        parentModels.Add(child);
                        enqueue = false;
                        break;
                    }
                }
                if (enqueue)
                    q.Enqueue(part);
                infiniteDetect++;
                if(infiniteDetect > 200000000) throw new Exception("Infinite cmp loop detected");
            }
            
            //Build collada
            var dae = NewCollada();
            var efx = new CL.library_effects();
            var mats = new CL.library_materials();
            var geos = new CL.library_geometries();
            var scenes = new CL.library_visual_scenes();
            var vscene = new CL.visual_scene();
            vscene.name = vscene.id = "main-scene";
            scenes.visual_scene = new CL.visual_scene[] { vscene };
            dae.scene = new CL.COLLADAScene();
            dae.scene.instance_visual_scene = new CL.InstanceWithExtra()
            {
                url = "#main-scene"
            };
            var glist = new List<CL.geometry>();
            var mlist = new List<string>();
            var matlist = new List<ExportMaterial>();
            BuildModel(resources, rootModel, glist, mlist, matlist);
            geos.geometry = glist.ToArray();
            mats.material = mlist.Select((x) => new CL.material()
            {
                name = x,
                id = x + "-material",
                instance_effect = new CL.instance_effect() {
                    url = "#" + x + "-effect"
                }
            }).ToArray();
            efx.effect = matlist.Select((x) => new CL.effect()
            {
                id = x.Name + "-effect",
                Items = new[]
                {
                    new CL.effectFx_profile_abstractProfile_COMMON()
                    {
                        technique = new CL.effectFx_profile_abstractProfile_COMMONTechnique()
                        {
                            id = "common",
                            Item = new CL.effectFx_profile_abstractProfile_COMMONTechniquePhong()
                            {
                                ambient = ColladaColor("ambient",Color4.Black),
                                emission = ColladaColor("emmision",Color4.Black),
                                diffuse = ColladaColor("diffuse",x.Dc),
                                specular = ColladaColor("specular",new Color4(0.25f,0.25f,0.25f,1f)),
                                shininess = ColladaFloat("shininess",50),
                                index_of_refraction = ColladaFloat("index_of_refraction",1)
                            }
                        }
                    }
                }
            }).ToArray();
            var rootNodes = new List<CL.node>();
            BuildNodes(rootModel, rootNodes);
            vscene.node = rootNodes.ToArray();
            dae.Items = new object[] { efx, mats, geos, scenes };
            using (var stream = File.Create(output))
                ColladaSupport.XML.Serialize(stream, dae);
        }
        static CL.common_color_or_texture_type ColladaColor(string sid, Color4 c)
        {
            var cl = new CL.common_color_or_texture_type();
            cl.Item = new CL.common_color_or_texture_typeColor() { sid = sid, Text =
                String.Join(" ", new[] { c.R, c.G, c.B, c.A }.Select((x) => x.ToString()))
                };
            return cl;
        }
        static CL.common_float_or_param_type ColladaFloat(string sid, float f)
        {
            var cl = new CL.common_float_or_param_type();
            cl.Item = new CL.common_float_or_param_typeFloat() { sid = sid, Value = f };
            return cl;
        }
        static void BuildNodes(InputModel mdl, List<CL.node> parent) 
        {
            var node = mdl.Export.GetNode(0, mdl.Transform, mdl.Model.Path);
            if(mdl.Children.Count > 0) {
                var children = new List<CL.node>();
                foreach (var child in mdl.Children)
                    BuildNodes(child, children);
                node.node1 = children.ToArray();
            }
            parent.Add(node);
            for (int i = 1; i < mdl.Export.Geometries.Count; i++) {
                var n = mdl.Export.GetNode(i, mdl.Transform, mdl.Model.Path);
                parent.Add(n);
            }
        }
        static void BuildModel(ResourceManager res, InputModel mdl, List<CL.geometry> geoList, List<string> mats, List<ExportMaterial> matinfos)
        {
            var processed = ProcessModel(mdl.Model, res);
            mdl.Export = processed;
            geoList.AddRange(processed.Geometries);
            foreach (var mat in processed.Materials)
            {
                if (!mats.Any((x) => x == mat.Name))
                {
                    mats.Add(mat.Name);
                    matinfos.Add(mat);
                }
            }
            foreach (var child in mdl.Children)
                BuildModel(res, child, geoList, mats, matinfos);
        }
        class ExportMaterial
        {
            public string Name;
            public Color4 Dc;
        }
        class ExportModel
        {
            public List<CL.geometry> Geometries = new List<CL.geometry>();
            public List<ExportMaterial> Materials = new List<ExportMaterial>();

            CL.instance_material[] GetMaterials(CL.geometry g)
            {
                var materials = new List<CL.instance_material>();
                foreach (var item in ((CL.mesh)g.Item).Items)
                {
                    string matref = ((CL.triangles)item).material;
                    if (!materials.Any((m) => m.symbol == matref))
                    {
                        materials.Add(new CL.instance_material()
                        {
                            symbol = matref,
                            target = "#" + matref
                        });
                    }
                }
                return materials.ToArray();
            }
            string MatrixText(Matrix4 t)
            {
                var floats = new float[] {
                    t.M11, t.M21, t.M31, t.M41,
                    t.M12, t.M22, t.M32, t.M42,
                    t.M13, t.M23, t.M33, t.M43,
                    t.M41, t.M42, t.M43, t.M44
                };
                return string.Join(" ", floats.Select((x) => x.ToString(CultureInfo.InvariantCulture)));
            }
            public CL.node GetNode(int index, Matrix4 transform, string name)
            {
                var n = new CL.node();
                if (index != 0)
                {
                    name += "_lod" + index;
                }
				n.name = n.id = name;
                    n.Items = new object[] {
                        new CL.matrix() {
                            sid = "transform",
                            Text = MatrixText(transform)
                        }
                    };
                n.ItemsElementName = new CL.ItemsChoiceType7[] {
                    CL.ItemsChoiceType7.matrix
                };
                n.instance_geometry = new CL.instance_geometry[] {
                    new CL.instance_geometry{
                        url = "#" + Geometries[index].id,
                        bind_material = new CL.bind_material() {
                            technique_common = GetMaterials(Geometries[index])
                        }
                    }
                };
                return n;
            }
        }

        static ExportModel ProcessModel(ModelFile mdl, ResourceManager resources)
        {
            mdl.Path = mdl.Path.Replace(' ', '_');
            var ex = new ExportModel();
            for (int midx = 0; midx < mdl.Levels.Length; midx++)
            {
                var lvl = mdl.Levels[midx];
                var processed = ProcessRef(lvl, resources);
                var geo = new CL.geometry();
                geo.name = geo.id = mdl.Path + "-level" + midx;
                var mesh = new CL.mesh();
                geo.Item = mesh;
                CL.source positions;
                CL.source normals = null;
                CL.source colors = null;
                CL.source tex1 = null;
                CL.source tex2 = null;
                int idxC = 1;
                positions = CreateSource(
                    geo.name + "-positions",
                    (k) => new Vector4(processed.Vertices[k].Position),
                    3, processed.Vertices.Length);
                mesh.vertices = new CL.vertices()
                {
                    id = geo.name + "-vertices",
                    input = new CL.InputLocal[] {new CL.InputLocal()
                    {
                            semantic = "POSITION", source = "#" + positions.id
                    }}
                };
                var sources = new List<CL.source>() { positions };
                if((processed.FVF & D3DFVF.NORMAL) == D3DFVF.NORMAL) {
                    normals = CreateSource(
                        geo.name + "-normals",
                        (k) => new Vector4(processed.Vertices[k].Normal),
                        3,processed.Vertices.Length);
                    sources.Add(normals);
                    idxC++;
                }
                if((processed.FVF & D3DFVF.DIFFUSE) == D3DFVF.DIFFUSE) {
                    colors = CreateSource(
                        geo.name + "-color",
                        (k) =>
                        {
                            var c = Color4.FromRgba(processed.Vertices[k].Diffuse);
                            return new Vector4(c.R, c.G, c.B, c.A);
                        }, 4, processed.Vertices.Length);
                    sources.Add(colors);
                    idxC++;
                }
                bool doTex1, doTex2 = false;
                if ((processed.FVF & D3DFVF.TEX2) == D3DFVF.TEX2)
                    doTex1 = doTex2 = true;
                else if ((processed.FVF & D3DFVF.TEX1) == D3DFVF.TEX1)
                    doTex1 = true;
                else
                    doTex1 = doTex2 = false;
                if(doTex1) {
                    tex1 = CreateSource(
                        geo.name + "-tex1",
                        (k) => new Vector4(processed.Vertices[k].TextureCoordinate),
                        2, processed.Vertices.Length);
                    sources.Add(tex1);
                    idxC++;
                }
                if(doTex2) {
                    tex2 = CreateSource(
                        geo.name + "-tex2",
                        (k) => new Vector4(processed.Vertices[k].TextureCoordinateTwo),
                        2, processed.Vertices.Length);
                    sources.Add(tex2);
                    idxC++;
                }
                mesh.source = sources.ToArray();
                var items = new List<object>();
                foreach(var dc in processed.Drawcalls) {
                    if (!ex.Materials.Any((x) => x.Name == dc.Material.Name)) ex.Materials.Add(dc.Material);
                    var trs = new CL.triangles();
                    trs.count = (ulong)(dc.Indices.Length / 3);
                    trs.material = dc.Material.Name + "-material";
                    List<int> pRefs = new List<int>(dc.Indices.Length * idxC);
                    List<CL.InputLocalOffset> inputs = new List<CL.InputLocalOffset>() {
                        new CL.InputLocalOffset() {
                            semantic = "VERTEX", source = "#" + geo.id + "-vertices", offset = 0
                        }
                    };
                    ulong off = 1;
                    if (normals != null)
                        inputs.Add(new CL.InputLocalOffset()
                        {
                            semantic = "NORMAL",
                            source = "#" + normals.id,
                            offset = off++
                        });
                    if (colors != null)
                        inputs.Add(new CL.InputLocalOffset()
                        {
                            semantic = "COLOR",
                            source = "#" + colors.id,
                            offset = off++
                        });
                    if (tex1 != null)
                        inputs.Add(new CL.InputLocalOffset()
                        {
                            semantic = "TEXCOORD",
                            source = "#" + tex1.id,
                            offset = off++
                        });
                    if (tex2 != null)
                        inputs.Add(new CL.InputLocalOffset()
                        {
                            semantic = "TEXCOORD",
                            source = "#" + tex2.id,
                            offset = off++
                        });
                    trs.input = inputs.ToArray();
                    for (int i = 0; i < dc.Indices.Length; i++) {
                        for (int j = 0; j < idxC; j++)
                            pRefs.Add(dc.Indices[i]);
                    }
                    trs.p = string.Join(" ", pRefs.ToArray());
                    items.Add(trs);
                }
                mesh.Items = items.ToArray();
                ex.Geometries.Add(geo);
            }
            return ex;
        }

        static CL.source CreateSource(string id,Func<int,Vector4> get, int components, int len)
        {
            var src = new CL.source();
            src.id = id;
            var floats = new float[len * components];
            for (int i = 0; i < len;i++) {
                var v4 = get(i);
                floats[i * components] = v4.X;
                floats[i * components + 1] = v4.Y;
                if (components > 2)
                    floats[i * components + 2] = v4.Z;
                if (components > 3)
                    floats[i * components + 3] = v4.W;
            }
            string arrId = id + "-array";
            src.Item = new CL.float_array()
            {
                id = arrId,
                Text = string.Join(" ", floats.Select((x) => x.ToString(CultureInfo.InvariantCulture)))
            };
            src.technique_common = new CL.sourceTechnique_common();
            var acc = new CL.accessor()
            {
                source = "#" + arrId,
                count = (ulong)len,
                stride = (ulong)components
            };
            src.technique_common.accessor = acc;
            if(components == 2) {
                acc.param = new CL.param[] {
                    new CL.param() { name = "U", type = "float" },
                    new CL.param() { name = "V", type = "float" }
                };
            } else if (components == 3) {
                acc.param = new CL.param[] {
                    new CL.param() { name = "X", type = "float" },
                    new CL.param() { name = "Y", type = "float" },
                    new CL.param() { name = "Z", type = "float" }
                };
            } else if (components == 4) {
                acc.param = new CL.param[] {
                    new CL.param() { name = "R", type = "float" },
                    new CL.param() { name = "G", type = "float" },
                    new CL.param() { name = "B", type = "float" },
                    new CL.param() { name = "A", type = "float" }
                };
            }
            return src;
        }
        static VMeshDump ProcessRef(VMeshRef vms, ResourceManager resources)
        {
            var d = new VMeshDump();
            List<VertexPositionNormalDiffuseTextureTwo> verts = new List<VertexPositionNormalDiffuseTextureTwo>();
            List<int> hashes = new List<int>();
            for (int meshi = vms.StartMesh; meshi < vms.StartMesh + vms.MeshCount; meshi++) {
                var m = vms.Mesh.Meshes[meshi];
                var dc = new VmsDrawcall();
                LibreLancer.Utf.Mat.Material mat;
                if((mat = resources.FindMaterial(m.MaterialCrc)) != null) {
                    dc.Material = new ExportMaterial() { 
                        Name = mat.Name,
                        Dc = mat.Dc
                    };
                } else {
                    dc.Material = new ExportMaterial() {
                        Name = string.Format("material_0x{0:X8}", m.MaterialCrc),
                        Dc = Color4.White
                    };
                }
                List<int> indices = new List<int>(m.NumRefVertices);
                for (int i = m.TriangleStart; i < m.TriangleStart + m.NumRefVertices; i++) {
                    int idx = vms.Mesh.Indices[i] + m.StartVertex + vms.StartVertex;
                    VertexPositionNormalDiffuseTextureTwo vert;
                    if (vms.Mesh.verticesVertexPosition != null)
                        vert = new VertexPositionNormalDiffuseTextureTwo() { Position = vms.Mesh.verticesVertexPosition[idx].Position };
                    else if (vms.Mesh.verticesVertexPositionNormal != null) {
                        vert = new VertexPositionNormalDiffuseTextureTwo()
                        {
                            Position = vms.Mesh.verticesVertexPositionNormal[idx].Position,
                            Normal = vms.Mesh.verticesVertexPositionNormal[idx].Normal
                        };
                    } else if (vms.Mesh.verticesVertexPositionTexture != null) {
                        vert = new VertexPositionNormalDiffuseTextureTwo()
                        {
                            Position = vms.Mesh.verticesVertexPositionTexture[idx].Position,
                            TextureCoordinate = vms.Mesh.verticesVertexPositionTexture[idx].TextureCoordinate
                        };
                    } else if (vms.Mesh.verticesVertexPositionNormalTexture != null) {
                        vert = new VertexPositionNormalDiffuseTextureTwo()
                        {
                            Position = vms.Mesh.verticesVertexPositionNormalTexture[idx].Position,
                            Normal = vms.Mesh.verticesVertexPositionNormalTexture[idx].Normal,
                            TextureCoordinate = vms.Mesh.verticesVertexPositionNormalTexture[idx].TextureCoordinate
                        };
                    } else if (vms.Mesh.verticesVertexPositionNormalTextureTwo != null) {
                        vert = new VertexPositionNormalDiffuseTextureTwo()
                        {
                            Position = vms.Mesh.verticesVertexPositionNormalTextureTwo[idx].Position,
                            Normal = vms.Mesh.verticesVertexPositionNormalTextureTwo[idx].Normal,
                            TextureCoordinate = vms.Mesh.verticesVertexPositionNormalTextureTwo[idx].TextureCoordinate,
                            TextureCoordinateTwo = vms.Mesh.verticesVertexPositionNormalTextureTwo[idx].TextureCoordinateTwo
                        };
                    } else if (vms.Mesh.verticesVertexPositionNormalDiffuseTexture != null) {
                        vert = new VertexPositionNormalDiffuseTextureTwo()
                        {
                            Position = vms.Mesh.verticesVertexPositionNormalDiffuseTexture[idx].Position,
                            Normal = vms.Mesh.verticesVertexPositionNormalDiffuseTexture[idx].Normal,
                            Diffuse = vms.Mesh.verticesVertexPositionNormalDiffuseTexture[idx].Diffuse,
                            TextureCoordinate = vms.Mesh.verticesVertexPositionNormalDiffuseTexture[idx].TextureCoordinate
                        };
                    } else if (vms.Mesh.verticesVertexPositionNormalDiffuseTextureTwo != null) {
                        vert = vms.Mesh.verticesVertexPositionNormalDiffuseTextureTwo[idx];
                    }
                    else
                        throw new Exception("something in state is real bad"); //Never called

                    var hash = ColladaSupport.HashVert(ref vert);
                    int newIndex = ColladaSupport.FindDuplicate(hashes, verts, 0, ref vert, hash);
                    if(newIndex == -1) {
                        newIndex = verts.Count;
                        verts.Add(vert);
                        hashes.Add(hash);
                    }
                    indices.Add(newIndex);
                }
                dc.Indices = indices.ToArray();
                d.Drawcalls.Add(dc);
            }
            d.Vertices = verts.ToArray();
            d.FVF = vms.Mesh.OriginalFVF;
            return d;
        }
        class VMeshDump
        {
            public VertexPositionNormalDiffuseTextureTwo[] Vertices;
            public D3DFVF FVF;
            public List<VmsDrawcall> Drawcalls = new List<VmsDrawcall>();
        }
        class VmsDrawcall
        {
            public ExportMaterial Material;
            public int[] Indices;
        }
        static CL.COLLADA NewCollada()
        {
            var dae = new CL.COLLADA();
            dae.asset = new CL.asset();
            dae.asset.created = dae.asset.modified = DateTime.Now;
            dae.asset.contributor = new CL.assetContributor[] {
                new CL.assetContributor() {
                    author = "LancerEdit User",
                    authoring_tool = "LancerEdit"
                }
            };
            return dae;
        }
    }
}
