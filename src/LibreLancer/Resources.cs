// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Reflection;
namespace LibreLancer
{
	public class Resources
	{
		public static string LoadString(string name)
		{
			using (var stream = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(name)))
			{
				return stream.ReadToEnd();
			}
		}
	}
}
