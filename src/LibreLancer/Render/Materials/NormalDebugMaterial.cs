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
using System.Collections.Generic;
using LibreLancer.Vertices;
using LibreLancer.Utf.Mat;

namespace LibreLancer
{
	public class NormalDebugMaterial : RenderMaterial
	{
		static ShaderVariables sh;
		public override void Use(RenderState rstate, IVertexType vertextype, Lighting lights)
		{
			if (sh == null)
				sh = ShaderCache.Get("Normals_PositionNormal.vs", "Normals.frag");
			if (Camera == null)
				return;
			rstate.BlendMode = BlendMode.Opaque;
			sh.SetViewProjection(Camera);
			//Dt
			var normalMatrix = World;
			normalMatrix.Invert();
			normalMatrix.Transpose();
			sh.SetWorld(ref World);
			sh.SetNormalMatrix(ref normalMatrix);
			sh.UseProgram();
		}

		public override void ApplyDepthPrepass(RenderState rstate)
		{
			throw new NotImplementedException();
		}

		public override bool IsTransparent
		{
			get
			{
				return false;
			}
		}
	}
}

