// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer.Utf.Mat
{
	public class TexFrameAnimation
	{
		public int TextureCount = 1;
		public int FrameCount = 0;
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
					case "texture count":
						TextureCount = leaf.Int32Data.Value;
						break;
					case "frame count":
						FrameCount = leaf.Int32Data.Value;
						break;
					case "fps":
						FPS = leaf.SingleData.Value;
						break;
					case "frame rects":
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
