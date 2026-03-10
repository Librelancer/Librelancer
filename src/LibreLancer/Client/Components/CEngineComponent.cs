// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Fx;
using LibreLancer.Physics;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.Server.Components;
using LibreLancer.Sounds;
using LibreLancer.World;

namespace LibreLancer.Client.Components
{
	public class CEngineComponent(GameObject parent, EngineEquipment engine) : SEngineComponent(parent, engine)
    {
        private List<ParticleEffectRenderer> fireFx = [];
        private AttachedSound? rumble;
        private AttachedSound? character;
        private AttachedSound? cruiseLoop;
        private AttachedSound? cruiseStart;
        private AttachedSound? cruiseEnd;
        private GameObject Ship = parent;

        private bool _active = true;

        public bool Active
        {
            get => _active;
            set {
                foreach(var fx in fireFx)
                {
                    fx.Active = value;
                }
                _active = value;
            }
        }

        public bool PlaySound = true;

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

        private bool triggeredStart = false;
        private bool triggeredEnd = false;

		public override void Update(double time)
        {
            var tr = Ship.WorldTransform;
            var pos = tr.Position;
            var vel = Vector3.Zero;
            if (Ship.PhysicsComponent is not null && Ship.PhysicsComponent.Body is not null)
            {
                vel = Ship.PhysicsComponent.Body.LinearVelocity;
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
                    rumble.Pitch = PitchFromRange(Engine.Def.RumblePitchRange);
                    rumble.Attenuation = AttenFromRange(Engine.Def.RumbleAttenRange);
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
                    character.Pitch = PitchFromRange(Engine.Def.CharacterPitchRange);
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

            foreach (var fx in fireFx)
            {
                fx.SParam = MathHelper.Clamp(Speed, 0, 1);
            }
        }
		public override void Register(PhysicsWorld? physics)
        {
            GameDataManager? gameData = GetGameData();
            if (gameData is not null)
            {
                var resourceManager = GetResourceManager()!;
                var hps = Ship.GetHardpoints();
                ParticleEffect? trailFx = null;
                var trailFxName = Engine.Def.TrailEffect;
                if (Parent!.Tag == GameObject.ClientPlayerTag && !string.IsNullOrEmpty(Engine.Def.TrailEffectPlayer))
                    trailFxName = Engine.Def.TrailEffectPlayer;
                if (!string.IsNullOrEmpty(trailFxName))
                {
                    trailFx = gameData.Items.Effects.Get(trailFxName)?.GetEffect(resourceManager);
                }

                ParticleEffect? flameFx = null;
                if(!string.IsNullOrEmpty(Engine.Def.FlameEffect))
                    flameFx = gameData.Items.Effects.Get(Engine.Def.FlameEffect)?.GetEffect(resourceManager);
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

                foreach (var fx in fireFx)
                {
                    Parent.ExtraRenderers.Add(fx);
                    fx.Active = Active;
                }
            }

            SoundManager? sound;

            if (!PlaySound || (sound = GetSoundManager()) == null)
            {
                return;
            }

            Vector3 cone = new Vector3(Engine.Def.InsideSoundCone, Engine.Def.OutsideSoundCone, Engine.Def.OutsideConeAttenuation);

            if (!string.IsNullOrWhiteSpace(Engine.Def.RumbleSound))
            {
                rumble = new AttachedSound(sound, Engine.Def.RumbleSound)
                {
                    Cone = cone
                };
                rumble.PlayIfInactive(true);
            }

            if (!string.IsNullOrWhiteSpace(Engine.Def.CharacterLoopSound))
            {
                character = new AttachedSound(sound, Engine.Def.CharacterLoopSound)
                {
                    Cone = cone
                };
                character.PlayIfInactive(true);
            }

            if (!string.IsNullOrWhiteSpace(Engine.Def.CruiseLoopSound))
            {
                cruiseLoop = new AttachedSound(sound, Engine.Def.CruiseLoopSound)
                {
                    Cone = cone
                };
            }

            if (!string.IsNullOrWhiteSpace(Engine.Def.CruiseStartSound))
            {
                cruiseStart = new AttachedSound(sound, Engine.Def.CruiseStartSound)
                {
                    Cone = cone
                };
            }

            if (!string.IsNullOrWhiteSpace(Engine.Def.CruiseStopSound))
            {
                cruiseEnd = new AttachedSound(sound, Engine.Def.CruiseStopSound)
                {
                    Cone = cone
                };
            }
        }

        public override void HardpointDestroyed(Hardpoint hardpoint)
        {
            for (var i = 0; i < fireFx.Count; i++)
            {
                if (fireFx[i].Attachment != hardpoint)
                {
                    continue;
                }

                Parent!.ExtraRenderers.Remove(fireFx[i]);
                fireFx.RemoveAt(i);
                i--;
            }
        }

        public override void Unregister(PhysicsWorld? physics)
		{
            foreach (var fx in fireFx)
            {
                Parent!.ExtraRenderers.Remove(fx);
            }

            rumble?.Stop();
            character?.Stop();
            cruiseLoop?.Stop();
            cruiseStart?.Stop();
            cruiseEnd?.Stop();
        }

	}
}
