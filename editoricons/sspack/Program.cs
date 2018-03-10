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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace sspack
{
	public enum FailCode
	{
		FailedParsingArguments = 1,
		ImageExporter,
		MapExporter,
		NoImages,
		ImageNameCollision,

		FailedToLoadImage,
		FailedToPackImage,
		FailedToCreateImage,
		FailedToSaveImage,
		FailedToSaveMap
	}

	public class Program
	{
		static int Main(string[] args)
		{
			return Launch(args);
		}

		public static int Launch(string[] args)
		{
			ProgramArguments arguments = ProgramArguments.Parse(args);

			if (arguments == null)
			{
				return (int)FailCode.FailedParsingArguments;
			}
			else
			{
				// make sure we have our list of exporters
				Exporters.Load();

				// try to find matching exporters
				IImageExporter imageExporter = null;
				IMapExporter mapExporter = null;

				string imageExtension = Path.GetExtension(arguments.image).Substring(1).ToLower();
				foreach (var exporter in Exporters.ImageExporters)
				{
					if (exporter.ImageExtension.ToLower() == imageExtension)
					{
						imageExporter = exporter;
						break;
					}
				}

				if (imageExporter == null)
				{
					Console.WriteLine("Failed to find exporters for specified image type.");
					return (int)FailCode.ImageExporter;
				}

				if (!string.IsNullOrEmpty(arguments.map))
				{
					string mapExtension = Path.GetExtension(arguments.map).Substring(1).ToLower();
					foreach (var exporter in Exporters.MapExporters)
					{
						if (exporter.MapExtension.ToLower() == mapExtension)
						{
							mapExporter = exporter;
							break;
						}
					}

					if (mapExporter == null)
					{
						Console.WriteLine("Failed to find exporters for specified map type.");
						return (int)FailCode.MapExporter;
					}
				}

				// compile a list of images
				List<string> images = new List<string>();
				FindImages(arguments, images);

				// make sure we found some images
				if (images.Count == 0)
				{
					Console.WriteLine("No images to pack.");
					return (int)FailCode.NoImages;
				}

				// make sure no images have the same name if we're building a map
				if (mapExporter != null)
				{
					for (int i = 0; i < images.Count; i++)
					{
						string str1 = Path.GetFileNameWithoutExtension(images[i]);

						for (int j = i + 1; j < images.Count; j++)
						{
							string str2 = Path.GetFileNameWithoutExtension(images[j]);

							if (str1 == str2)
							{
								Console.WriteLine("Two images have the same name: {0} = {1}", images[i], images[j]);
								return (int)FailCode.ImageNameCollision;
							}
						}
					}
				}

				// generate our output
				ImagePacker imagePacker = new ImagePacker();
				Bitmap outputImage;
				Dictionary<string, Rectangle> outputMap;

				// pack the image, generating a map only if desired
				int result = imagePacker.PackImage(images, arguments.pow2, arguments.sqr, arguments.mw, arguments.mh, arguments.pad, mapExporter != null, out outputImage, out outputMap);
				if (result != 0)
				{
					Console.WriteLine("There was an error making the image sheet.");
					return result;
				}

				// try to save using our exporters
				try 
				{
					if (File.Exists(arguments.image))
						File.Delete(arguments.image);
					imageExporter.Save(arguments.image, outputImage);
				}
				catch (Exception e)
				{
					Console.WriteLine("Error saving file: " + e.Message);
					return (int)FailCode.FailedToSaveImage;
				}
				
				if (mapExporter != null)
				{
					try
					{
						if (File.Exists(arguments.map))
							File.Delete(arguments.map); 
						mapExporter.Save(arguments.map, outputMap);
					}
					catch (Exception e)
					{
						Console.WriteLine("Error saving file: " + e.Message);
						return (int)FailCode.FailedToSaveMap;
					}
				}
			}

			return 0;
		}

		private static void FindImages(ProgramArguments arguments, List<string> images)
		{
			List<string> inputFiles = new List<string>();

			if (!string.IsNullOrEmpty(arguments.il))
			{
				using (StreamReader reader = new StreamReader(arguments.il))
				{
					while (!reader.EndOfStream)
					{
						inputFiles.Add(reader.ReadLine());
					}
				}
			}

			if (arguments.input != null)
			{
				inputFiles.AddRange(arguments.input);
			}

			foreach (var str in inputFiles)
			{
				if (MiscHelper.IsImageFile(str))
				{
					images.Add(str);
				}
			}
		}
	}
}
