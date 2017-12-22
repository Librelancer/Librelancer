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
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
namespace LibreLancer
{
	public class GameConfig
	{
		public string FreelancerPath;
		public bool MuteMusic = false;
        public bool ForceAngle = false;
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
			if (!Directory.Exists(Path.Combine(dir, "EXE")) || 
			    !Directory.Exists(Path.Combine(dir, "DATA")))
			{
				return false;
			}
			return true;
		}

		public static GameConfig Create(bool loadFile = true, Func<string> filePath = null)
		{
			if (!loadFile) return new GameConfig((filePath ?? DefaultConfigPath));
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
			var cfg = new GameConfig((filePath ?? DefaultConfigPath));
			if (cfg.UUID == null)
				cfg.UUID = Guid.NewGuid();
			cfg.Save();
			return cfg;
		}

		public void Save()
		{
			var xml = new XmlSerializer(typeof(GameConfig));
			using (var writer = new StreamWriter(filePath()))
			{
				xml.Serialize(writer, this);
			}
		}

		static string DefaultConfigPath()
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "librelancer.xml");
		}

		[DllImport("kernel32.dll")]
		static extern bool SetDllDirectory (string directory);

		FreelancerGame game;

		public void Launch()
		{
			if (Platform.RunningOS == OS.Windows)
			{
				string bindir = Path.GetDirectoryName(typeof(GameConfig).Assembly.Location);
				var fullpath = Path.Combine(bindir, IntPtr.Size == 8 ? "x64" : "x86");
				SetDllDirectory(fullpath);
			}
			else
				ForceAngle = false;
			game = new FreelancerGame(this);
			game.Run ();
		}

		public void Crashed()
		{
			game.Crashed();
		}
	}
}

