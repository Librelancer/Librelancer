// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Vms;

using LibreLancer.Primitives;

namespace LibreLancer.Utf.Mat
{
    /// <summary>
    /// Represents a UTF Sphere File (.sph)
    /// </summary>
    public class SphFile : UtfFile, IDrawable
    {
        private QuadSphere sphere;
		
		private bool ready;

        private ILibFile library;

		public MatFile MaterialLibrary;
		public TxmFile TextureLibrary;
		public VmsFile VMeshLibrary;
        public float Radius { get; private set; }

        private List<string> sideMaterialNames;
        private Material[] sideMaterials;
        public Material[] SideMaterials
        {
            get
            {
                if (sideMaterials == null)
                {
                    sideMaterials = new Material[sideMaterialNames.Count];
                    for (int i = 0; i < sideMaterialNames.Count; i++)
                    {
                        sideMaterials[i] = library.FindMaterial(CrcTool.FLModelCrc(sideMaterialNames[i]));
                        //if (sideMaterials[i] == null) sideMaterials[i] = new Material();
                    }
                }

                return sideMaterials;
            }
        }

		public List<string> SideMaterialNames
		{
			get
			{
				return sideMaterialNames;
			}
		}

		public SphFile(string path, ILibFile materialLibrary) : this(parseFile(path), materialLibrary)
		{

		}
		public SphFile(IntermediateNode root, ILibFile library)
        {
            if (root == null) throw new ArgumentNullException("root");
            if (library == null) throw new ArgumentNullException("materialLibrary");

            ready = false;

			this.library = library;
            sideMaterialNames = new List<string>();

			bool sphereSet = false;
			foreach (Node node in root)
			{
				switch (node.Name.ToLowerInvariant())
				{
					case "sphere":
						if (sphereSet) throw new Exception("Multiple sphere nodes");
						sphereSet = true;
						var sphereNode = (IntermediateNode)node;
						foreach (LeafNode sphereSubNode in sphereNode)
						{
							string name = sphereSubNode.Name.ToLowerInvariant();

							if (name.StartsWith("m", StringComparison.OrdinalIgnoreCase)) sideMaterialNames.Add(sphereSubNode.StringData);
							else if (name == "radius") Radius = sphereSubNode.SingleArrayData[0];
							else if (name == "sides")
							{
								int count = sphereSubNode.Int32ArrayData[0];
								if (count != sideMaterialNames.Count) throw new Exception("Invalid number of sides in " + node.Name + ": " + count);
							}
							else throw new Exception("Invalid node in " + node.Name + ": " + sphereSubNode.Name);
						}
						break;
					case "vmeshlibrary":
						IntermediateNode vMeshLibraryNode = node as IntermediateNode;
						if (VMeshLibrary == null) VMeshLibrary = new VmsFile(vMeshLibraryNode, library);
						else throw new Exception("Multiple vmeshlibrary nodes in 3db root");
						break;
					case "material library":
						IntermediateNode materialLibraryNode = node as IntermediateNode;
						if (MaterialLibrary == null) MaterialLibrary = new MatFile(materialLibraryNode, library);
						else throw new Exception("Multiple material library nodes in 3db root");
						break;
					case "texture library":
						IntermediateNode textureLibraryNode = node as IntermediateNode;
						if (TextureLibrary == null) TextureLibrary = new TxmFile(textureLibraryNode);
						else throw new Exception("Multiple texture library nodes in 3db root");
						break;
				}
			}
        }
		Material defaultMaterial;
		public void Initialize(ResourceManager cache)
        {
            if (sideMaterialNames.Count >= 6)
			{
                sphere = cache.GetQuadSphere(48);
				defaultMaterial = cache.DefaultMaterial;
                ready = true;
            }
        }

        public void Resized()
        {
            if (ready)
            {
				
                //planetEffect.SetParameter ("Projection", camera.Projection);
                //updatePlanetTexture = true;
            }
        }
        ICamera _camera;
		public void Update(ICamera camera, TimeSpan delta, TimeSpan totalTime)
        {
            if (ready)
            {
                _camera = camera;
				if (SideMaterials.Length > 6) {
					//var mat = (AtmosphereMaterial)SideMaterials [6].Render;
					//mat.Projection = camera.Projection;
					//mat.CameraPosition = camera.Position;
				}
            }
        }
		static CubeMapFace[] faces = new CubeMapFace[] {
			CubeMapFace.PositiveZ,
			CubeMapFace.PositiveX,
			CubeMapFace.NegativeZ,
			CubeMapFace.NegativeX,
			CubeMapFace.PositiveY,
			CubeMapFace.NegativeY
		};
		public float GetRadius()
		{
			return Radius;
		}
		public void Draw(RenderState rstate, Matrix4 world, Lighting lights)
        {
			throw new NotImplementedException();
        }
		public void DepthPrepass(RenderState rstate, Matrix4 world)
		{
			if (SideMaterials.Length < 6)
				return;
			var transform = Matrix4.CreateScale(Radius) * world;
			for (int i = 0; i < 6; i++)
			{
				if (SideMaterials[i].Render.IsTransparent) continue;
				int start, count;
				Vector3 pos;
				sphere.GetDrawParameters(faces[i], out start, out count, out pos);
				SideMaterials[i].Render.Camera = _camera;
				SideMaterials[i].Render.World = transform;
				SideMaterials[i].Render.ApplyDepthPrepass(rstate);
				sphere.VertexBuffer.Draw(PrimitiveTypes.TriangleList, 0, start, count);
			}
		}
		public void DrawBuffer(CommandBuffer buffer, Matrix4 world, ref Lighting lighting, Material overrideMat = null)
		{
			if (SideMaterials.Length < 6)
				return;
			if (ready)
			{
				for (int i = 0; i < SideMaterials.Length; i++)
					if (SideMaterials[i] != null && !SideMaterials[i].Loaded) SideMaterials[i].Loaded = false;
				for (int i = 0; i < 6; i++)
				{
					int start, count;
					Vector3 pos;
					sphere.GetDrawParameters(faces[i], out start, out count, out pos);
					if(SideMaterials[i] == null) SideMaterials[i] = library.FindMaterial(CrcTool.FLModelCrc(sideMaterialNames[i]));
					var mat = SideMaterials[i] ?? defaultMaterial;
					mat = overrideMat ?? mat;
                    mat.Render.Camera = _camera;
					var transform = Matrix4.CreateScale(Radius) * world;
					buffer.AddCommand(
						mat.Render,
						null,
						transform,
						lighting,
						sphere.VertexBuffer,
						PrimitiveTypes.TriangleList,
						0,
						start,
						count,
						SortLayers.OBJECT
					);
				}
				//Draw atmosphere
				if (SideMaterials.Length > 6 && overrideMat == null)
				{
					if (SideMaterials[6] == null)
					{
						SideMaterials[6] = library.FindMaterial(CrcTool.FLModelCrc(sideMaterialNames[6]));
						if (SideMaterials[6] == null) return;
					}
					var mat = (AtmosphereMaterial)SideMaterials[6].Render;
					var transform = Matrix4.CreateScale(Radius * mat.Scale) * world;
					for (int i = 0; i < 6; i++)
					{
						int start, count;
						Vector3 pos;
						sphere.GetDrawParameters(faces[i], out start, out count, out pos);
						SideMaterials[6].Render.Camera = _camera;
						buffer.AddCommand(
							SideMaterials[6].Render,
							null,
							transform,
							lighting,
							sphere.VertexBuffer,
							PrimitiveTypes.TriangleList,
							0,
							start,
							count,
							SortLayers.OBJECT,
							RenderHelpers.GetZ(transform, _camera.Position, pos)
						);
					}
				}
			}
			else
				throw new Exception();
		}
    }
}