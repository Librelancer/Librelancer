// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Fuses;
using LibreLancer.GameData;
using LibreLancer.Render;
using LibreLancer.World;

namespace LibreLancer.Client.Components
{
    public class CDamageFuseComponent : GameComponent
    {
        class DamageFuseRunner
        {
            private FuseResources fuse;
            private float threshold;
            private bool ran = false;
            private Queue<FuseAction> actions;
            private double T = 0;
            private List<ParticleEffectRenderer> effects = new List<ParticleEffectRenderer>();
            private int renIndex = 0;

            public DamageFuseRunner(FuseResources fuse, float threshold)
            {
                this.fuse = fuse;
                this.threshold = threshold;
                actions = new Queue<FuseAction>(fuse.Fuse.Actions.OrderBy(x => x.AtT));
            }

            public void Update(float health, double time, GameObject parent)
            {
                if (health > threshold)
                {
                    T = 0;
                    renIndex = 0;
                    if (ran)
                    {
                        foreach (var child in effects) {
                            parent.ExtraRenderers.Remove(child);
                        }
                        effects.Clear();
                        actions = new Queue<FuseAction>(fuse.Fuse.Actions.OrderBy(x => x.AtT));
                    }
                    ran = false;
                }
                else
                {
                    ran = true;
                    T += time;
                    FuseAction act;
                    while (actions.Count > 0 && (act = actions.Peek()).AtT <= T)
                    {
                        actions.Dequeue();
                        if (act is FuseStartEffect)
                        {
                            var fxact = ((FuseStartEffect)act);
                            if(string.IsNullOrWhiteSpace(fxact.Effect)) continue;
                            if (!fuse.Fx.TryGetValue(fxact.Effect, out var fx)) continue;
                            if (fx == null) continue;
                            var pfx = fx.GetEffect(parent.World.Renderer.ResourceManager);
                            if (pfx == null) continue;
                            foreach (var fxhp in fxact.Hardpoints)
                            {
                                var hp = parent.GetHardpoint(fxhp);
                                var fxobj = new ParticleEffectRenderer(pfx) {Index = renIndex++, Attachment = hp};
                                parent.ExtraRenderers.Add(fxobj);
                                effects.Add(fxobj);
                            }
                        }
                    }
                }
            }
        }

        private List<DamageFuseRunner> runners = new List<DamageFuseRunner>();
        public Explosion Explosion;
        public CDamageFuseComponent(GameObject parent, IEnumerable<DamageFuse> fuses, Explosion explosion) : base(parent)
        {
            foreach(var f in fuses)
                runners.Add(new DamageFuseRunner(f.Fuse, f.Threshold));
            Explosion = explosion;
        }


        public override void Update(double time)
        {
            if (!Parent.TryGetComponent<CHealthComponent>(out var health))
                return;
            foreach(var runner in runners)
                runner.Update(health.CurrentHealth, time, Parent);
        }
    }
}
