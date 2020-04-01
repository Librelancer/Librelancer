// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Numerics;
using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
	public abstract class NamedObject
	{
        [Entry("nickname")] 
        public string Nickname;
        [Entry("pos")] 
        public Vector3? Pos;
        [Entry("rotate")] 
        public Vector3? Rotate;
        public override string ToString()
		{
			return Nickname;
		}
	}
}
