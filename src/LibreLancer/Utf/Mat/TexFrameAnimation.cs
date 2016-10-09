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
namespace LibreLancer.Utf.Mat
{
	public class TexFrameAnimation
	{
		public float FPS;
		public RectangleF[] Frames;
		public TexFrameAnimation(IntermediateNode node)
		{
			foreach (var child in node)
			{
				var leaf = child as LeafNode;
				if (leaf == null)
					throw new Exception("Texture Animation should not have child intermediate node");
				switch (leaf.Name.ToLowerInvariant())
				{
					case "fps":
						FPS = leaf.SingleData.Value;
						break;
					case "frames":
						var floats = leaf.SingleArrayData;
						if (floats.Length % 4 != 0)
							throw new Exception("Incorrect frame data for Texture Animation");
						Frames = new RectangleF[floats.Length / 4];
						for (int i = 0; i < Frames.Length; i++)
						{
							Frames[i] = new RectangleF(
								floats[i * 4],
								floats[i * 4 + 1],
								floats[i * 4 + 2],
								floats[i * 4 + 3]
							);
						}
						break;
				}
			}
		}
	}
}
