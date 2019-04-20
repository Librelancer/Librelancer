// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.GameData;
using LibreLancer.Data.Fuses;
namespace LibreLancer
{
    public class FuseRunnerComponent : GameComponent
    {
        public FuseResources Fuse;
        public bool Running = false;
        public double T = 0;
        public FuseRunnerComponent(GameObject parent) : base(parent)
        {
        }

        Queue<FuseAction> actions;
        public void Run()
        {
            actions = new Queue<FuseAction>(Fuse.Fuse.Actions.OrderBy(x => x.AtT));
            Running = true;
            T = 0;
        }

        public override void Update(TimeSpan time)
        {
            T += time.TotalSeconds;
            FuseAction act;
            while(actions.Count > 0 && (act = actions.Peek()).AtT <= T)
            {
                actions.Dequeue();
                if (act is FuseStartEffect)
                {
                    var fxact = ((FuseStartEffect)act);
                    if (Fuse.Fx[fxact.Effect] == null) continue;
                    var hp = Parent.GetHardpoint(fxact.Hardpoint);
                    var fxobj = new GameObject()
                    {
                        Parent = Parent,
                        Attachment = hp,
                        RenderComponent = new ParticleEffectRenderer(Fuse.Fx[fxact.Effect])
                    };
                    Parent.ForceRenderCheck.Add(fxobj.RenderComponent);
                    Parent.Children.Add(fxobj);
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
                        var f = Parent.SpawnDebris(dst.GroupName);
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
