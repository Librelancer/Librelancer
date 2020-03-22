// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer.GameData.Items
{
	public class Equipment
	{
		public Equipment()
		{
		}
        public string Nickname;
        public uint CRC;
        public float[] LODRanges;
        public string HPChild;
        public ResolvedModel ModelFile;
	}
}

