// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.GameData.Items;
using LibreLancer.Fx;
namespace LibreLancer
{
    public class UpdateSParamComponent : GameComponent
    {
        public UpdateSParamComponent(GameObject parent) : base(parent)
        {
        }
        public override void Update(TimeSpan time)
        {
            float sparam = 0;
            EngineComponent eng;
            if (Parent.Parent != null)
            {
                if ((eng = Parent.Parent.GetComponent<EngineComponent>()) != null)
                {
                    sparam = eng.Speed;
                }
            }
            ((ParticleEffectRenderer)Parent.RenderComponent).SParam = sparam;
        }
    }
}
