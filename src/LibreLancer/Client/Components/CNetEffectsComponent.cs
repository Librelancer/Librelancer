// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using LibreLancer.Net.Protocol;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.World;

namespace LibreLancer.Client.Components
{
    public class CNetEffectsComponent : GameComponent
    {
        private SpawnedEffect[] effects = new SpawnedEffect[0];

        public CNetEffectsComponent(GameObject parent) : base(parent)
        {
        }

        private int renIndex = 0;

        private List<ParticleEffectRenderer> spawned = new List<ParticleEffectRenderer>();

        void Spawn(SpawnedEffect effect)
        {
            var fx = Parent.World.Renderer.Game.GetService<GameDataManager>().Items.Effects.Get(effect.Effect);
            if (fx == null) return;
            var pfx = fx.GetEffect(Parent.World.Renderer.ResourceManager);
            if (pfx == null) return;
            foreach (var fxhp in effect.Hardpoints)
            {
                var hp = Parent.GetHardpoint(fxhp);
                var fxobj = new ParticleEffectRenderer(pfx) {Index = renIndex++, Attachment = hp};
                Parent.ExtraRenderers.Add(fxobj);
                spawned.Add(fxobj);
            }
        }

        public void UpdateEffects(SpawnedEffect[] fx)
        {
            foreach (var f in fx)
            {
                bool found = false;
                foreach (var f2 in effects) {
                    if (f2.ID == f.ID)
                    {
                        found = true;
                        break;
                    }
                }
                if(!found) Spawn(f);
            }
            effects = fx;
        }
    }
}
