// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Ini;
namespace LibreLancer.Compatibility.GameData
{
	public class Cursor
	{
		public string Nickname;
		public float Blend; //TODO: What is this?
		public float Spin = 0;
		public float Scale = 1;
		public Vector2 Hotspot = Vector2.Zero;
		public Color4 Color = Color4.White;
		public string Shape;
		public Cursor(Section s)
		{
			foreach (var e in s)
			{
				switch (e.Name.ToLowerInvariant())
				{
					case "nickname":
						Nickname = e[0].ToString();
						break;
					case "anim":
						Shape = e[0].ToString(); //TODO: mouse.ini cursor anim
						break;
					case "blend":
						Blend = e[0].ToSingle();
						break;
					case "spin":
						Spin = e[0].ToSingle();
						break;
					case "scale":
						Scale = e[0].ToSingle();
						break;
					case "hotspot":
						Hotspot = new Vector2(e[0].ToSingle(), e[1].ToSingle());
						break;
					case "color":
						Color = new Color4(e[0].ToInt32() / 255f, e[1].ToInt32() / 255f, e[2].ToInt32() / 255f, e[3].ToInt32() / 255f);
						break;
				}
			}
		}
	}
}
