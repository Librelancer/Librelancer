// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using BepuUtilities;
using LibreLancer.ContentEdit.Model;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Vms;
using SimpleMesh;

namespace LibreLancer.ContentEdit
{
    public static class UiIconGenerator
    {
        static Vertex IcoVert(Vector3 pos, Vector2 tex1) => new Vertex(
            pos, Vector3.UnitZ, Vector4.One, Vector4.Zero,
            tex1, Vector2.Zero, Vector2.Zero, Vector2.Zero);

        private static Vertex[] vertices_ship = {
            IcoVert(new Vector3(0.035523005f,-0.034072388f,-8.816621E-08f), new Vector2(0.99950f,0.00050f)),
            IcoVert(new Vector3(0.035523005f, 0.034072228f,-8.765389E-08f), new Vector2(0.99950f,0.99950f)),
            IcoVert(new Vector3(-0.035523012f,-0.034072388f,-8.816621E-08f), new Vector2(0.00050f,0.00050f)),
            IcoVert(new Vector3(-0.035523012f, 0.034072217f,-8.765389E-08f), new Vector2(0.00050f,0.99950f)),
        };

        private static ushort[] indices_ship = {
            0, 1, 2, 1, 3, 2
        };

        public static EditableUtf UncompressedFromFile(string iconName, string filename, bool alpha)
        {
            var texNode = new LUtfNode();
            var tgaNodes = TextureImport.TGAMipmaps(filename, MipmapMethod.Lanczos4, true);
            foreach (var n in tgaNodes)
                n.Parent = texNode;
            texNode.Children = tgaNodes;
            return Generate(iconName, texNode, alpha);
        }

        public static EditableUtf CompressedFromFile(string iconName, string filename, bool alpha)
        {
            var ddsNode = new LUtfNode() {Children = new List<LUtfNode>()};
            ddsNode.Children.Add(new LUtfNode()
            {
                Name = "MIPS",
                Data = TextureImport.CreateDDS(filename, DDSFormat.DXT5, MipmapMethod.Lanczos4, true, true),
                Parent = ddsNode,
            });
            return Generate(iconName, ddsNode, alpha);
        }

        public static EditableUtf Generate(string iconName, LUtfNode textureNode, bool alpha)
        {
            var modelFile = new EditableUtf();
            var unique = IdSalt.New();
            string textureName = $"data.icon.{iconName}.{unique}.tga";
            string materialName = $"data.icon.{iconName}.{unique}";
            string meshName = $"data.icon.{iconName}.lod0-{unique}.vms";
            //VMeshLibrary
            var geom = new Geometry();
            geom.Vertices = vertices_ship;
            geom.Indices = new Indices { Indices16 = indices_ship };
            geom.Attributes = VertexAttributes.Position | VertexAttributes.Normal | VertexAttributes.Texture1;
            geom.Groups = new TriangleGroup[]
            {
                new TriangleGroup() { BaseVertex =  0, StartIndex = 0, Material = new SimpleMesh.Material() { Name = materialName }, IndexCount = 6 }
            };
            geom.Min = new Vector3(-0.03552301f, -0.03407239f, -0.00000009f);
            geom.Max = new Vector3(0.035523f, 0.03407223f, 0.00000009f);
            geom.Center = new Vector3(0.00066627f, -0.00288963f, -0.00000009f);
            geom.Radius = 0.05172854f;
            var vmsLib = new LUtfNode() {Name = "VMeshLibrary", Parent = modelFile.Root, Children = new List<LUtfNode>()};
            modelFile.Root.Children.Add(vmsLib);
            var vmsName = new LUtfNode() {Name = meshName, Parent = vmsLib, Children = new List<LUtfNode>()};
            vmsLib.Children.Add(vmsName);
            vmsName.Children.Add(new LUtfNode()
            {
               Name = "VMeshData",
               Parent = vmsName,
               Data = GeometryWriter.VMeshData(geom, false)
            });
            //VMeshPart
            var vmeshPart = new LUtfNode() {Name = "VMeshPart", Parent = modelFile.Root, Children = new List<LUtfNode>()};
            modelFile.Root.Children.Add(vmeshPart);
            vmeshPart.Children.Add(new LUtfNode() {Name = "VMeshRef", Parent = vmeshPart, Data = GeometryWriter.VMeshRef(geom, meshName)});

            //Texture
            var textureLibrary = new LUtfNode()
            {
                Name = "Texture Library",
                Parent = modelFile.Root
            };
            textureLibrary.Children = new List<LUtfNode>();
            var clonedTex = textureNode.MakeCopy();
            clonedTex.Parent = textureLibrary;
            clonedTex.Name = textureName;
            textureLibrary.Children.Add(clonedTex);
            modelFile.Root.Children.Add(textureLibrary);
            //Material
            var materialLibrary = new LUtfNode()
            {
                Name = "Material Library",
                Parent = modelFile.Root
            };
            materialLibrary.Children = new List<LUtfNode>();
            var material = new LUtfNode()
            {
                Name = materialName,
                Parent = materialLibrary
            };
            material.Children = new List<LUtfNode>();
            material.Children.Add(new LUtfNode()
            {
                Name = "Type",
                Parent = material,
                StringData = alpha ? "DcDtOcOt" : "DcDt"
            });
            material.Children.Add(new LUtfNode()
            {
                Name = "Dt_name",
                Parent = material,
                StringData = textureName
            });
            material.Children.Add(new LUtfNode()
            {
                Name = "Dt_flags",
                Parent = material,
                Data = BitConverter.GetBytes((int)SamplerFlags.Default)
            });
            materialLibrary.Children.Add(material);
            modelFile.Root.Children.Add(materialLibrary);

            return modelFile;
        }
    }
}
