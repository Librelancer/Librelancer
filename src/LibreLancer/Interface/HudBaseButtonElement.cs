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
namespace LibreLancer
{
	public class HudBaseButtonElement : HudToggleButtonElement
	{
		public Color4 ActiveColor = Color4.Yellow;
		public Color4 InactiveColor = new Color4(160, 196, 210, 255);

		public HudBaseButtonElement(UIManager manager, string pathA, float x, float y, float scaleX, float scaleY) : base(manager, pathA, null, x, y, scaleX, scaleY)
		{
		}

		public override void DrawBase()
		{
			var vms = ((Utf.Cmp.ModelFile)drawableA).Levels[0].Mesh;
			for (int i = 0; i < vms.MeshCount; i++)
			{
				var mat = (BasicMaterial)vms.Meshes[i].Material?.Render;
				if (mat == null) continue;
				if (State == ToggleState.Active)
				{
					mat.Dc = ActiveColor;
				}
				else
				{
					mat.Dc = InactiveColor;
				}
			}
			drawableA.Update(IdentityCamera.Instance, TimeSpan.Zero, TimeSpan.FromSeconds(Manager.Game.TotalTime));
			drawableA.Draw(Manager.Game.RenderState, GetWorld(UIScale, Position), Lighting.Empty);
		}
	}
}
