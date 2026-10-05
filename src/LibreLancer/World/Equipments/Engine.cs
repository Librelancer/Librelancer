using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Fx;
using LibreLancer.Net.Protocol;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.Sounds;

namespace LibreLancer.World.Equipments;

public class Engine : EquipmentObject<EngineEquipment>
{
    public float Speed;
    public bool EngineKill;
    public CruiseThrustState CruiseThrust;
    private bool _active = true;

    public bool Active
    {
        get => _active;
        set
        {
            foreach (var fx in fireFx)
            {
                fx.Active = value;
            }

            _active = value;
        }
    }

    private List<ParticleEffectRenderer> fireFx = [];
    private AttachedSound? rumble;
    private AttachedSound? character;
    private AttachedSound? cruiseLoop;
    private AttachedSound? cruiseStart;
    private AttachedSound? cruiseEnd;
    private AttachedSound? killSound;
    private bool triggeredStart = false;
    private bool triggeredEnd = false;
    private bool lastEk = false;

    public Thruster[] Thrusters = [];


    public Engine(
        GameObject parent,
        Hardpoint? attachment,
        EngineEquipment equipment,
        ResourceManager resources,
        SoundManager? sound,
        EquipmentType type) : base(parent, attachment, equipment, type,
        type == EquipmentType.LocalPlayer || type == EquipmentType.RemoteObject,
        true)
    {
        if (type == EquipmentType.LocalPlayer ||
            type == EquipmentType.RemoteObject)
        {
            CreateParticleFx(resources);
            CreateSoundEffects(sound);
        }
    }

    public override void ResolveReferences()
    {
        Thrusters = Parent.EquipmentOfType<Thruster>().ToArray();
    }


    private float PitchFromRange(Vector2 range)
    {
        if (range == Vector2.Zero) return 1;
        return 1.0f + MathHelper.Lerp(range.X, range.Y, Speed) / 100f;
    }

    private float AttenFromRange(Vector2 range)
    {
        if (range == Vector2.Zero) return 0;
        return MathHelper.Lerp(range.X, range.Y, Speed);
    }


    public override void OnUpdate(double delta, GameWorld world)
    {
        var tr = Parent.Transform;
        var pos = tr.Position;
        var vel = Vector3.Zero;
        if (Parent.PhysicsComponent is not null)
        {
            vel = Parent.PhysicsComponent.Body.LinearVelocity;
        }

        if (rumble != null)
        {
            if (Speed >= 0.901f)
            {
                rumble.Stop();
            }
            else
            {
                rumble.Position = pos;
                rumble.Pitch = PitchFromRange(Equipment.Def.RumblePitchRange);
                rumble.Attenuation = AttenFromRange(Equipment.Def.RumbleAttenRange);
                rumble.Velocity = vel;
                rumble.PlayIfInactive(true);
            }

            rumble.Update();
        }

        if (character != null)
        {
            if (Speed >= 0.901f)
            {
                character.Stop();
            }
            else
            {
                character.Pitch = PitchFromRange(Equipment.Def.CharacterPitchRange);
                character.Position = pos;
                character.Velocity = vel;
                character.PlayIfInactive(true);
            }

            character.Update();
        }

        if (cruiseLoop != null)
        {
            if (Speed < 0.995f)
            {
                if (cruiseLoop.Active)
                {
                    cruiseEnd?.PlayIfInactive(false);
                }

                cruiseLoop.Stop();
            }
            else
            {
                cruiseLoop.Position = pos;
                cruiseLoop.Velocity = vel;
                cruiseLoop.PlayIfInactive(true);
            }

            cruiseLoop.Update();
        }

        if (cruiseStart != null)
        {
            if (Speed is <= 0.9f or >= 0.995f)
            {
                cruiseStart.Stop();
                triggeredStart = false;
            }
            else
            {
                cruiseStart.Position = pos;
                cruiseStart.Velocity = vel;
                if (!triggeredStart)
                {
                    cruiseStart.PlayIfInactive(false);
                    triggeredStart = true;
                }
            }

            cruiseStart.Update();
        }

        if (cruiseEnd != null)
        {
            cruiseEnd.Position = pos;
            cruiseEnd.Velocity = vel;
            cruiseEnd.Update();
        }

        if (killSound != null)
        {
            if (lastEk != EngineKill)
            {
                if (EngineKill)
                {
                    killSound.Play(false);
                }

                lastEk = EngineKill;
            }

            killSound.Position = pos;
            killSound.Velocity = vel;
            killSound.Update();
        }

        foreach (var fx in fireFx)
        {
            fx.SParam = MathHelper.Clamp(Speed, 0, 1);
        }
    }

    public override void RenderUpdate(double delta)
    {
        base.RenderUpdate(delta);
        if (fireFx.Count > 0)
        {
            var tr = Parent.Transform;
            var mat = Parent.Transform.Matrix();
            foreach (var fx in fireFx)
            {
                fx.Update(delta, tr.Position, mat);
            }
        }
    }

    public override void PrepareRender(ICamera camera, NebulaRenderer? nr, SystemRenderer sys, bool parentCull)
    {
        base.PrepareRender(camera, nr, sys, parentCull);
        foreach (var fx in fireFx)
        {
            fx.PrepareRender(camera, nr, sys, parentCull);
        }
    }

    public override void OnRemoved()
    {
        rumble?.Stop();
        character?.Stop();
        cruiseLoop?.Stop();
        cruiseStart?.Stop();
        cruiseEnd?.Stop();
    }

    void CreateSoundEffects(SoundManager? sound)
    {
        if (sound == null)
            return;
        Vector3 cone = new Vector3(Equipment.Def.InsideSoundCone, Equipment.Def.OutsideSoundCone,
            Equipment.Def.OutsideConeAttenuation);

        if (!string.IsNullOrWhiteSpace(Equipment.Def.RumbleSound))
        {
            rumble = new AttachedSound(sound, Equipment.Def.RumbleSound)
            {
                Cone = cone
            };
            rumble.PlayIfInactive(true);
        }

        if (!string.IsNullOrWhiteSpace(Equipment.Def.CharacterLoopSound))
        {
            character = new AttachedSound(sound, Equipment.Def.CharacterLoopSound)
            {
                Cone = cone
            };
            character.PlayIfInactive(true);
        }

        if (!string.IsNullOrWhiteSpace(Equipment.Def.CruiseLoopSound))
        {
            cruiseLoop = new AttachedSound(sound, Equipment.Def.CruiseLoopSound)
            {
                Cone = cone
            };
        }

        if (!string.IsNullOrWhiteSpace(Equipment.Def.CruiseStartSound))
        {
            cruiseStart = new AttachedSound(sound, Equipment.Def.CruiseStartSound)
            {
                Cone = cone
            };
        }

        if (!string.IsNullOrWhiteSpace(Equipment.Def.CruiseStopSound))
        {
            cruiseEnd = new AttachedSound(sound, Equipment.Def.CruiseStopSound)
            {
                Cone = cone
            };
        }

        if (!string.IsNullOrWhiteSpace(Equipment.Def.EngineKillSound))
        {
            killSound = new AttachedSound(sound, Equipment.Def.EngineKillSound)
            {
                Cone = cone
            };
        }
    }

    void CreateParticleFx(ResourceManager resourceManager)
    {
        var hps = Parent.GetHardpoints();
        ResolvedFx? trailEffect = Equipment.TrailEffect ?? Equipment.TrailEffectPlayer;
        if ((Parent.Flags & GameObjectFlags.Player) == GameObjectFlags.Player
            && Equipment.TrailEffectPlayer != null)
            trailEffect = Equipment.TrailEffectPlayer;

        ParticleEffect? trailFx = trailEffect?.GetEffect(resourceManager);
        ParticleEffect? flameFx = Equipment.FlameEffect?.GetEffect(resourceManager);

        foreach (var hp in hps)
        {
            if (hp.Name.Equals("hpengineglow", StringComparison.OrdinalIgnoreCase) ||
                !hp.Name.StartsWith("hpengine", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (trailFx != null)
            {
                fireFx.Add(new ParticleEffectRenderer(trailFx) { Attachment = hp });
            }

            if (flameFx != null)
            {
                fireFx.Add(new ParticleEffectRenderer(flameFx) { Index = 1, Attachment = hp });
            }
        }
    }
}
