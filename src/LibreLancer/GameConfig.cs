// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Exceptions;
using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using LibreLancer.Data.Ini;

namespace LibreLancer
{
    [ParsedSection]
	public partial class GameConfig
	{
        public GameSettings Settings = new();

        [Entry("freelancer_path")]
		public string FreelancerPath = "";
        [Entry("res_width")]
		public int BufferWidth = 1024;
        [Entry("res_height")]
		public int BufferHeight = 768;
        [Entry("uuid")]
		public Guid UUID = Guid.Empty;

        [XmlIgnore]
		public Func<FreelancerGame, GameState> CustomState;

		private GameConfig(Func<string> filePath)
		{
			this.filePath = filePath;
		}

		public GameConfig()
		{
		}

        private Func<string> filePath;

		public static bool CheckFLDirectory(string dir)
        {
            Data.IO.FileSystem fs;
            try
            {
                fs = Data.IO.FileSystem.FromPath(dir, true);
            }
            catch
            {
                return false;
            }
            return fs.FileExists("librelancer.ini") || fs.FileExists("EXE\\freelancer.ini");
        }

        public void Validate()
        {
            if (!CheckFLDirectory(FreelancerPath))
            {
                throw new InvalidFreelancerDirectory(FreelancerPath);
            }
        }

		public static GameConfig Create(bool loadFile = true, Func<string>? filePath = null)
		{
            if (!loadFile)
            {
                return new GameConfig((filePath ?? DefaultConfigPath));
            }

			var cfgpath = (filePath ?? DefaultConfigPath)();
            if (File.Exists(cfgpath))
            {
                var allS = IniFile.ParseFile(cfgpath, null, false, null).ToArray();
                var lS = allS.FirstOrDefault(x => x.Name == "Librelancer");
                var set = allS.FirstOrDefault(x => x.Name == "Settings");

                GameConfig? cfg;
                if (lS != null)
                {
                    TryParse(lS, out cfg);
                    cfg!.filePath = (filePath ?? DefaultConfigPath);
                }
                else
                {
                    cfg = new((filePath ?? DefaultConfigPath));
                }
                if (set != null)
                {
                    GameSettings.TryParse(set, out var settings);
                    cfg.Settings = settings ?? new();
                }

                if (cfg.UUID == Guid.Empty)
                {
                    cfg.UUID = Guid.NewGuid();
                }

                return cfg;
            }
            else
            {
                var cfg = new GameConfig((filePath ?? DefaultConfigPath));
                if (cfg.UUID == Guid.Empty)
                {
                    cfg.UUID = Guid.NewGuid();
                }

                cfg.Save();
                return cfg;
            }
		}

		public void Save()
        {
            Saved?.Invoke(this);

            using var writer = new StreamWriter(filePath());
            writer.WriteLine("[Librelancer]");
            writer.WriteLine($"freelancer_path = {FreelancerPath}");
            writer.WriteLine($"res_width = {BufferWidth}");
            writer.WriteLine($"res_height = {BufferHeight}");
            writer.WriteLine($"uuid = {UUID:D}");
            writer.WriteLine();
            Settings.Write(writer);
        }

        public event Action<GameConfig> Saved;

        private static string DefaultConfigPath()
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "librelancer.ini");
		}
	}
}

