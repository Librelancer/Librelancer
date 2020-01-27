// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using LibreLancer.Data;

namespace LibreLancer
{
    public class FontDescription
    {
        public string FontName;
        public float FontSize;
    }

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
            if(ren2d == null) ren2d = game.GetService<Renderer2D>();
            var k = new FKey() { Name = name, Style = style };
			Font fnt;
			if (!systemFonts.TryGetValue(k, out fnt))
			{
				fnt = Font.FromSystemFont(ren2d, name, style);
				systemFonts.Add(k, fnt);
			}
			return fnt;
		}

		bool _loaded = false;
		Game game;
        Renderer2D ren2d;
		Dictionary<int, FontDescription> infocardFonts = new Dictionary<int, FontDescription>();
        private Dictionary<string, string> nicknames;
        public FontManager(Game game)
		{
			this.game = game;
		}

        public void ConstructDefaultFonts()
        {
            var v = new FontDescription() { FontName = "Arial", FontSize = 16 };
            nicknames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            nicknames["normal"] = "Arial";
            infocardFonts.Add(-1, v);
            _loaded = true;
        }
        public string ResolveNickname(string identifier)
        {
            if (nicknames.TryGetValue(identifier, out string o)) return o;
            return nicknames["normal"];
        }
        
        void LoadFonts(FontsIni fonts, RichFontsIni rf, FileSystem fs, string dataPath)
        {
            var d = dataPath.Replace('\\', Path.DirectorySeparatorChar);
            foreach(var f in fonts.FontFiles)
            {
                Platform.AddTtfFile(fs.Resolve(Path.Combine(d, f)));
            }
            nicknames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in fonts.UIFonts)
            {
                nicknames[id.Nickname] = id.Font;
            }
            infocardFonts = new Dictionary<int, FontDescription>();
            var v = new FontDescription() { FontName = "Agency FB", FontSize = 16 };
            infocardFonts.Add(-1, v);
            foreach (var f in rf.Fonts)
            {
                var desc = new FontDescription() { FontName = f.Name, FontSize = f.Size };
                infocardFonts.Add(f.Index, desc);
            }
            _loaded = true;
        }
        public void LoadFontsFromIni(FreelancerIni fl, FileSystem fs)
        {
            var rf = new RichFontsIni();
            foreach (var file in fl.RichFontPaths) {
                rf.AddRichFontsIni(file, fs);
            }
            var fn = new FontsIni();
            foreach(var file in fl.FontPaths) {
                fn.AddFontsIni(file, fs);
            }
            LoadFonts(fn, rf, fs, fl.DataPath);
        }
        public void LoadFontsFromGameData(GameDataManager gd)
        {
            LoadFonts(gd.Ini.Fonts, gd.Ini.RichFonts, gd.VFS, gd.Ini.Freelancer.DataPath);
        }

        public FontDescription GetInfocardFont (int index)
        {
            FontDescription desc;
            if (!infocardFonts.TryGetValue(index, out desc))
                return infocardFonts[-1];
            return desc;
        }
    }
}
