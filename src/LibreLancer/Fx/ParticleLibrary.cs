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
        public GameItemCollection<ParticleEffect> Effects = [];
        public Dictionary<string, FxNode> Nodes = new(StringComparer.OrdinalIgnoreCase);
        public ResourceManager Resources;
        public string AlePath;
        private readonly HashSet<uint> errored = [];

        public ParticleLibrary(ResourceManager res, AleFile ale)
        {
            Resources = res;
            AlePath = ale.Path;
            Dictionary<uint, FxNode> nodesByCrc = new();

            foreach (var conv in ale.NodeLib.Nodes.Select(NodeFromAle))
            {
                Nodes[conv.NodeName] = conv;
                nodesByCrc[conv.CRC] = conv;
            }

            foreach (var effect in ale.FxLib.Effects)
            {
                Dictionary<uint, NodeReference> nodesByIndex = [];
                List<EmitterReference> emitters = [];
                List<AppearanceReference> appearances = [];
                List<FieldReference> fields = [];

                foreach (var nodeRef in effect.Fx)
                {
                    FxNode? node = null;

                    if (!nodeRef.IsAttachmentNode)
                    {
                        if (!nodesByCrc.TryGetValue(nodeRef.CRC, out node))
                        {
                            var errorNode = "_error_node_0";
                            var i = 1;

                            while (Nodes.ContainsKey(errorNode))
                            {
                                errorNode = $"_error_node_{i++}";
                            }

                            node = new FxNode(errorNode);
                            Nodes[errorNode] = node;
                            FLLog.Error("Fx", effect.Name + " bad node CRC 0x" + nodeRef.CRC.ToString("x"));
                        }
                    }

                    NodeReference reference = NodeReference.Create(node!);

                    switch (reference)
                    {
                        case EmitterReference emit:
                            emitters.Add(emit);
                            break;
                        case AppearanceReference app:
                            appearances.Add(app);
                            break;
                    }

                    reference.IsAttachmentNode = nodeRef.IsAttachmentNode;
                    nodesByIndex.Add(nodeRef.Index, reference);
                }

                foreach (var nodeRef in effect.Fx)
                {
                    var nd = nodesByIndex[nodeRef.Index];

                    if (nodeRef.Parent == 32768)
                    {
                        continue;
                    }

                    var parent = nodesByIndex[nodeRef.Parent];
                    parent.Children.Add(nd);
                    nd.Parent = parent;
                }

                foreach (var pair in effect.Pairs)
                {
                    var n1 = nodesByIndex[pair.Source];
                    var n2 = nodesByIndex[pair.Target];

                    switch (n1)
                    {
                        case EmitterReference er when n2 is AppearanceReference ar:
                            er.Linked = ar;
                            break;
                        case AppearanceReference ap when n2 is FieldReference fp:
                            ap.Linked = fp;
                            break;
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

        private static FxNode NodeFromAle(AlchemyNode ale) => ale.ClassName switch
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

        public ParticleEffect? FindEffect(uint crc)
        {
            if (Effects.Count == 1)
                return Effects.First(); // Work around buggy mods
            var fx = Effects.Get(crc);
            if (fx != null)
                return fx;

            if (!errored.Contains(crc))
            {
                var crcInt = unchecked((int) crc);
                FLLog.Error("Fx", $"Unable to find fx crc {crcInt} in {AlePath ?? "(null)"}");
                errored.Add(crc);
            }

            return null;
        }

        public (ALEffectLib, AlchemyNodeLibrary) Serialize()
        {
            Dictionary<FxNode, AlchemyNode> nodes = new();
            List<ALEffect> effects = [];

            foreach (var fx in Effects)
            {
                var alfx = new ALEffect(fx.Nickname) { CRC = fx.CRC };
                List<NodeReference> list = [];
                Dictionary<NodeReference, uint> tree = new();

                foreach (var n in fx.Tree)
                {
                    BuildTree(n);
                }

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

                foreach (var n in fx.Tree)
                {
                    AssignChildren(n);
                }

                // Assign pairs
                foreach (var nodeReference in list)
                {
                    switch (nodeReference)
                    {
                        case EmitterReference er when er.Linked != null:
                        {
                            var self = tree[nodeReference];
                            var other = tree[er.Linked];
                            alfx.Pairs.Add((self, other));
                            break;
                        }
                        case AppearanceReference ap when ap.Linked != null:
                        {
                            var self = tree[nodeReference];
                            var other = tree[ap.Linked];
                            alfx.Pairs.Add((self, other));
                            break;
                        }
                    }
                }

                effects.Add(alfx);
                continue;

                void BuildTree(NodeReference r)
                {
                    tree[r] = (uint) (list.Count + 1);
                    list.Add(r);

                    foreach (var c in r.Children)
                    {
                        BuildTree(c);
                    }
                }

                void AssignChildren(NodeReference r)
                {
                    var self = tree[r];

                    foreach (var c in r.Children)
                    {
                        alfx.Fx[(int) (tree[c] - 1)].Parent = self;
                        AssignChildren(c);
                    }
                }
            }

            var nodelib = new AlchemyNodeLibrary
            {
                Nodes = nodes.Values.ToList()
            };

            var allib = new ALEffectLib
            {
                Effects = effects
            };

            return (allib, nodelib);

            void AddNode(FxNode fx)
            {
                if (!nodes.ContainsKey(fx))
                {
                    nodes[fx] = fx.SerializeNode();
                }
            }
        }
    }
}
