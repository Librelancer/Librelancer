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
using LibreLancer.Utf.Cmp;
namespace LibreLancer
{
	//TODO: Merge common code with UIMenuButton and HudBaseButtonElement
	public class UIXButton : UIElement
	{
		ModelFile model;
		class ModifiedMaterial
		{
			public BasicMaterial Mat;
			public Color4 Dc;
		}
		List<ModifiedMaterial> materials = new List<ModifiedMaterial>();

		public UIXButton(UIManager man, float x, float y, float scaleX, float scaleY) : base(man)
		{
			model = (ModelFile)man.Game.ResourceManager.GetDrawable(man.Game.GameData.ResolveDataPath("INTERFACE\\TEXTOFFER\\x.3db"));
			UIPosition = new Vector2(x, y);
			UIScale = new Vector2(scaleX, scaleY);
			var l0 = model.Levels[0];
			var vms = l0.Mesh;
			//Save Mesh material state
			for (int i = l0.StartMesh; i < l0.StartMesh + l0.MeshCount; i++)
			{
				var mat = (BasicMaterial)vms.Meshes[i].Material?.Render;
				if (mat == null) continue;
				bool found = false;
				foreach (var m in materials)
				{
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

		Color4 color;
		protected override void UpdateInternal(TimeSpan time)
		{
			Rectangle r;
			TryGetHitRectangle(out r);
			if (r.Contains(Manager.Game.Mouse.X, Manager.Game.Mouse.Y))
				color = GetPulseColor();
			else
				color = Manager.TextColor;
		}

		public event Action Clicked;

		public override void WasClicked()
		{
			if (Clicked != null) Clicked();
		}
		Color4 GetPulseColor()
		{
			//TODO: Made this function playing around in GeoGebra. Probably not great
			double pulseState = Math.Abs(Math.Cos(9 * Manager.Game.TotalTime));
			var a = new Color3f(Manager.TextColor.R, Manager.TextColor.G, Manager.TextColor.B);
			var b = new Color3f(Color4.Yellow.R, Color4.Yellow.G, Color4.Yellow.B);
			var result = Utf.Ale.AlchemyEasing.EaseColorRGB(
				Utf.Ale.EasingTypes.Linear,
				(float)pulseState,
				0,
				1,
				a,
				b
			);
			return new Color4(result.R, result.G, result.B, 1);
		}

		public override bool TryGetHitRectangle(out Rectangle rect)
		{
			var tl = IdentityCamera.Instance.ScreenToPixel(UIPosition.X - (UIScale.X) * 0.012f, UIPosition.Y + (UIScale.Y) * 0.012f);
			var br = IdentityCamera.Instance.ScreenToPixel(UIPosition.X + (UIScale.X) * 0.015f, UIPosition.Y - (UIScale.Y) * 0.015f);
			rect = new Rectangle(
				(int)tl.X,
				(int)tl.Y,
				(int)(br.X - tl.X),
				(int)(br.Y - tl.Y)
			);
			return true;
		}

		public override void DrawText()
		{
		}

		public override void DrawBase()
		{
			for (int i = 0; i < materials.Count; i++)
			{
				materials[i].Mat.Dc = color;
			}
			model.Update(IdentityCamera.Instance, TimeSpan.Zero, TimeSpan.FromSeconds(Manager.Game.TotalTime));
			model.Draw(
				Manager.Game.RenderState,
				GetWorld(UIScale, UIPosition),
				Lighting.Empty
			);
			for (int i = 0; i < materials.Count; i++)
			{
				materials[i].Mat.Dc = materials[i].Dc;
			}
		}
	}
}
