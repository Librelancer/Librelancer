// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Render;
using LibreLancer.World;

namespace LibreLancer.Client.Components
{
    public class CUpdateSParamComponent : GameComponent
    {
        public CUpdateSParamComponent(GameObject parent) : base(parent)
        {
        }
        public override void Update(double time)
        {
            if (Parent.RenderComponent == null) return;
            float sparam = 0;
            CEngineComponent eng;
            if (Parent.Parent != null)
            {
                if ((eng = Parent.Parent.GetComponent<CEngineComponent>()) != null)
                {
                    sparam = eng.Speed;
                }
            }
            ((ParticleEffectRenderer)Parent.RenderComponent).SParam = sparam;
        }
    }
}
