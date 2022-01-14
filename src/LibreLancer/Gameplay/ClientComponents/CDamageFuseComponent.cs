// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Fuses;
using LibreLancer.GameData;
using LibreLancer.Physics;

namespace LibreLancer
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
            private List<AttachedEffect> effects = new List<AttachedEffect>();
            private int renIndex = 0;

            public DamageFuseRunner(FuseResources fuse, float threshold)
            {
                this.fuse = fuse;
                this.threshold = threshold;
                actions = new Queue<FuseAction>(fuse.Fuse.Actions.OrderBy(x => x.AtT));
            }
            
            public void Update(float health, double time, GameObject parent)
            {
                foreach(var fx in effects)
                    fx.Update(parent, time, 0);
                if (health > threshold)
                {
                    T = 0;
                    renIndex = 0;
                    if (ran)
                    {
                        foreach (var child in effects) {
                            parent.ExtraRenderers.Remove(child.Effect);
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
                                var fxobj = new AttachedEffect(hp,
                                    new ParticleEffectRenderer(pfx) {Index = renIndex++});
                                parent.ExtraRenderers.Add(fxobj.Effect);
                                effects.Add(fxobj);
                            }
                        }
                    }
                }
            }
        }

        private List<DamageFuseRunner> runners = new List<DamageFuseRunner>();
        public CDamageFuseComponent(GameObject parent, IEnumerable<DamageFuse> fuses) : base(parent)
        {
            foreach(var f in fuses)
                runners.Add(new DamageFuseRunner(f.Fuse, f.Threshold));
        }
        

        public override void Update(double time)
        {
            if (!Parent.TryGetComponent<SHealthComponent>(out var health))
                return;
            foreach(var runner in runners)
                runner.Update(health.CurrentHealth, time, Parent);
        }
    }
}