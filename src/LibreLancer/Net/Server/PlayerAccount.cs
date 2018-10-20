// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
namespace LibreLancer
{
	public class PlayerAccount
	{
		public int ID;
		public Guid GUID;
		public DateTime Registered;
		public DateTime LastVisit;
		public string Email;
        public List<ListedCharacter> Characters = new List<ListedCharacter>();
		public PlayerAccount()
		{
		}
	}
}
