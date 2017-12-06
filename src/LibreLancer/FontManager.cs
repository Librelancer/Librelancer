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
	public class FontManager
	{
		class FontVariations
		{
			public Font Regular;
			public Font Bold;
			public Font Italic;
			public Font BoldItalic;
		}

		bool _loaded = false;
		FreelancerGame game;
		Dictionary<int, FontVariations> infocardFonts = new Dictionary<int, FontVariations>();
		public FontManager(FreelancerGame game)
		{
			this.game = game;
		}

		public void LoadFonts()
		{
			var v = new FontVariations();
			v.Regular = Font.FromSystemFont(game.Renderer2D, "Agency FB", 14);
			v.Bold = Font.FromSystemFont(game.Renderer2D, "Agency FB", 14, FontStyles.Bold);
			v.Italic = Font.FromSystemFont(game.Renderer2D,"Agency FB", 14, FontStyles.Italic);
			v.BoldItalic = Font.FromSystemFont(game.Renderer2D, "Agency FB", 14, FontStyles.Bold | FontStyles.Italic);
			infocardFonts.Add(-1, v);

			foreach (var f in game.GameData.GetRichFonts())
			{
				//points = pixels * 72 / 96
				float sz = f.Size * 72f / 96f;

				var variations = new FontVariations();
				variations.Regular = Font.FromSystemFont(game.Renderer2D, f.Name, sz);
				variations.Bold = Font.FromSystemFont(game.Renderer2D, f.Name, sz, FontStyles.Bold);
				variations.Italic = Font.FromSystemFont(game.Renderer2D, f.Name, sz, FontStyles.Italic);
				variations.BoldItalic = Font.FromSystemFont(game.Renderer2D, f.Name, sz, FontStyles.Bold | FontStyles.Italic);
				infocardFonts.Add(f.Index, variations);
			}
			_loaded = true;
		}

		public Font GetInfocardFont(int index, FontStyles style)
		{
			if (!_loaded)
				LoadFonts();
			var variations = infocardFonts[index];
			switch (style)
			{
				case FontStyles.Regular:
					return variations.Regular;
				case FontStyles.Bold:
					return variations.Bold;
				case FontStyles.Italic:
					return variations.Italic;
				case FontStyles.Bold | FontStyles.Italic:
					return variations.BoldItalic;
				default:
					throw new ArgumentException("FontStyles has invalid value", nameof(style));
			}
		}

	}
}
