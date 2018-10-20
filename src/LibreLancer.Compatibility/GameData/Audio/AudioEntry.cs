// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer.Compatibility.GameData.Audio
{
	public class AudioEntry
	{
		public string Nickname;
		public string File;
		public AudioType Type;
		public int CrvPitch;
		public int Attenuation;
		public bool Is2d = false;
		public AudioEntry()
		{
		}
	}
}

