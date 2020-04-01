// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer.Utf.Mat
{
	public class TexFrameAnimation
	{
		public int TextureCount = 1;
		public int FrameCount = 0;
		public float FPS;
		public TexFrame[] Frames;
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
						if (floats.Length % 5 != 0)
							throw new Exception("Incorrect frame data for Texture Animation");
						Frames = new TexFrame[floats.Length / 5];
						for (int i = 0; i < Frames.Length; i++)
						{
							Frames[i] = new TexFrame(
								(int)floats[i * 5],
								floats[i * 5 + 1],
								floats[i * 5 + 2],
								floats[i * 5 + 3],
                                floats[i * 5 + 4]
							);
						}
						break;
				}
			}
		}
	}

    public struct TexFrame
    {
        public int TextureIndex;
        public Vector2 UV1;
        public Vector2 UV2;
        public TexFrame(int idx, float u1, float v1, float u2, float v2)
        {
            TextureIndex = idx;
            UV1 = new Vector2(u1, v1);
            UV2 = new Vector2(u2, v2);
        }
    }
}
