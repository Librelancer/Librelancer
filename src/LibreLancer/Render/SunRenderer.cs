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
using LibreLancer.Utf.Mat;
using LibreLancer.Primitives;
using LibreLancer.GameData;
using LibreLancer.GameData.Archetypes;
namespace LibreLancer
{
	public class SunRenderer : ObjectRenderer
	{
		public Sun Sun { get; private set; }
		SunMaterial material;
		VertexBuffer vertexBuffer;
		ElementBuffer elementBuffer;
		int primitiveCount;

		public SunRenderer (ICamera camera, Matrix4 world, bool useObjectPosAndRotate, SystemObject sun)
			:base (camera, world, useObjectPosAndRotate, sun)
		{
			Sun = SpaceObject.Archetype as Sun;
			SphFile s = Sun.Drawable as SphFile;

			Ellipsoid sphere = new Ellipsoid(new Vector3(s.Radius), 100, 100);
			vertexBuffer = sphere.VertexBuffer;
			elementBuffer = sphere.ElementBuffer;
			primitiveCount = elementBuffer.IndexCount / 3;
			material = new SunMaterial ();
		}
		public override void Update (TimeSpan elapsed)
		{
			material.ViewProjection = camera.ViewProjection;
		}
		public override void Draw (CommandBuffer buffer, Lighting lights)
		{
			/*material.World = World;
			material.Use (rstate, null, null);
			vertexBuffer.Draw (PrimitiveTypes.TriangleList, 0, 0, primitiveCount)*/
			buffer.AddCommand(
				material,
				World,
				lights,
				vertexBuffer,
				PrimitiveTypes.TriangleList,
				0,
				0,
				primitiveCount
			);
		}
		public override void Dispose ()
		{
			vertexBuffer.Dispose ();
			elementBuffer.Dispose ();
		}
	}
}

