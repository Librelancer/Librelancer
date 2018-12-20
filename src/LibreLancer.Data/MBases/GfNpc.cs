// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Ini;
namespace LibreLancer.Data
{
	public class GfNpc
	{
		public string Nickname;
		public string Body;
		public string Head;
		public string LeftHand;
		public string RightHand;

		public GfNpc(Section s)
		{
			foreach (var e in s)
			{
				switch (e.Name.ToLowerInvariant())
				{
					case "nickname":
						Nickname = e[0].ToString();
						break;
					case "body":
						Body = e[0].ToString();
						break;
					case "head":
						Head = e[0].ToString();
						break;
					case "lefthand":
						LeftHand = e[0].ToString();
						break;
					case "righthand":
						RightHand = e[0].ToString();
						break;
				}
			}
		}
	}
}
