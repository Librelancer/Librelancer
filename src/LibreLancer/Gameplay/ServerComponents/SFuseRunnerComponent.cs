// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Equipment;
using LibreLancer.GameData;
using LibreLancer.Data.Fuses;
namespace LibreLancer
{
    public class SFuseRunnerComponent : GameComponent
    {
        public FuseResources Fuse;
        public bool Running = false;
        public double T = 0;

        public List<SpawnedEffect> Effects = new List<SpawnedEffect>();
        public SFuseRunnerComponent(GameObject parent) : base(parent)
        {
        }

        Queue<FuseAction> actions;
        public void Run()
        {
            actions = new Queue<FuseAction>(Fuse.Fuse.Actions.OrderBy(x => x.AtT));
            Running = true;
            T = 0;
        }


        private uint fxID = 1;
        
        public override void Update(double time)
        {
            T += time;
            FuseAction act;
            while(actions.Count > 0 && (act = actions.Peek()).AtT <= T)
            {
                actions.Dequeue();
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
                else if (act is FuseIgniteFuse)
                {

                }
            }
        }
    }
}
