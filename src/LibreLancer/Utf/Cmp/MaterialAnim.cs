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
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
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
			if (MACount != 1) //TODO: implement multi-frame MaterialAnim
				return;

			float duration = MADeltas[0];
			float uVelocity = MADeltas[1];
			float vVelocity = MADeltas[2];
			float uVelocityScale = MADeltas[3];
			float vVelocityScale = MADeltas[4];

			float t = totalTime % duration;
			uOffset = t * uVelocity;
			vOffset = t * vVelocity;
			uScale = 1 + (t * uVelocityScale);
			vScale = 1 + (t * vVelocityScale);
		}


	}
}
