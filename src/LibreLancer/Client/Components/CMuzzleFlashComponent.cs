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
    public List<ParticleEffectRenderer> Renderers = [];

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

    public override void Register(PhysicsWorld? physics)
    {
        var resManager = GetResourceManager();
        if (Object.FlashEffect == null || resManager == null)
        {
            return;
        }

        var pfx = Object.FlashEffect.GetEffect(resManager);
        if (pfx == null)
        {
            return;
        }

        var hpfires = Parent.GetHardpoints().Where((x) => x.Name.StartsWith("hpfire", StringComparison.CurrentCultureIgnoreCase)).ToArray();
        foreach (var fire in hpfires)
        {
            var pr = new ParticleEffectRenderer(pfx)
            {
                Active = false,
                Attachment = fire
            };
            Parent.ExtraRenderers.Add(pr);
        }

    }

    public override void Unregister(PhysicsWorld? physics)
    {
        foreach (var renderer in Renderers)
        {
            Parent.ExtraRenderers.Remove(renderer);
        }
    }
}
