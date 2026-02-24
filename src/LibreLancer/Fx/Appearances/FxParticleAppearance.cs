// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Utf.Ale;
namespace LibreLancer.Fx
{
	public class FxParticleAppearance : FxAppearance
    {
        public string LifeName = "";
        public string DeathName = "";
        public bool UseDynamicRotation;
        public bool SmoothRotation;
		public FxParticleAppearance (AlchemyNode ale) : base(ale)
        {
            LifeName = ale.GetString(AleProperty.ParticleApp_LifeName);
            DeathName = ale.GetString(AleProperty.ParticleApp_DeathName);
            UseDynamicRotation = ale.GetBoolean(AleProperty.ParticleApp_UseDynamicRotation);
            SmoothRotation = ale.GetBoolean(AleProperty.ParticleApp_SmoothRotation);
        }

        public FxParticleAppearance(string name) : base(name)
        {
        }

        public override AlchemyNode SerializeNode()
        {
            var n = base.SerializeNode();
            n.Parameters.Add(new(AleProperty.ParticleApp_LifeName, LifeName));
            n.Parameters.Add(new(AleProperty.ParticleApp_DeathName, DeathName));
            if(UseDynamicRotation)
                n.Parameters.Add(new(AleProperty.ParticleApp_UseDynamicRotation, UseDynamicRotation));
            if(SmoothRotation)
                n.Parameters.Add(new(AleProperty.ParticleApp_SmoothRotation, SmoothRotation));
            return n;
        }
    }
}

