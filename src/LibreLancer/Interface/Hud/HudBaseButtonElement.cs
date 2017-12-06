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
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Collections.Generic;
namespace LibreLancer
{
	public class HudBaseButtonElement : HudToggleButtonElement
	{
		public Color4 ActiveColor = Color4.Yellow;
		public Color4 InactiveColor = new Color4(160, 196, 210, 255);

		class ModifiedMaterial
		{
			public BasicMaterial Mat;
			public Color4 Dc;
		}
		List<ModifiedMaterial> materials = new List<ModifiedMaterial>();

		public HudBaseButtonElement(UIManager manager, string pathA, float x, float y, float scaleX, float scaleY) : base(manager, pathA, null, x, y, scaleX, scaleY)
		{
			var l0 = ((Utf.Cmp.ModelFile)drawableA).Levels[0];
			var vms = l0.Mesh;
			//Save Mesh material state
			for (int i = l0.StartMesh; i < l0.StartMesh + l0.MeshCount; i++)
			{
				var mat = (BasicMaterial)vms.Meshes[i].Material?.Render;
				if (mat == null) continue;
				bool found = false;
				foreach (var m in materials) {
					if (m.Mat == mat)
					{
						found = true;
						break;
					}
				}
				if (found) continue;
				materials.Add(new ModifiedMaterial() { Mat = mat, Dc = mat.Dc });
			}
		}


		public override unsafe void DrawBase()
		{
			var vms = ((Utf.Cmp.ModelFile)drawableA).Levels[0].Mesh;
			//Save and restore material colours since it affects other parts of the UI
			for (int i = 0; i < materials.Count; i++)
			{
				if (State == ToggleState.Active)
				{
					materials[i].Mat.Dc = ActiveColor;
				}
				else
				{
					materials[i].Mat.Dc = InactiveColor;
				}
			}
			drawableA.Update(IdentityCamera.Instance, TimeSpan.Zero, TimeSpan.FromSeconds(Manager.Game.TotalTime));
			drawableA.Draw(Manager.Game.RenderState, GetWorld(UIScale, Position), Lighting.Empty);
			for (int i = 0; i < materials.Count; i++)
			{
				materials[i].Mat.Dc = materials[i].Dc;
			}
		}
	}
}
