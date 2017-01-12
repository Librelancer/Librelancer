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
