// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
namespace LibreLancer
{
	public class ScreenshotManager
	{
		public bool Running = true;
        private ConcurrentQueue<SaveCommand> toSave = new();
        private int index = 0;
        private FreelancerGame g;
        private string screenshotdir;
        private List<string> names = [];
		public ScreenshotManager(FreelancerGame game)
		{
			Thread thr = new Thread(new ThreadStart(SaveThread));
            thr.Name = "ScreenshotSaver";
            thr.Start();
            g = game;
			screenshotdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "FreelancerShots");
		}
		public void TakeScreenshot()
		{
			index++;
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
		public void Save(string filename, int width, int height, Bgra8[] data)
		{
			toSave.Enqueue(new SaveCommand() { Data = data, Filename = filename, Width = width, Height = height });
		}

        private void SaveThread()
		{
			while (Running)
			{
				while (toSave.Count > 0)
				{
                    if (toSave.TryDequeue(out var s))
                    {
                        using var output = File.Create(s.Filename);
                        ImageLib.PNG.Save(output, s.Width, s.Height, s.Data, true);
                    }
				}
				Thread.Sleep(100);
			}
		}
		public void Stop()
		{
			Running = false;
		}

        private class SaveCommand
		{
			public int Width;
			public int Height;
			public string Filename;
			public Bgra8[] Data;
		}
	}
}
