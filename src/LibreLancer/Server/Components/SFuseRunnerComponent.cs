// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Fuses;
using LibreLancer.GameData;
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
                        Hardpoints = fxact.Hardpoints.ToArray()
                    });
                    Parent.World.Server.EffectSpawned(Parent);
                }
                else if (act is FuseDestroyGroup)
                {
                    var dst = (FuseDestroyGroup)act;
                    if(dst.Fate == FusePartFate.disappear)
                    {
                        Parent.DisableCmpPart(dst.GroupName);
                    } 
                    else if (dst.Fate == FusePartFate.debris)
                    {
                        Parent.SpawnDebris(dst.GroupName);
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
                    Run(instance.Fuse.GameData.GetFuse(ig.Fuse));
                }
                else if (act is FuseDestroyRoot)
                {
                    if (Parent.TryGetComponent<SNPCComponent>(out var npc))
                    {
                        npc.Killed();
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
