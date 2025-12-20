// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Schema.Fuses;
using LibreLancer.Data.GameData;
using LibreLancer.Net.Protocol;
using LibreLancer.World;

namespace LibreLancer.Server.Components
{
    public class SFuseRunnerComponent : GameComponent
    {
        public List<SpawnedEffect> Effects = new List<SpawnedEffect>();
        public SFuseRunnerComponent(GameObject parent) : base(parent)
        {
        }
        class FuseInstance
        {
            public Queue<FuseAction> actions;
            public FuseResources Fuse;
            public double T = 0;
        }

        private List<FuseInstance> instances = new List<FuseInstance>();
        public void Run(FuseResources fuse)
        {
            var instance = new FuseInstance();
            instance.Fuse = fuse;
            instance.actions = new Queue<FuseAction>(fuse.Fuse.Actions.OrderBy(x => x.AtT));
            instance.T = 0;
            instances.Add(instance);
        }

        private BitArray128 runningHealthFuses = new BitArray128();
        public List<DamageFuse> DamageFuses = new List<DamageFuse>();

        public bool RunningDeathFuse => instances.Any(x => x.Fuse.Fuse.DeathFuse);

        public void RunAtHealth(float t)
        {
            for (int i = 0; i < DamageFuses.Count; i++) {
                if (t < DamageFuses[i].Threshold && !runningHealthFuses[i])
                {
                    runningHealthFuses[i] = true;
                    Run(DamageFuses[i].Fuse);
                    FLLog.Debug("Server", $"Running fuse {DamageFuses[i].Fuse.Fuse.Name}");
                }
            }
        }


        private uint fxID = 1;

        void Update(double time, FuseInstance instance)
        {
            instance.T += time / instance.Fuse.Fuse.Lifetime;
            FuseAction act;
            while(instance.actions.Count > 0 && (act = instance.actions.Peek()).AtT <= instance.T)
            {
                instance.actions.Dequeue();
                if (act is FuseStartEffect)
                {
                    var fxact = ((FuseStartEffect) act);
                    Effects.Add(new SpawnedEffect()
                    {
                        ID = fxID++, Effect = fxact.Effect,
                        Hardpoints = fxact.Hardpoints.ToArray(),
                    });
                    Parent.World.Server.EffectSpawned(Parent);
                }
                else if (act is FuseDestroyGroup)
                {
                    var dst = (FuseDestroyGroup)act;
                    if(dst.Fate == FusePartFate.disappear)
                    {
                        Parent.DisableCmpPart(dst.GroupName, GetResourceManager(), out _);
                    }
                    else if (dst.Fate == FusePartFate.debris)
                    {
                        Parent.SpawnDebris(dst.GroupName, GetResourceManager());
                    }
                }
                else if (act is FuseDestroyHpAttachment)
                {

                }
                else if (act is FuseImpulse)
                {

                }
                else if (act is FuseIgniteFuse ig)
                {
                    bool start = true;
                    foreach (var f in instances) {
                        if (f.Fuse.Fuse.Name.Equals(ig.Fuse, StringComparison.OrdinalIgnoreCase)) {
                            FLLog.Debug("Fuse", $"Fuse already running {ig.Fuse}");
                            start = false;
                            break;
                        }
                    }
                    if (start)
                    {
                        FLLog.Debug("Fuse", $"Igniting {ig.Fuse}");
                        Run(GetGameData().Items.Fuses.Get(ig.Fuse));
                    }
                }
                else if (act is FuseDestroyRoot)
                {
                    FLLog.Debug("Fuse", $"Killing {Parent}");
                    if (Parent.TryGetComponent<SDestroyableComponent>(out var destroy))
                    {
                        destroy.Destroy(true);
                    }
                }
            }
        }

        public override void Update(double time)
        {
            for (int i = 0; i < instances.Count; i++)
            {
                Update(time, instances[i]);
            }
        }
    }
}
