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
using System.Linq;
using System.Collections.Generic;
using LibreLancer.Utf.Ale;
namespace LibreLancer.Fx
{
	public class ParticleLibrary
	{
		public List<FxNode> Nodes = new List<FxNode> ();
		public List<ParticleEffect> Effects = new List<ParticleEffect>();
		public ResourceManager Resources;

		public ParticleLibrary (ResourceManager res, AleFile ale)
		{
			Resources = res;
			foreach (var node in ale.NodeLib.Nodes) {
				Nodes.Add (NodeFromAle (node));
			}
			foreach (var effect in ale.FxLib.Effects) {
				var fx = new ParticleEffect (this);
				var root = new FxRootNode();
				fx.CRC = effect.CRC;
				fx.Name = effect.Name;
				foreach (var noderef in effect.Fx)
				{
					if (noderef.IsAttachmentNode && FindNode(noderef.CRC) == null)
					{
						Nodes.Add(new FxNode("Attachment_0x" + noderef.CRC.ToString("X"), "Empty") { CRC = noderef.CRC });
					}
				}
				foreach (var noderef in effect.Fx) {
					var nd = FindNode(noderef.CRC);
					if (noderef.Parent != 32768)
					{
						fx.Parents.Add(nd, FindNode(effect.FindRef(noderef.Parent).CRC));
					}
					else
						fx.Parents.Add(nd, root);
					if (noderef.IsAttachmentNode)
						fx.AttachmentNodes.Add(nd);
				}
				foreach (var pair in effect.Pairs)
				{
					var n1 = FindNode(effect.FindRef(pair.Item1).CRC);
					List<Fx.FxNode> pairedTo;
					if (!fx.Pairs.TryGetValue(n1, out pairedTo)) {
						pairedTo = new List<FxNode>();
						fx.Pairs.Add(n1, pairedTo);
					}
					pairedTo.Add(FindNode(effect.FindRef(pair.Item2).CRC));
				}
				fx.SetNodes(effect.Fx.Select(arg => FindNode(arg.CRC)));
				Effects.Add(fx);
			}
		}
		FxNode FindNode(uint crc)
		{
			var result = from FxNode n in Nodes where n.CRC == crc select n;
			var c = result.Count();
			if (c == 1)
				return result.First();
			else if (c == 0)
				return null;
			else
				throw new Exception("ALE CRC collision");
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
			var result = from ParticleEffect e in Effects where e.CRC == crc select e;
			if (result.Count() == 1)
				return result.First();
			throw new Exception();
		}
	}
}

