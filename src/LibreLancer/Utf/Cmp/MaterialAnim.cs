// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer.Utf.Cmp
{
	public class MaterialAnim
	{
		public int MACount;
		public int MAFlags;
		public float[] MADeltas;

		public MaterialAnim(IntermediateNode node)
		{
			foreach (var child in node)
			{
				if (child is IntermediateNode)
					throw new Exception("Invalid node in MaterialAnim " + child.Name);
				var leaf = child as LeafNode;
				switch (leaf.Name.ToLowerInvariant())
				{
					case "macount":
						MACount = leaf.Int32ArrayData [0];
						break;
					case "maflags":
						MAFlags = leaf.Int32ArrayData[0];
						break;
					case "madeltas":
						MADeltas = leaf.SingleArrayData;
						break;
					case "makeys":
						//TODO: MAKeys
						break;
				}
			}
		}

		public float UOffset
		{
			get
			{
				return uOffset;
			}
		}
		public float VOffset
		{
			get
			{
				return vOffset;
			}
		}
		public float UScale
		{
			get
			{
				return uScale;
			}
		}
		public float VScale
		{
			get
			{
				return vScale;
			}
		}
		//5 floats per frame
		//time
		//u offset velocity
		//v offset velocity
		//u scale velocity
		//v scale velocity

		float uOffset = 0;
		float vOffset = 0;
		float uScale = 1;
		float vScale = 1;
		/// <summary>
		/// Update the MaterialAnim.
		/// </summary>
		/// <param name="totalTime">Total time (get from FreelancerGame).</param>
		public void Update(float totalTime)
		{
            uOffset = 0;
            vOffset = 0;
            uScale = 1;
            vScale = 1;
            for (int i = 0; i < MACount; i++)
            {
                int k = i * 5;
                float duration = MADeltas[k];
                float uVelocity = MADeltas[k +1];
                float vVelocity = MADeltas[k + 2];
                float uVelocityScale = MADeltas[k + 3];
                float vVelocityScale = MADeltas[k + 4];
                //loop from Beginning
                float t = totalTime;
                while (t > duration) t -= duration;
                //process anim
                uOffset += t * uVelocity;
                vOffset += t * vVelocity;
                uScale *= 1 + (t * uVelocityScale);
                vScale *= 1 + (t * vVelocityScale);
            }
        }
    }
}
