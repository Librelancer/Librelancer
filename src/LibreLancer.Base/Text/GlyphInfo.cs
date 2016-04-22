/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
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

