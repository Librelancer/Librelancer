// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
namespace LibreLancer.GameData
{
	public class Base
	{
        public string System;
		public BaseRoom StartRoom;
		public List<BaseRoom> Rooms = new List<BaseRoom>();
		public Base()
		{
		}
	}
}
