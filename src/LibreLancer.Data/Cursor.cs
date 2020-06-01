// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Ini;
namespace LibreLancer.Data
{
	public class Cursor
	{
        [Entry("nickname")]
		public string Nickname;
        [Entry("blend")]
		public float Blend; //TODO: What is this?
        [Entry("spin")]
		public float Spin = 0;
        [Entry("scale")]
		public float Scale = 1;
        [Entry("hotspot")]
		public Vector2 Hotspot = Vector2.Zero;
        [Entry("color")]
		public Color4 Color = Color4.White;

        [Entry("anim")]
        void HandleAnim(Entry e)
        {
            //TODO: Incomplete parse
            Shape = e[0].ToString();
        }
        public string Shape;
    }
}
