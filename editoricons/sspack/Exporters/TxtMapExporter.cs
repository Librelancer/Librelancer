#region MIT License

/*
 * Copyright (c) 2009-2010 Nick Gravelyn (nick@gravelyn.com), Markus Ewald (cygon@nuclex.org)
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a 
 * copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, including without limitation 
 * the rights to use, copy, modify, merge, publish, distribute, sublicense, 
 * and/or sell copies of the Software, and to permit persons to whom the Software 
 * is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all 
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
 * PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
 * 
 */

#endregion

using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace sspack
{
	public class TxtMapExporter : IMapExporter
	{
		public string MapExtension
		{
			get { return "txt"; }
		}

		public void Save(string filename, Dictionary<string, Rectangle> map)
		{
			// copy the files list and sort alphabetically
			string[] keys = new string[map.Count];
			map.Keys.CopyTo(keys, 0);
			List<string> outputFiles = new List<string>(keys);
			outputFiles.Sort();

			using (StreamWriter writer = new StreamWriter(filename))
			{
				foreach (var image in outputFiles)
				{
					// get the destination rectangle
					Rectangle destination = map[image];

					// write out the destination rectangle for this bitmap
					writer.WriteLine(string.Format(
	                 	"{0} = {1} {2} {3} {4}", 
	                 	Path.GetFileNameWithoutExtension(image), 
	                 	destination.X, 
	                 	destination.Y, 
	                 	destination.Width, 
	                 	destination.Height));
				}
			}
		}
	}
}