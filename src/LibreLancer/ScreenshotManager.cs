// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace LibreLancer
{
	public class ScreenshotManager
	{
        private FreelancerGame g;
        private string screenshotdir;
        private List<string> names = [];
		public ScreenshotManager(FreelancerGame game)
		{
            g = game;
			screenshotdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "FreelancerShots");
		}
		public void TakeScreenshot()
		{
			if (!Directory.Exists(screenshotdir))
				Directory.CreateDirectory(screenshotdir);
			var dt = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss", System.Globalization.CultureInfo.InvariantCulture);
			var name = Path.Combine(screenshotdir, "librelancer_" + dt);
			if (names.Contains(name))
			{
				name += " " + new Random().Next();
			}
			names.Add(name);
			g.Screenshot(Path.Combine(screenshotdir, name + ".png"));
		}

        public void Save(string filename, int width, int height, Bgra8[] data) => Task.Run(() =>
        {
            using var output = File.Create(filename);
            ImageLib.PNG.Save(output, width, height, data, true);
        });
	}
}
