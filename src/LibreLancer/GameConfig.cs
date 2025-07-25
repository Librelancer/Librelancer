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
        public GameSettings Settings = new GameSettings();

        [Entry("freelancer_path")]
		public string FreelancerPath = "";
        [Entry("res_width")]
		public int BufferWidth = 1024;
        [Entry("res_height")]
		public int BufferHeight = 768;
        [Entry("uuid")]
		public Guid? UUID;

        [XmlIgnore]
		public Func<FreelancerGame, GameState> CustomState;

		private GameConfig(Func<string> filePath)
		{
			this.filePath = filePath;
		}

		public GameConfig()
		{
		}

		Func<string> filePath;

        static bool FileExists(string dir, string f)
        {
            return Directory.GetFiles(dir).Any(x => Path.GetFileName(x).Equals(f, StringComparison.OrdinalIgnoreCase));
        }

		public static bool CheckFLDirectory(string dir)
        {
            if (!Directory.Exists(dir)) return false;
            bool exeExists = false;
            string exePath = null;
            foreach (var child in Directory.GetDirectories(dir)) {
                if (Path.GetFileName(child).Equals("exe", StringComparison.OrdinalIgnoreCase)) {
                    exePath = child;
                    break;
                }
            }
            if (FileExists(dir, "librelancer.ini")) return true;
            if (!string.IsNullOrEmpty(exePath) && FileExists(exePath, "freelancer.ini")) return true;
            return false;
        }

        public void Validate()
        {
            if (!CheckFLDirectory(FreelancerPath))
            {
                throw new InvalidFreelancerDirectory(FreelancerPath);
            }
        }

		public static GameConfig Create(bool loadFile = true, Func<string> filePath = null)
		{
            if (!loadFile)
            {
                return new GameConfig((filePath ?? DefaultConfigPath));
            }

			var cfgpath = (filePath ?? DefaultConfigPath)();
            if (File.Exists(cfgpath))
            {
                var allS = IniFile.ParseFile(cfgpath, null, false).ToArray();
                var lS = allS.FirstOrDefault(x => x.Name == "Librelancer");
                var set = allS.FirstOrDefault(x => x.Name == "Settings");
                GameConfig cfg;
                if (lS != null)
                {
                    TryParse(lS, out cfg);
                    cfg.filePath = (filePath ?? DefaultConfigPath);
                }
                else
                {
                    cfg = new((filePath ?? DefaultConfigPath));
                }
                if (set != null)
                {
                    GameSettings.TryParse(set, out cfg.Settings);
                    cfg.Settings ??= new();
                }
                if (cfg.UUID == null)
                    cfg.UUID = Guid.NewGuid();
                return cfg;
            }
            else
            {
                var cfg = new GameConfig((filePath ?? DefaultConfigPath));
                if (cfg.UUID == null)
                    cfg.UUID = Guid.NewGuid();
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
            writer.WriteLine($"uuid = {UUID.Value:D}");
            writer.WriteLine();
            Settings.Write(writer);
        }

        public event Action<GameConfig> Saved;

		static string DefaultConfigPath()
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "librelancer.ini");
		}
	}
}

