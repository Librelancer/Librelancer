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

using OpenTK;
using OpenTK.Graphics.OpenGL;
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

        public SphFile(string path, ILibFile materialLibrary)
        {
            if (path == null) throw new ArgumentNullException("path");
            if (materialLibrary == null) throw new ArgumentNullException("materialLibrary");

            ready = false;

            this.materialLibrary = materialLibrary;
            sideMaterialNames = new List<string>();

            IntermediateNode sphereNode = parseFile(path)[0] as IntermediateNode;
            if (sphereNode == null) throw new FileContentException(FILE_TYPE, "SphFile without sphere");

            foreach (LeafNode sphereSubNode in sphereNode)
            {
                string name = sphereSubNode.Name.ToLowerInvariant();

                if (name.StartsWith("m", StringComparison.OrdinalIgnoreCase)) sideMaterialNames.Add(sphereSubNode.StringData);
                else if (name == "radius") Radius = sphereSubNode.SingleData.Value;
                else if (name == "sides")
                {
                    int count = sphereSubNode.Int32Data.Value;
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
		Matrix4 viewproj;
		public void Update(ICamera camera, TimeSpan delta)
        {
            if (ready)
            {
				viewproj = camera.ViewProjection;
				if (SideMaterials.Length > 6) {
					var mat = (AtmosphereMaterial)SideMaterials [6].Render;
					mat.CameraPosition = camera.Position;
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
		public void Draw(RenderState rstate, Matrix4 world, Lighting lights)
        {
			if (ready) {
				//Draw for me!
				for (int i = 0; i < 6; i++) {
					SideMaterials [i].Render.World = Matrix4.CreateScale (Radius) * world;
					SideMaterials [i].Render.ViewProjection = viewproj;
					SideMaterials [i].Render.Use (rstate, sphere.VertexType, lights);
					sphere.Draw (faces [i]);
				}
				/*if (SideMaterials.Length > 6) {
					var mat = (AtmosphereMaterial)SideMaterials [6].Render;
					SideMaterials [6].Render.World = Matrix4.CreateScale (Radius * mat.Scale) * world;
					SideMaterials [6].Render.ViewProjection = viewproj;
					SideMaterials [6].Render.Use (rstate, sphere.VertexType, lights);
					for (int i = 0; i < 6; i++)
						sphere.Draw (faces [i]);
				}*/
			} else
				throw new Exception ();
        }
    }
}