// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data;

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

        public static NodeReference Create(FxNode node) => node switch
        {
            FxAppearance app => new AppearanceReference(app),
            FxEmitter em => new EmitterReference(em),
            FxField fld => new FieldReference(fld),
            _ => new EmptyNodeReference(node)
        };

    }

    public class EmptyNodeReference : NodeReference
    {
        public override FxNode Node { get; }

        public EmptyNodeReference(FxNode n)
        {
            Node = n;
        }
    }

    public class EmitterReference(FxEmitter emitter) : NodeReference
    {
        public FxEmitter Emitter = emitter;
        public override FxNode Node => Emitter;

        public AppearanceReference Linked;
        public int AppBufIdx; // Index of the particle buffer of the linked appearance
    }

    public class AppearanceReference(FxAppearance app) : NodeReference
    {
        public FxAppearance Appearance = app;
        public override FxNode Node => Appearance;

        public FieldReference Linked;
    }

    public class FieldReference(FxField field) : NodeReference
    {
        public FxField Field = field;
        public override FxNode Node => Field;
    }

	public class ParticleEffect : IdentifiableItem
    {
        // Tree for editor use
        public List<NodeReference> Tree;
        // Emitters and appearances
        public List<EmitterReference> Emitters;
        public List<AppearanceReference> Appearances;
        // Calculated info
        public int[] ParticleCounts;
        public float Radius;

        public void CalculateInfo()
        {
            ParticleCounts = new int[Appearances.Count];
            float radius = 0;
            foreach (var emitNode in Emitters)
            {
                var emitter = (FxEmitter) emitNode.Node;
                if(emitNode.Linked == null) continue;
                emitNode.AppBufIdx = Appearances.IndexOf(emitNode.Linked);
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

                ParticleCounts[emitNode.AppBufIdx] += maxParticles;
                if (emitNode.Linked.Parent == null)
                {
                    radius = float.PositiveInfinity;
                }
                else if (float.IsFinite(radius))
                {
                    var r = emitter.Pressure.GetMax(true) * emitter.InitLifeSpan.GetMax(false);
                    r += emitter.GetMaxDistance(emitNode);
                    if (emitNode.Linked.Appearance is FxPerpAppearance perp)
                    {
                        r += perp.Size.GetMax(false);
                    }
                    else if (emitNode.Linked.Appearance is FLBeamAppearance)
                    {
                        //do nothing
                    }
                    else if (emitNode.Linked.Appearance is FxRectAppearance rect)
                    {
                        var w = rect.Width.GetMax(false);
                        var h = rect.Length.GetMax(false);
                        r += w > h ? w : h;
                    }
                    else if (emitNode.Linked.Appearance is FxOrientedAppearance orient)
                    {
                        var w = orient.Width?.GetMax(false) ?? 0f;
                        var h = orient.Height?.GetMax(false) ?? 0f;
                        r += Math.Max(w, h);
                    }
                    else if (emitNode.Linked.Appearance is FxBasicAppearance basic)
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
            string nickname,
            List<EmitterReference> emitters,
            List<AppearanceReference> appearances,
            List<NodeReference> tree
            )
        {
            CRC = crc;
            Nickname = nickname;
            Emitters = emitters;
            Appearances = appearances;
            Tree = tree;
            CalculateInfo();
        }

	}
}

