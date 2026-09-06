using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Physics;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.World;

namespace LibreLancer.Client.Components;

public class CMuzzleFlashComponent : GameComponent
{
    public ResolvedFx? FlashEffect { get; }
    public List<ParticleEffectRenderer> Renderers { get; } = [];

    public CMuzzleFlashComponent(GameObject parent, GunEquipment gun) : base(parent)
    {
        FlashEffect = gun.FlashEffect;
    }

    public CMuzzleFlashComponent(GameObject parent, CountermeasureEquipment countermeasure) : base(parent)
    {
        FlashEffect = countermeasure.FlashEffect;
    }

    public void OnFired()
    {
        foreach (var fire in Renderers)
        {
            fire.Active = true;
            fire.Restart();
        }
    }

    public override void Register(GameWorld world)
    {
        var resManager = GetResourceManager(world);
        if (FlashEffect == null || resManager == null)
        {
            return;
        }

        var pfx = FlashEffect.GetEffect(resManager);
        if (pfx == null)
        {
            return;
        }

        var hpfires = Parent.GetHardpoints()
            .Where(x => x.Name.StartsWith("hpfire", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        foreach (var fire in hpfires)
        {
            var pr = new ParticleEffectRenderer(pfx)
            {
                Active = false,
                Attachment = fire
            };
            Parent.ExtraRenderers.Add(pr);
            Renderers.Add(pr);
        }

    }

    public override void Unregister(GameWorld world)
    {
        foreach (var renderer in Renderers)
        {
            Parent.ExtraRenderers.Remove(renderer);
        }
        Renderers.Clear();
    }
}
