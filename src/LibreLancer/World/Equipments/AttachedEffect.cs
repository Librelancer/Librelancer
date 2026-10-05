using LibreLancer.Data.GameData.Items;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.Sounds;

namespace LibreLancer.World.Equipments;

public class AttachedEffect : EquipmentObject<EffectEquipment>
{
    private AttachedSound? sfx;
    private ParticleEffectRenderer? pfx;

    public AttachedEffect(
        GameObject parent,
        Hardpoint? attachment,
        EffectEquipment equipment,
        EquipmentType type,
        ResourceManager resources,
        SoundManager? sounds)
        : base(parent, attachment, equipment, type,
            type == EquipmentType.LocalPlayer || type == EquipmentType.RemoteObject,
            type == EquipmentType.LocalPlayer || type == EquipmentType.RemoteObject)
    {
        // This equipment is only active on the client in space
        if (type == EquipmentType.Cutscene || type == EquipmentType.Server)
            return;
        // Init
        var audio = equipment.Effect?.Sound;
        if (audio != null && sounds != null)
        {
            sfx = new(sounds, audio.Nickname);
        }
        var particles = equipment.Effect?.GetEffect(resources);
        if (particles != null)
        {
            pfx = new ParticleEffectRenderer(particles);
        }
    }

    public override void OnUpdate(double delta, GameWorld world)
    {
        if (sfx != null)
        {
            var tr = GetTransform();
            sfx.Position = tr.Position;
            sfx.PlayIfInactive(true);
        }
    }

    public override void RenderUpdate(double delta)
    {
        if (pfx != null)
        {
            var tr = GetTransform();
            var mat = tr.Matrix();
            pfx.Update(delta, tr.Position, mat);
        }
    }

    public override void PrepareRender(ICamera camera, NebulaRenderer? nr, SystemRenderer sys, bool parentCull)
    {
        pfx?.PrepareRender(camera, nr, sys, parentCull);
    }

    public override void OnRemoved()
    {
        sfx?.Stop();
    }
}
