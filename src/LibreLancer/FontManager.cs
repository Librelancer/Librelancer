// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using LibreLancer.Data;
using LibreLancer.Data.IO;
using LibreLancer.Data.Schema;
using LibreLancer.Data.Schema.Fonts;
using LibreLancer.Graphics;

namespace LibreLancer
{
    public class FontDescription
    {
        public string FontName;
        public float FontSize;
    }

    public class FontManager
	{
        bool _loaded = false;
        Dictionary<int, FontDescription> infocardFonts = new Dictionary<int, FontDescription>();
        private Dictionary<string, string> nicknames;
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

        void LoadFonts(RenderContext context, FontsIni fonts, RichFontsIni rf, FileSystem fs, string dataPath)
        {
            foreach(var f in fonts.FontFiles)
            {
                if (fs.FileExists(dataPath + f))
                    context.Renderer2D.CreateRichTextEngine().AddTtfFile(f, fs.ReadAllBytes(dataPath + f));
                else
                    FLLog.Error("Fonts", "Could not find ttf file " + dataPath + f);
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
        public void LoadFontsFromIni(FreelancerIni fl, RenderContext render, FileSystem fs)
        {
            var rf = new RichFontsIni();
            foreach (var file in fl.RichFontPaths) {
                rf.AddRichFontsIni(file, fs);
            }
            var fn = new FontsIni();
            foreach(var file in fl.FontPaths) {
                fn.AddFontsIni(file, fs);
            }
            LoadFonts(render, fn, rf, fs, fl.DataPath);
        }
        public void LoadFontsFromGameData(RenderContext render, GameDataManager gd)
        {
            LoadFonts(render, gd.Items.Ini.Fonts, gd.Items.Ini.RichFonts, gd.VFS, gd.Items.Ini.Freelancer.DataPath);
        }

        public FontDescription GetInfocardFont (int index)
        {
            if (!_loaded) throw new InvalidOperationException("FontManager not initialized");
            FontDescription desc;
            if (!infocardFonts.TryGetValue(index, out desc))
                return infocardFonts[-1];
            return desc;
        }
    }
}
