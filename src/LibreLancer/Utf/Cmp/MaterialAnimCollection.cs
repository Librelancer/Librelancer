// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
namespace LibreLancer.Utf.Cmp
{
	public class MaterialAnimCollection
	{
		public Dictionary<string, MaterialAnim> Anims = new Dictionary<string, MaterialAnim>(StringComparer.OrdinalIgnoreCase);
		List<MaterialAnim> updateList = new List<MaterialAnim>();
		public MaterialAnimCollection(IntermediateNode node)
		{
			foreach (var n in node)
			{
				if (n is IntermediateNode)
				{
					var anm = new MaterialAnim(n as IntermediateNode);
					Anims.Add(n.Name, anm);
					updateList.Add(anm);
				}
				else
				{
					throw new Exception("Invalid node in MaterialAnim " + node.Name);
				}
			}
		}

		public void Update(float totalTime)
		{
			for (int i = 0; i < updateList.Count; i++)
				updateList[i].Update(totalTime);
		}
	}
}
