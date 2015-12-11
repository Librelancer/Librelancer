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
        private Quad quad;
        private Ellipsoid sphere;
        //private EffectInstance planetEffect;
		Shader shader;
        private TextureCube planetTexture;
        private bool ready, updatePlanetTexture;

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
            updatePlanetTexture = true;

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

		public void Initialize(ResourceCache cache)
        {
            if (sideMaterialNames.Count >= 6)
            {
                //this.device = device;
                //this.camera = camera;

                quad = new Quad();
                sphere = new Ellipsoid(new Vector3(Radius), 48, 64);

                foreach (Material m in SideMaterials)
                    m.Initialize(cache);

                //planetEffect = content.Load<EffectInstance>("effects/Planet");
				//planetEffect.SetParameter ("Projection", camera.Projection);
				shader = ShaderCache.Get (
					"Planet.vs",
					"Planet.frag"
				);
				drawPlanetTexture ();
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
		public void Update(Camera camera)
        {
            if (ready)
            {
				viewproj = camera.ViewProjection;

				//planetEffect.SetParameter ("View", camera.View);
            }
        }
		static int ddsno = 0;
        private void drawPlanetTexture()
        {
			
			var rendertarget = new RenderTarget2D (512, 512);
			planetTexture = new TextureCube (512, false, SurfaceFormat.Color);
			GL.BindFramebuffer (FramebufferTarget.Framebuffer, rendertarget.FBO);
			ViewportManager.Instance.Push (0, 0, 512, 512);
			Colorb[] data = new Colorb[512 * 512];
			CubeMapFace[] faces = new CubeMapFace[] {
				CubeMapFace.PositiveZ,
				CubeMapFace.PositiveX,
				CubeMapFace.NegativeZ,
				CubeMapFace.NegativeX,
				CubeMapFace.PositiveY,
				CubeMapFace.NegativeY
			};
			for (int i = 0; i < 6; i++) {
				GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
				SideMaterials [i].Render.World = Matrix4.Identity;
				SideMaterials [i].Render.ViewProjection = Matrix4.Identity;
				SideMaterials [i].Render.Use (quad.VertexBuffer.VertexType, new Lighting ());
				quad.VertexBuffer.Draw (PrimitiveTypes.TriangleList, 0, 0, Quad.PrimitiveCount);
				rendertarget.GetData (data);
				DDSLib.DDSToFile ("rendered" + ddsno++ + ".dds", false, rendertarget, false);
				planetTexture.SetData (faces[i], data);
			}
			GL.BindFramebuffer (FramebufferTarget.Framebuffer, 0);
			ViewportManager.Instance.Pop ();
			updatePlanetTexture = false;
           /* // Generate TextureCube for planet
            planetTexture = new TextureCube(device, 512, true, SurfaceFormat.Color);
			var renderTarget = new RenderTarget2D (device, 512, 512);
            device.SetVertexBuffer(quad.VertexBuffer);
            device.Indices = Quad.IndexBuffer;
			Color[] data = new Color[512 * 512];
			device.SetRenderTarget (renderTarget);
            //device.SetRenderTarget(planetTexture, CubeMapFace.PositiveZ);
            device.Clear(ClearOptions.Target, Color.Magenta, 0, 0);
            SideMaterials[0].Draw(D3DFVF.XYZ | D3DFVF.TEX1, PrimitiveTypes.TriangleList, 0, Quad.VERTEX_COUNT, 0, Quad.PrimitiveCount, Color.White, null, Matrix.Identity);
			renderTarget.GetData<Color> (data);
			planetTexture.SetData<Color> (CubeMapFace.PositiveZ, data);
            //device.SetRenderTarget(planetTexture, CubeMapFace.PositiveX);
            device.Clear(ClearOptions.Target, Color.Magenta, 0, 0);
            SideMaterials[1].Draw(D3DFVF.XYZ | D3DFVF.TEX1, PrimitiveTypes.TriangleList, 0, Quad.VERTEX_COUNT, 0, Quad.PrimitiveCount, Color.White, null, Matrix.Identity);
			renderTarget.GetData<Color> (data);
			planetTexture.SetData<Color> (CubeMapFace.PositiveX, data);
            //device.SetRenderTarget(planetTexture, CubeMapFace.NegativeZ);
            device.Clear(ClearOptions.Target, Color.Magenta, 0, 0);
            SideMaterials[2].Draw(D3DFVF.XYZ | D3DFVF.TEX1, PrimitiveTypes.TriangleList, 0, Quad.VERTEX_COUNT, 0, Quad.PrimitiveCount, Color.White, null, Matrix.Identity);
			renderTarget.GetData<Color> (data);
			planetTexture.SetData<Color> (CubeMapFace.NegativeZ, data);
            //device.SetRenderTarget(planetTexture, CubeMapFace.NegativeX);
            device.Clear(ClearOptions.Target, Color.Magenta, 0, 0);
            SideMaterials[3].Draw(D3DFVF.XYZ | D3DFVF.TEX1, PrimitiveTypes.TriangleList, 0, Quad.VERTEX_COUNT, 0, Quad.PrimitiveCount, Color.White, null, Matrix.Identity);
			renderTarget.GetData<Color> (data);
			planetTexture.SetData<Color> (CubeMapFace.NegativeX, data);
            //device.SetRenderTarget(planetTexture, CubeMapFace.PositiveY);
            device.Clear(ClearOptions.Target, Color.Magenta, 0, 0);
            SideMaterials[4].Draw(D3DFVF.XYZ | D3DFVF.TEX1, PrimitiveTypes.TriangleList, 0, Quad.VERTEX_COUNT, 0, Quad.PrimitiveCount, Color.White, null, Matrix.Identity);
			renderTarget.GetData<Color> (data);
			planetTexture.SetData<Color> (CubeMapFace.PositiveY, data);
            //device.SetRenderTarget(planetTexture, CubeMapFace.NegativeY);
            device.Clear(ClearOptions.Target, Color.Magenta, 0, 0);
            SideMaterials[5].Draw(D3DFVF.XYZ | D3DFVF.TEX1, PrimitiveTypes.TriangleList, 0, Quad.VERTEX_COUNT, 0, Quad.PrimitiveCount, Color.White, null, Matrix.Identity);
			renderTarget.GetData<Color> (data);
			planetTexture.SetData<Color> (CubeMapFace.NegativeY, data);

            device.SetRenderTarget(null);
			renderTarget.Dispose ();
            updatePlanetTexture = false;*/
        }

		public void Draw(Matrix4 world, Lighting lights)
        {
            if (ready)
            {
				if (updatePlanetTexture)
					drawPlanetTexture ();
				//Set texture
				shader.SetInteger("planetTexture", 0);
				planetTexture.BindTo (TextureUnit.Texture0);
				//Do the rest
				shader.SetMatrix ("World", ref world);
				shader.SetMatrix ("ViewProjection", ref viewproj);
				shader.SetColor4("AmbientColor", lights.Ambient);
				shader.SetInteger ("LightCount", lights.Lights.Count);
				for (int i = 0; i < lights.Lights.Count; i++) {
					var lt = lights.Lights [i];
					shader.SetVector3 ("LightsPos", lt.Position, i);
					shader.SetVector3 ("LightsRot", lt.Rotation, i);
					shader.SetColor4 ("LightsColor", lt.Color, i);
					shader.SetInteger ("LightsRange", lt.Range, i);
					shader.SetVector3 ("LightsAttenuation", lt.Attenuation, i);
				}
				shader.UseProgram ();
				sphere.VertexBuffer.Draw (
					PrimitiveTypes.TriangleList, 0, 0, 
					sphere.ElementBuffer.IndexCount / 3
				);
                /*if (updatePlanetTexture) drawPlanetTexture();

                // Draw planet
                device.SetVertexBuffer(sphere.VertexBuffer);
                device.Indices = sphere.IndexBuffer;
				lights = null;
                if (lights != null)
                {
					planetEffect.SetParameter ("LightCount", lights.Count);
                    for (int i = 0; i < 9; i++)
                    {
						planetEffect.SetArrayParameter ("LightsPos", i, i < lights.Count ? lights [i].Pos.Value : Vector3.Zero);
						planetEffect.SetArrayParameter ("LightsColor", i, i < lights.Count ? lights [i].Color.Value.ToVector4 () : Vector4.Zero);
						planetEffect.SetArrayParameter ("LightsRange", i, i < lights.Count ? lights [i].Range.Value : 0);
						planetEffect.SetArrayParameter ("LightsAttenuation", i, i < lights.Count ? lights [i].Attenuation ?? new Vector3 (1, 0, 0) : Vector3.Zero);
                    }
                }

				planetEffect.SetParameter ("AmbientColor", ambient.ToVector4 ());
				planetEffect.SetParameter ("World", world);
				planetEffect.SetParameter ("PlanetTexture", planetTexture);
				planetEffect.Apply ();
                device.DrawIndexedPrimitives(PrimitiveTypes.TriangleList, 0, 0, sphere.VertexBuffer.VertexCount, 0, sphere.IndexBuffer.IndexCount / 3);*/
            }
        }
    }
}