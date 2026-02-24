// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Collections.Generic;
using LibreLancer.Data;
using LibreLancer.Resources;
using LibreLancer.Utf.Ale;

namespace LibreLancer.Fx
{
	public class ParticleLibrary
    {
        public GameItemCollection<ParticleEffect> Effects = new();
        public Dictionary<string, FxNode> Nodes = new(StringComparer.OrdinalIgnoreCase);
		public ResourceManager Resources;
        public string AlePath;
		public ParticleLibrary (ResourceManager res, AleFile ale)
		{
			Resources = res;
            AlePath = ale.Path;
            Dictionary<uint, FxNode> nodesByCrc = new();
            foreach (var n in ale.NodeLib.Nodes)
            {
                var conv = NodeFromAle(n);
                Nodes[conv.NodeName] = conv;
                nodesByCrc[conv.CRC] = conv;
            }
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
                        if(!nodesByCrc.TryGetValue(noderef.CRC, out node))
                        {
                            string errorNode = "_error_node_0";
                            int i = 1;
                            while (Nodes.ContainsKey(errorNode))
                                errorNode = $"_error_node_{i++}";
                            node = new FxNode(errorNode);
                            Nodes[errorNode] = node;
                            FLLog.Error("Fx", effect.Name + " bad node CRC 0x" + noderef.CRC.ToString("x"));
                        }
					}

                    NodeReference reference = NodeReference.Create(node);
                    if (reference is EmitterReference emit)
                    {
                        emitters.Add(emit);
                    }
                    else if (reference is AppearanceReference app)
                    {
                        appearances.Add(app);
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
                        er.Linked = ar;
                    }
                    if (n1 is AppearanceReference ap &&
                        n2 is FieldReference fp)
                    {
                        ap.Linked = fp;
                    }
				}
                Effects.Add(new ParticleEffect(
                    effect.CRC,
                    effect.Name,
                    emitters,
                    appearances,
                    nodesByIndex.Values.Where(x => x.Parent == null).ToList()
                    ));
			}
		}

        static FxNode NodeFromAle(AlchemyNode ale) => ale.ClassName switch
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
            _ => throw new ArgumentException(ale.ClassName)
        };

        private HashSet<uint> errored;
		public ParticleEffect FindEffect(uint crc)
		{
			if (Effects.Count == 1)
				return Effects.First(); //Work around buggy mods
            var fx = Effects.Get(crc);
            if (fx != null)
                return fx;
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
                var alfx = new ALEffect() { Name = fx.Nickname, CRC = fx.CRC };
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
                    // Init with 32768 for empty parent
                    if (nr.IsAttachmentNode)
                    {
                        alfx.Fx.Add(new AlchemyNodeRef(1, 0, 32768, tree[nr]));
                    }
                    else
                    {
                        AddNode(nr.Node);
                        alfx.Fx.Add(new AlchemyNodeRef(0, nr.Node.CRC, 32768, tree[nr]));
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
                    if (list[i] is EmitterReference er && er.Linked != null)
                    {
                        var self = tree[list[i]];
                        var other = tree[er.Linked];
                        alfx.Pairs.Add((self, other));
                    }
                    else if (list[i] is AppearanceReference ap && ap.Linked != null)
                    {
                        var self = tree[list[i]];
                        var other = tree[ap.Linked];
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

