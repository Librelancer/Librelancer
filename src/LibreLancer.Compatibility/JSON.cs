// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and confiditons defined in
// LICENSE, which is part of this source code package

using System;
using Newtonsoft.Json;
namespace LibreLancer.Compatibility
{
	//Wrap around JsonConvert
	public class JSON
	{
		public static T Deserialize<T>(string str)
		{
			return JsonConvert.DeserializeObject<T>(str);
		}
		public static string Serialize<T>(T obj)
		{
			return JsonConvert.SerializeObject(obj, Formatting.Indented);
		}
	}
}
