// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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
                        var nd = ale.NodeLib.Nodes.FirstOrDefault((arg) => arg.CRC == noderef.CRC);
                        if(nd == null)
                        {
                            node = new FxNode("error node", "error node");
                            FLLog.Error("Fx", fx.Name + " bad node CRC 0x" + noderef.CRC.ToString("x"));
                        }
                        else
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

