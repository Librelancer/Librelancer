using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.Sounds;

namespace LibreLancer.World.Equipments;

public class Thruster : EquipmentModelObject<ThrusterEquipment>
{
    public bool Enabled;

    private List<ParticleEffectRenderer> fireFx = [];
    private AttachedSound? thrustSound;

    public Thruster(
        GameObject parent,
        Hardpoint? attachment,
        ThrusterEquipment equipment,
        EquipmentType type,
        ResourceManager resources,
        SoundManager? sound) : base(parent, attachment, equipment, type, resources,
        type == EquipmentType.LocalPlayer || type == EquipmentType.RemoteObject)
    {
        if (type == EquipmentType.LocalPlayer || type == EquipmentType.RemoteObject)
        {
            var pfx = Equipment.Particles?.GetEffect(resources);
            if (pfx != null)
            {
                foreach (var hp in GetHardpoints()
                             .Where(x => x.Name.Equals(Equipment.HpParticles, StringComparison.OrdinalIgnoreCase)))
                {
                    fireFx.Add(new ParticleEffectRenderer(pfx) { Attachment = hp, Active = false, SParam = 1 });
                }
            }
            if (sound != null)
            {
                var soundName = FindThrusterSound(sound);
                if (soundName != null)
                {
                    sound.LoadSound(soundName);
                    thrustSound = new AttachedSound(sound, soundName);
                }
            }
        }
    }

    public override void OnUpdate(double delta, GameWorld world)
    {
        foreach (var renderer in fireFx)
        {
            renderer.Active = Enabled;
        }

        if (thrustSound == null)
        {
            return;
        }

        if (!Enabled)
        {
            thrustSound.Stop();
            return;
        }

        thrustSound.Position = Parent.Transform.Position;
        thrustSound.Velocity = Parent.PhysicsComponent?.Body?.LinearVelocity ?? Vector3.Zero;
        thrustSound.PlayIfInactive(true);
        thrustSound.Update();
    }

    public override void RenderUpdate(double delta)
    {
        base.RenderUpdate(delta);
        var tr = GetTransform();
        foreach (var renderer in fireFx)
        {
            renderer.Update(delta, tr);
        }
    }

    public override void PrepareRender(ICamera camera, NebulaRenderer? nr, SystemRenderer sys, bool parentCull)
    {
        base.PrepareRender(camera, nr, sys, parentCull);
        foreach (var renderer in fireFx)
        {
            renderer.PrepareRender(camera, nr, sys, parentCull);
        }
    }

    private string? FindThrusterSound(SoundManager sound)
    {
        var isPlayer = (Parent.Flags & GameObjectFlags.Player) == GameObjectFlags.Player;
        var soundNames = isPlayer
            ? new[] { "interior_thruster_sound", Equipment.Nickname, "exterior_thruster_sound" }
            : new[] { Equipment.Nickname, "exterior_thruster_sound" };

        foreach (var soundName in soundNames)
        {
            if (!string.IsNullOrWhiteSpace(soundName) && sound.GetEntry(soundName) != null)
            {
                return soundName;
            }
        }

        return null;
    }

    public override void OnRemoved()
    {
        thrustSound?.Stop();
    }
}
