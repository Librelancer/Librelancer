﻿// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Exceptions;
using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
namespace LibreLancer
{
	public class GameConfig
	{
		public string FreelancerPath = "";
        public float MasterVolume = 1.0f;
        public float SfxVolume = 1.0f;
        public float MusicVolume = 1.0f;
        public bool IntroMovies = true;
		public int BufferWidth = 1024;
		public int BufferHeight = 768;
		public int MSAASamples = 0;
		public bool VSync = true;
		public Guid? UUID;

		//This default is to stop dlopen on linux from trying to open itself
		[XmlIgnore]
		public string MpvOverride = "__MPV_OVERRIDE_STRING";

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

		public static bool CheckFLDirectory(string dir)
        {
            if (!Directory.Exists(dir)) return false;
            bool exeExists = false;
            bool dataExists = false;
            foreach (var child in Directory.GetDirectories(dir).Select(Path.GetFileName))
            {
                if (child.Equals("exe", StringComparison.OrdinalIgnoreCase))
                    exeExists = true;
                if (child.Equals("data", StringComparison.OrdinalIgnoreCase))
                    dataExists = true;
            }
            return exeExists && dataExists;
        }

        public void Validate()
        {
            if (!LibreLancer.GameConfig.CheckFLDirectory(FreelancerPath))
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
                var xml = new XmlSerializer(typeof(GameConfig));
                using (var reader = new StreamReader(cfgpath))
                {
                    var loaded = (GameConfig)xml.Deserialize(reader);
                    loaded.filePath = (filePath ?? DefaultConfigPath);
                    if (loaded.UUID == null)
                        loaded.UUID = Guid.NewGuid();
                    return loaded;
                }
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
            var xml = new XmlSerializer(typeof(GameConfig));
			using (var writer = new StreamWriter(filePath()))
			{
				xml.Serialize(writer, this);
			}
		}

        public event Action<GameConfig> Saved;
        
		static string DefaultConfigPath()
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "librelancer.xml");
		}
	}
}

