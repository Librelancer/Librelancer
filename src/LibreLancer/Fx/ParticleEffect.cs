// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
namespace LibreLancer.Fx
{
	public class ParticleEffect
	{
		ParticleLibrary lib;

		public string Name;
		public uint CRC;

		public List<NodeReference> References = new List<NodeReference>();
        public int EmitterCount;
        public int BeamCount;

        public float Radius;

        public void CalculateRadius()
        {
            float radius = 0;
            foreach (var emitNode in References.Where(x => x.Node is FxEmitter))
            {
                var emitter = (FxEmitter) emitNode.Node;
                if(emitNode.Paired.Count == 0) continue;
                if (emitNode.Paired[0].Parent == null)
                {
                    radius = float.PositiveInfinity;
                    break;
                }
                else
                {
                    var r = emitter.Pressure.GetMax(true) * emitter.InitLifeSpan.GetMax(false);
                    r += emitter.GetMaxDistance(emitNode);
                    if (emitNode.Paired[0].Node is FxPerpAppearance perp)
                    {
                        r += perp.Size.GetMax(false);
                    } 
                    else if (emitNode.Paired[0].Node is FLBeamAppearance)
                    {
                        //do nothing
                    }
                    else if (emitNode.Paired[0].Node is FxRectAppearance rect)
                    {
                        var w = rect.Width.GetMax(false);
                        var h = rect.Length.GetMax(false);
                        r += w > h ? w : h;
                    }
                    else if (emitNode.Paired[0].Node is FxBasicAppearance basic)
                    {
                        r += basic.Size.GetMax(false);
                    }
                    if (r > radius) radius = r;
                }
            }
            Radius = radius;
        }

		public ParticleEffect (ParticleLibrary lib)
		{
			this.lib = lib;
		}

		public ResourceManager ResourceManager
		{	
			get
			{
				return lib.Resources;
			}
		}
	}
}

