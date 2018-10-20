// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
namespace LibreLancer
{
	public class FontManager
	{
		struct FKey
		{
			public string Name;
			public FontStyles Style;
		}
		Dictionary<FKey, Font> systemFonts = new Dictionary<FKey, Font>();
		public Font GetSystemFont(string name, FontStyles style = FontStyles.Regular)
		{
			var k = new FKey() { Name = name, Style = style };
			Font fnt;
			if (!systemFonts.TryGetValue(k, out fnt))
			{
				fnt = Font.FromSystemFont(game.Renderer2D, name, style);
				systemFonts.Add(k, fnt);
			}
			return fnt;
		}
		class FontVariations
		{
			public Font Regular;
			public Font Bold;
			public Font Italic;
			public Font BoldItalic;
			public int Size;
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
			v.Regular = GetSystemFont("Agency FB");
			v.Bold = GetSystemFont("Agency FB", FontStyles.Bold);
			v.Italic = GetSystemFont("Agency FB", FontStyles.Italic);
			v.BoldItalic = GetSystemFont("Agency FB",FontStyles.Bold | FontStyles.Italic);
			v.Size = 14;
			infocardFonts.Add(-1, v);

			foreach (var f in game.GameData.GetRichFonts())
			{
				//points = pixels * 72 / 96
				int sz = (int)(f.Size * 72f / 96f);

				var variations = new FontVariations();
				variations.Size = sz;
				variations.Regular = GetSystemFont(f.Name, FontStyles.Regular);
				variations.Bold = GetSystemFont(f.Name, FontStyles.Bold);
				variations.Italic = GetSystemFont(f.Name, FontStyles.Italic);
				variations.BoldItalic = GetSystemFont(f.Name, FontStyles.Bold | FontStyles.Italic);
				infocardFonts.Add(f.Index, variations);
			}
			_loaded = true;
		}

		public Font GetInfocardFont(int index, FontStyles style, out int size)
		{
			if (!_loaded)
				LoadFonts();
			var variations = infocardFonts[index];
			size = variations.Size;
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
