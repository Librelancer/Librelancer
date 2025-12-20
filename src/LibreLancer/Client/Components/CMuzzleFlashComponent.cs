using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Physics;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.World;

namespace LibreLancer.Client.Components;

public class CMuzzleFlashComponent : GameComponent
{
    public GunEquipment Object;
    public List<ParticleEffectRenderer> Renderers = new List<ParticleEffectRenderer>();

    public CMuzzleFlashComponent(GameObject parent, GunEquipment gun) : base(parent)
    {
        Object = gun;
    }

    public void OnFired()
    {
        foreach (var fire in Renderers)
        {
            fire.Active = true;
            fire.Restart();
        }
    }

    public override void Register(PhysicsWorld physics)
    {
        if (Object.FlashEffect == null || GetResourceManager() == null) return;
        var pfx = Object.FlashEffect.GetEffect(GetResourceManager());
        if (pfx == null) return;

        var hpfires = Parent.GetHardpoints()
            .Where((x) => x.Name.StartsWith("hpfire", StringComparison.CurrentCultureIgnoreCase)).ToArray();
        foreach (var fire in hpfires)
        {
            var pr = new ParticleEffectRenderer(pfx);
            pr.Active = false;
            pr.Attachment = fire;
            Parent.ExtraRenderers.Add(pr);
        }

    }

    public override void Unregister(Physics.PhysicsWorld physics)
    {
        for (int i = 0; i < Renderers.Count; i++)
            Parent.ExtraRenderers.Remove(Renderers[i]);
    }
}
