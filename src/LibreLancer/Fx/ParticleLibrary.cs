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
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Linq;
using System.Collections.Generic;
using LibreLancer.Utf.Ale;
namespace LibreLancer.Fx
{
	public class ParticleLibrary
	{
		public List<ParticleEffect> Effects = new List<ParticleEffect>();
		public ResourceManager Resources;
		public ParticleLibrary (ResourceManager res, AleFile ale)
		{
			Resources = res;
			foreach (var effect in ale.FxLib.Effects) {
				var fx = new ParticleEffect (this);
				fx.CRC = effect.CRC;
				fx.Name = effect.Name;
				Dictionary<uint, NodeReference> nodesByIndex = new Dictionary<uint, NodeReference>();
				foreach (var noderef in effect.Fx)
				{
					FxNode node = null;
					if (!noderef.IsAttachmentNode)
					{
						node = NodeFromAle(ale.NodeLib.Nodes.Where((arg) => arg.CRC == noderef.CRC).First()); 
					}
					var reference = new NodeReference();
					reference.Node = node;
					reference.IsAttachmentNode = noderef.IsAttachmentNode;
					nodesByIndex.Add(noderef.Index, reference);
				}
				foreach (var noderef in effect.Fx)
				{
					var nd = nodesByIndex[noderef.Index];
					if (noderef.Parent != 32768)
					{
						var parent = nodesByIndex[noderef.Parent];
						parent.Children.Add(nd);
						nd.Parent = parent;
					}
				}
				foreach (var pair in effect.Pairs)
				{
					var n1 = nodesByIndex[pair.Item1];
					var n2 = nodesByIndex[pair.Item2];
					n1.Paired.Add(n2);
				}
				fx.References = new List<NodeReference>(nodesByIndex.Values);
				Effects.Add(fx);
			}
		}
		static FxNode NodeFromAle(AlchemyNode ale)
		{
			var type = typeof(ParticleLibrary).Assembly.GetType("LibreLancer.Fx." + ale.Name);
			if (type != null)
			{
				return (FxNode)Activator.CreateInstance(type, ale);
			}
			else {
				throw new NotImplementedException(ale.Name);
			}
		}
		public ParticleEffect FindEffect(uint crc)
		{
			if (Effects.Count == 1)
				return Effects[0]; //Work around buggy mods
			var result = from ParticleEffect e in Effects where e.CRC == crc select e;
			if (result.Count() == 1)
				return result.First();
			throw new Exception();
		}
	}
}

