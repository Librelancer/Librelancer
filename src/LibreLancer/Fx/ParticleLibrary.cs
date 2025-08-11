// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Collections.Generic;
using LibreLancer.Resources;
using LibreLancer.Utf.Ale;

namespace LibreLancer.Fx
{
	public class ParticleLibrary
	{
		public List<ParticleEffect> Effects = new List<ParticleEffect>();
		public ResourceManager Resources;
        public string AlePath;
		public ParticleLibrary (ResourceManager res, AleFile ale)
		{
			Resources = res;
            AlePath = ale.Path;
			foreach (var effect in ale.FxLib.Effects) {

				Dictionary<uint, NodeReference> nodesByIndex = new Dictionary<uint, NodeReference>();
                List<EmitterReference> emitters = new List<EmitterReference>();
                List<AppearanceReference> appearances = new List<AppearanceReference>();
                List<FieldReference> fields = new List<FieldReference>();
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
                    else if (node is FxField field)
                    {
                        var fr = new FieldReference() { Field = field };
                        fields.Add(fr);
                        reference = fr;
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
                    if (n1 is AppearanceReference ap &&
                        n2 is FieldReference fp)
                    {
                        ap.FieldIdx = fields.IndexOf(fp);
                    }
				}
                Effects.Add(new ParticleEffect(
                    effect.CRC,
                    effect.Name,
                    emitters.ToArray(),
                    appearances.ToArray(),
                    fields.ToArray(),
                    nodesByIndex.Values.Where(x => x.Parent == null).ToArray()
                    ));
			}
		}

        static FxNode NodeFromAle(AlchemyNode ale) => ale.Name switch
        {
            "FxNode" => new FxNode(ale),
            "FLBeamAppearance" => new FLBeamAppearance(ale),
            "FLDustAppearance" => new FLDustAppearance(ale),
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

        private HashSet<uint> errored;
		public ParticleEffect FindEffect(uint crc)
		{
			if (Effects.Count == 1)
				return Effects[0]; //Work around buggy mods
            for (int i = 0; i < Effects.Count; i++)
            {
                if (Effects[i].CRC == crc)
                {
                    return Effects[i];
                }
            }
            errored ??= new HashSet<uint>();
            if (!errored.Contains(crc))
            {
                int crcInt = unchecked((int)crc);
                FLLog.Error("Fx", $"Unable to find fx crc {crcInt} in {AlePath ?? "(null)"}");
                errored.Add(crc);
            }
            return null;
        }


        public (ALEffectLib, AlchemyNodeLibrary) Serialize()
        {
            Dictionary<FxNode, AlchemyNode> nodes = new();
            List<ALEffect> effects = new();

            void AddNode(FxNode fx)
            {
                if (!nodes.ContainsKey(fx))
                    nodes[fx] = fx.SerializeNode();
            }

            foreach (var fx in Effects)
            {
                var alfx = new ALEffect() { Name = fx.Name, CRC = fx.CRC };
                List<NodeReference> list = new();
                Dictionary<NodeReference, uint> tree = new();
                void BuildTree(NodeReference r)
                {
                    tree[r] = (uint)(list.Count + 1);
                    list.Add(r);
                    foreach (var c in r.Children)
                        BuildTree(c);
                }
                foreach (var n in fx.Tree)
                    BuildTree(n);
                foreach (var nr in list)
                {
                    if (nr.IsAttachmentNode)
                    {
                        alfx.Fx.Add(new AlchemyNodeRef(1, 0, 0, tree[nr]));
                    }
                    else
                    {
                        AddNode(nr.Node);
                        alfx.Fx.Add(new AlchemyNodeRef(0, nr.Node.CRC, 0, tree[nr]));
                    }
                }
                void AssignChildren(NodeReference r)
                {
                    var self = tree[r];
                    foreach (var c in r.Children)
                    {
                        alfx.Fx[(int)(tree[c] - 1)].Parent = self;
                        AssignChildren(c);
                    }
                }
                foreach (var n in fx.Tree)
                    AssignChildren(n);
                // Assign pairs
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] is EmitterReference er && er.AppIdx != -1)
                    {
                        var self = tree[list[i]];
                        var other = tree[fx.Appearances[er.AppIdx]];
                        alfx.Pairs.Add((self, other));
                    }
                    else if (list[i] is AppearanceReference ap && ap.FieldIdx != -1)
                    {
                        var self = tree[list[i]];
                        var other = tree[fx.Appearances[ap.FieldIdx]];
                        alfx.Pairs.Add((self, other));
                    }
                }
                effects.Add(alfx);
            }


            var nodelib = new AlchemyNodeLibrary();
            nodelib.Nodes = nodes.Values.ToList();
            var allib = new ALEffectLib();
            allib.Effects = effects;
            return (allib, nodelib);
        }
	}
}

