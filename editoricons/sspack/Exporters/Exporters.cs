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
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;

namespace sspack
{
	public static class Exporters
	{
		private static List<IImageExporter> imageExporters = new List<IImageExporter>();
		private static List<IMapExporter> mapExporters = new List<IMapExporter>();

		public static ReadOnlyCollection<IImageExporter> ImageExporters { get; private set; }
		public static ReadOnlyCollection<IMapExporter> MapExporters { get; private set; }

		public static void Load() { /* invokes static constructor */ }

		static Exporters()
		{
			ImageExporters = new ReadOnlyCollection<IImageExporter>(imageExporters);
			MapExporters = new ReadOnlyCollection<IMapExporter>(mapExporters);

			// find built in exporters
			FindExporters(Assembly.GetExecutingAssembly());

			// find exporters in any DLLs in the directory with sspack.exe
			string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string[] dlls = Directory.GetFiles(dir, "*.dll", SearchOption.TopDirectoryOnly);
			foreach (string file in dlls)
			{
				try { FindExporters(Assembly.LoadFile(file)); }
				catch { /* don't care */ }
			}
		}

		private static void FindExporters(Assembly assembly)
		{
			foreach (Type type in assembly.GetTypes())
			{
				if (!type.IsAbstract && type.IsClass)
				{
					try
					{
						IImageExporter imageExporter = Activator.CreateInstance(type) as IImageExporter;
						if (imageExporter != null)
						{
							imageExporters.Add(imageExporter);
						}

						IMapExporter mapExporter = Activator.CreateInstance(type) as IMapExporter;
						if (mapExporter != null)
						{
							mapExporters.Add(mapExporter);
						}
					}
					catch { /* don't care */ }
				}
			}
		}
	}
}
