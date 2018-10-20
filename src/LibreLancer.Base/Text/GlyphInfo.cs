// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
	class GlyphInfo
	{
		public uint CharIndex;
		public Texture2D Texture;
		public Rectangle Rectangle;
		public bool Render; //Does this glyph have texture data? (space and tab do not.)
		public int AdvanceX;
		public int AdvanceY;
		public int HorizontalAdvance;
		public int XOffset;
		public int YOffset;
		public bool Kerning;
		//Big constructor!
		public GlyphInfo(
			Texture2D t, Rectangle r, int advanceX, 
			int advanceY, int horizontalAdvance, 
			int xoffset, int yoffset,  uint index, 
			bool kerning
		)
		{
			Texture = t;
			Rectangle = r;
			Render = true;
			AdvanceX = advanceX;
			AdvanceY = advanceY;
			HorizontalAdvance = horizontalAdvance;
			XOffset = xoffset;
			YOffset = yoffset;
			CharIndex = index;
			Kerning = kerning;
		}
		//Little constructor for space + tab
		public GlyphInfo(int advanceX, int advanceY, uint index, bool kerning)
		{
			Render = false;
			AdvanceX = advanceX;
			AdvanceY = advanceY;
			CharIndex = index;
			Kerning = kerning;
		}
	}
}

