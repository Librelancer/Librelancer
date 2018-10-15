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
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
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
		ConcurrentQueue<SaveCommand> toSave = new ConcurrentQueue<SaveCommand>();
		int index = 0;
		FreelancerGame g;
		string screenshotdir;
		List<string> names = new List<string>();
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
		public void Save(string filename, int width, int height, byte[] data)
		{
			toSave.Enqueue(new SaveCommand() { Data = data, Filename = filename, Width = width, Height = height });
		}
		void SaveThread()
		{
			while (Running)
			{
				while (toSave.Count > 0)
				{
					SaveCommand s;
					if (toSave.TryDequeue(out s))
					{
						ImageLib.PNG.Save(s.Filename, s.Width, s.Height, s.Data);
					}
				}
				Thread.Sleep(100);
			}
		}
		public void Stop()
		{
			Running = false;
		}
		class SaveCommand
		{
			public int Width;
			public int Height;
			public string Filename;
			public byte[] Data;
		}
	}
}
