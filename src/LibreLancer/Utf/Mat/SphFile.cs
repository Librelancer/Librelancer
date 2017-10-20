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
 * The Original Code is Starchart code (http://flapi.sourceforge.net/).
 * Data structure from Freelancer UTF Editor by Cannon & Adoxa, continuing the work of Colin Sanby and Mario 'HCl' Brito (http://the-starport.net)
 * Sphere creation code taken form here: http://forums.create.msdn.com/forums/p/11680/61589.aspx
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2011
 * the Initial Developer. All Rights Reserved.
 */

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

        private ILibFile materialLibrary;

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
                        sideMaterials[i] = materialLibrary.FindMaterial(CrcTool.FLModelCrc(sideMaterialNames[i]));
                        if (sideMaterials[i] == null) sideMaterials[i] = new Material();
                    }
                }

                return sideMaterials;
            }
        }

		public SphFile(string path, ILibFile materialLibrary) : this(parseFile(path), materialLibrary)
		{

		}
        public SphFile(IntermediateNode node, ILibFile materialLibrary)
        {
            if (node == null) throw new ArgumentNullException("node");
            if (materialLibrary == null) throw new ArgumentNullException("materialLibrary");

            ready = false;

            this.materialLibrary = materialLibrary;
            sideMaterialNames = new List<string>();

			IntermediateNode sphereNode = node[0] as IntermediateNode;
			if (sphereNode == null || sphereNode.Name.ToLowerInvariant() != "sphere") throw new FileContentException(FILE_TYPE, "SphFile without sphere");

            foreach (LeafNode sphereSubNode in sphereNode)
            {
                string name = sphereSubNode.Name.ToLowerInvariant();

				if (name.StartsWith ("m", StringComparison.OrdinalIgnoreCase)) sideMaterialNames.Add (sphereSubNode.StringData);
				else if (name == "radius") Radius = sphereSubNode.SingleArrayData [0];
                else if (name == "sides")
                {
					int count = sphereSubNode.Int32ArrayData [0];
                    if (count != sideMaterialNames.Count) throw new Exception("Invalid number of sides in " + sphereNode.Name + ": " + count);
                }
                else throw new Exception("Invalid node in " + sphereNode.Name + ": " + sphereSubNode.Name);
            }
        }

		public void Initialize(ResourceManager cache)
        {
            if (sideMaterialNames.Count >= 6)
			{
				sphere = new QuadSphere(48);

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
		public void DrawBuffer(CommandBuffer buffer, Matrix4 world, Lighting lighting)
		{
			if (SideMaterials.Length < 6)
				return;
			if (ready)
			{
				for (int i = 0; i < 6; i++)
				{
					int start, count;
					Vector3 pos;
					sphere.GetDrawParameters(faces[i], out start, out count, out pos);
                    SideMaterials[i].Render.Camera = _camera;
					buffer.AddCommand(
						SideMaterials[i].Render,
						null,
						Matrix4.CreateScale(Radius) * world,
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
				if (SideMaterials.Length > 6)
				{
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