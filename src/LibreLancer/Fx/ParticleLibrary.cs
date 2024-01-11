// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Collections.Generic;
using LibreLancer.Data.Effects;
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

				Dictionary<uint, NodeReference> nodesByIndex = new Dictionary<uint, NodeReference>();
                List<EmitterReference> emitters = new List<EmitterReference>();
                List<AppearanceReference> appearances = new List<AppearanceReference>();

				foreach (var noderef in effect.Fx)
				{
					FxNode node = null;
					if (!noderef.IsAttachmentNode)
					{
                        var nd = ale.NodeLib.Nodes.FirstOrDefault((arg) => arg.CRC == noderef.CRC);
                        if(nd == null)
                        {
                            node = new FxNode("error node", "error node");
                            FLLog.Error("Fx", effect.Name + " bad node CRC 0x" + noderef.CRC.ToString("x"));
                        }
                        else
                            node = NodeFromAle(ale.NodeLib.Nodes.Where((arg) => arg.CRC == noderef.CRC).First());
					}

                    NodeReference reference;
                    if (node is FxEmitter emit)
                    {
                        var er = new EmitterReference() {Emitter = emit};
                        emitters.Add(er);
                        reference = er;
                    }
                    else if (node is FxAppearance app)
                    {
                        var ar = new AppearanceReference() {Appearance = app};
                        appearances.Add(ar);
                        reference = ar;
                    }
                    else
                    {
                        reference = new EmptyNodeReference(node);
                    }
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
                    if (n1 is EmitterReference er &&
                        n2 is AppearanceReference ar)
                    {
                        er.AppIdx = appearances.IndexOf(ar);
                    }
				}
                Effects.Add(new ParticleEffect(
                    effect.CRC,
                    effect.Name,
                    emitters.ToArray(),
                    appearances.ToArray(),
                    nodesByIndex.Values.Where(x => x.Parent == null).ToArray()
                    ));
			}
		}

        static FxNode NodeFromAle(AlchemyNode ale) => ale.Name switch
        {
            "FxNode" => new FxNode(ale),
            "FLBeamAppearance" => new FLBeamAppearance(ale),
            "FxBasicAppearance" => new FxBasicAppearance(ale),
            "FxMeshAppearance" => new FxMeshAppearance(ale),
            "FxOrientedAppearance" => new FxOrientedAppearance(ale),
            "FxParticleAppearance" => new FxParticleAppearance(ale),
            "FxPerpAppearance" => new FxPerpAppearance(ale),
            "FxRectAppearance" => new FxRectAppearance(ale),
            "FxConeEmitter" => new FxConeEmitter(ale),
            "FxCubeEmitter" => new FxCubeEmitter(ale),
            "FxSphereEmitter" => new FxSphereEmitter(ale),
            "FLBeamField" => new FLBeamField(ale),
            "FLDustField" => new FLDustField(ale),
            "FxAirField" => new FxAirField(ale),
            "FxCollideField" => new FxCollideField(ale),
            "FxGravityField" => new FxGravityField(ale),
            "FxRadialField" => new FxRadialField(ale),
            "FxTurbulenceField" => new FxTurbulenceField(ale),
            _ => throw new ArgumentException(ale.Name)
        };

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

