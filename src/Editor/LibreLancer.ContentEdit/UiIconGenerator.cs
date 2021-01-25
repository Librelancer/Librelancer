// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Vms;
using Vert = LibreLancer.Vertices.VertexPositionNormalDiffuseTextureTwo;

namespace LibreLancer.ContentEdit
{
    public enum IconType
    {
        Commodity,
        Ship
    }
    public static class UiIconGenerator
    {
        private static Vert[] vertices_commodity = {
            new Vert(new Vector3(-0.030086353f, -0.03651117f,-7.872152E-08f), Vector3.UnitZ, 0, new Vector2(0.00050f,0.99950f), Vector2.Zero),
            new Vert(new Vector3(0.040959664f, -0.03651117f,-7.872152E-08f), Vector3.UnitZ, 0, new Vector2(0.99950f,0.99950f), Vector2.Zero),
            new Vert(new Vector3(0.040959664f, 0.031633444f,-7.82092E-08f), Vector3.UnitZ, 0, new Vector2(0.99950f,0.00050f), Vector2.Zero),
            new Vert(new Vector3(-0.030086353f, 0.031633433f,-7.82092E-08f), Vector3.UnitZ, 0, new Vector2(0.00050f,0.00050f), Vector2.Zero),
        };
        private static ushort[] indices_commodity = {
            0, 1, 2, 0, 2, 3
        };
        
        private static Vert[] vertices_ship = {
            new Vert(new Vector3(0.035523012f, -0.03407232f,-1.1246429E-07f), Vector3.UnitZ, 0, new Vector2(0.99950f,0.99950f), Vector2.Zero),
            new Vert(new Vector3(0.035523012f, 0.034072332f,-1.1195198E-07f), Vector3.UnitZ, 0, new Vector2(0.99950f,0.00050f), Vector2.Zero),
            new Vert(new Vector3(-0.035523023f, -0.03407232f,-1.1246429E-07f), Vector3.UnitZ, 0, new Vector2(0.00050f,0.99950f), Vector2.Zero),
            new Vert(new Vector3(-0.035523023f, 0.034072317f,-1.1195196E-07f), Vector3.UnitZ, 0, new Vector2(0.00050f,0.00050f), Vector2.Zero),
        };

        private static ushort[] indices_ship = {
            0, 1, 2, 1, 3, 2
        };

        public static EditableUtf UncompressedFromFile(IconType type, string iconName, string filename, bool alpha)
        {
            var texNode = new LUtfNode() { Children = new List<LUtfNode>()};
            texNode.Children.Add(new LUtfNode() { Name = "MIP0", Data = TextureImport.TGANoMipmap(filename, true)});
            return Generate(type, iconName, texNode, alpha);
        }

        public static EditableUtf CompressedFromFile(IconType type, string iconName, string filename, bool alpha)
        {
            var ddsNode = new LUtfNode() {Children = new List<LUtfNode>()};
            ddsNode.Children.Add(new LUtfNode()
            {
                Name = "MIPS",
                Data = TextureImport.CreateDDS(filename, alpha ? DDSFormat.DXT5 : DDSFormat.DXT1, MipmapMethod.None, true, true)
            });
            return Generate(type, iconName, ddsNode, alpha);
        }
        
        public static EditableUtf Generate(IconType type, string iconName, LUtfNode textureNode, bool alpha)
        {
            var modelFile = new EditableUtf();
            var unique = IdSalt.New();
            string textureName = $"data.icon.{iconName}.{unique}.tga";
            string materialName = $"data.icon.{iconName}.{unique}";
            string meshName = $"data.icon.{iconName}.lod0-{unique}.vms";
            //VMeshLibrary
            var geom = new ColladaGeometry();
            geom.Vertices = type == IconType.Commodity ? vertices_commodity : vertices_ship;
            geom.Indices = type == IconType.Commodity ? indices_commodity : indices_ship;
            geom.FVF = D3DFVF.NORMAL | D3DFVF.XYZ | D3DFVF.TEX1;
            geom.Drawcalls = new[]
            {
                new ColladaDrawcall() { StartVertex = 0, EndVertex = 3, Material = new ColladaMaterial() { Name = materialName}, StartIndex = 0, TriCount = 2 }
            };
            geom.CalculateDimensions();
            var vmsLib = new LUtfNode() {Name = "VMeshLibrary", Parent = modelFile.Root, Children = new List<LUtfNode>()};
            modelFile.Root.Children.Add(vmsLib);
            var vmsName = new LUtfNode() {Name = meshName, Parent = vmsLib, Children = new List<LUtfNode>()};
            vmsLib.Children.Add(vmsName);
            vmsName.Children.Add(new LUtfNode()
            {
               Name = "VMeshData",
               Parent = vmsName,
               Data = geom.VMeshData()
            });
            //VMeshPart
            var vmeshPart = new LUtfNode() {Name = "VMeshPart", Parent = modelFile.Root, Children = new List<LUtfNode>()};
            modelFile.Root.Children.Add(vmeshPart);
            vmeshPart.Children.Add(new LUtfNode() {Name = "VMeshRef", Parent = vmeshPart, Data = geom.VMeshRef(meshName)});
            
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
                Data = Encoding.ASCII.GetBytes(alpha ? "DcDtOcOt" : "DcDt")
            });
            material.Children.Add(new LUtfNode()
            {
                Name = "Dt_name",
                Parent = material,
                Data = Encoding.ASCII.GetBytes(textureName)
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