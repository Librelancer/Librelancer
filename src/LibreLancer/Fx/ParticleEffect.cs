// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
namespace LibreLancer.Fx
{

    public abstract class NodeReference
    {
        public NodeReference Parent;
        public List<NodeReference> Children = new List<NodeReference>();


        private int flags = (1 << 1); //Enabled

        public bool IsAttachmentNode
        {
            get => MathHelper.GetFlag(flags, 0);
            set => MathHelper.SetFlag(ref flags, 0, value);
        }
        public bool Enabled
        {
            get => MathHelper.GetFlag(flags, 1);
            set => MathHelper.SetFlag(ref flags, 1, value);
        }
        public abstract FxNode Node { get; }
    }

    public class EmptyNodeReference : NodeReference
    {
        public override FxNode Node { get; }

        public EmptyNodeReference(FxNode n)
        {
            Node = n;
        }
    }

    public class EmitterReference : NodeReference
    {
        public FxEmitter Emitter;
        public override FxNode Node => Emitter;

        public int AppIdx = -1;
    }

    public class AppearanceReference : NodeReference
    {
        public FxAppearance Appearance;
        public override FxNode Node => Appearance;
    }

	public class ParticleEffect
    {
        public string Name;
        public uint CRC;

        // Tree for editor use
        public NodeReference[] Tree;
        // Emitters and appearances
        public EmitterReference[] Emitters;
        public AppearanceReference[] Appearances;
        // Calculated info
        public int[] ParticleCounts;
        public float Radius;

        void CalculateInfo()
        {
            ParticleCounts = new int[Appearances.Length];
            float radius = 0;
            foreach (var emitNode in Emitters)
            {
                var emitter = (FxEmitter) emitNode.Node;
                if(emitNode.AppIdx == -1) continue;
                var paired = Appearances[emitNode.AppIdx];
                int maxParticles = 500;
                if (emitter.MaxParticles != null)
                {
                    var max1 = (int) Math.Ceiling(emitter.MaxParticles.GetMax(false));
                    if (max1 < maxParticles)
                        maxParticles = max1;
                }
                if (emitter.Frequency != null &&
                    emitter.InitLifeSpan != null)
                {
                    var max2 = (int) (Math.Ceiling(emitter.Frequency.GetMax(false) *
                                                   emitter.InitLifeSpan.GetMax(false)));
                    if (max2 < maxParticles)
                        maxParticles = max2;
                }
                ParticleCounts[emitNode.AppIdx] += maxParticles;
                if (paired.Parent == null)
                {
                    radius = float.PositiveInfinity;
                }
                else if (float.IsFinite(radius))
                {
                    var r = emitter.Pressure.GetMax(true) * emitter.InitLifeSpan.GetMax(false);
                    r += emitter.GetMaxDistance(emitNode);
                    if (paired.Appearance is FxPerpAppearance perp)
                    {
                        r += perp.Size.GetMax(false);
                    }
                    else if (paired.Appearance is FLBeamAppearance)
                    {
                        //do nothing
                    }
                    else if (paired.Appearance is FxRectAppearance rect)
                    {
                        var w = rect.Width.GetMax(false);
                        var h = rect.Length.GetMax(false);
                        r += w > h ? w : h;
                    }
                    else if (paired.Appearance is FxOrientedAppearance orient)
                    {
                        var w = orient.Width?.GetMax(false) ?? 0f;
                        var h = orient.Height?.GetMax(false) ?? 0f;
                        r += Math.Max(w, h);
                    }
                    else if (paired.Appearance is FxBasicAppearance basic)
                    {
                        r += basic.Size.GetMax(false);
                    }
                    if (r > radius) radius = r;
                }
            }
            Radius = radius;
        }

		public ParticleEffect (
            uint crc,
            string name,
            EmitterReference[] emitters,
            AppearanceReference[] appearances,
            NodeReference[] tree
            )
        {
            CRC = crc;
            Name = name;
            Emitters = emitters;
            Appearances = appearances;
            Tree = tree;
            CalculateInfo();
        }

	}
}

