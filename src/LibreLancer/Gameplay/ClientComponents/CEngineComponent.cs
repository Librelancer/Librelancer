// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Collections.Generic;
using LibreLancer.GameData.Items;
using LibreLancer.Fx;
namespace LibreLancer
{
	public class CEngineComponent : SEngineComponent
	{
        List<AttachedEffect> fireFx = new List<AttachedEffect>();
        private AttachedSound rumble;
        private AttachedSound character;
        private AttachedSound cruiseLoop;
        private AttachedSound cruiseStart;
        private AttachedSound cruiseEnd;
		GameObject parent;
		public CEngineComponent(GameObject parent, EngineEquipment engine) : base(parent)
		{
            this.parent = parent;
			Engine = engine;
        }

        float PitchFromRange(Vector2 range)
        {
            if (range == Vector2.Zero) return 1;
            return 1.0f + MathHelper.Lerp(range.X, range.Y, Speed) / 100f;
        }

        float AttenFromRange(Vector2 range)
        {
            if (range == Vector2.Zero) return 0;
            return MathHelper.Lerp(range.X, range.Y, Speed);
        }
		public override void Update(double time)
        {
            var tr = parent.WorldTransform;
            var pos = Vector3.Transform(Vector3.Zero,tr);
            var vel = Vector3.Zero;
            if (parent.PhysicsComponent != null)
            {
                vel = parent.PhysicsComponent.Body.LinearVelocity;
            }
            if (rumble != null)
            {
                if (Speed >= 0.901f) {
                    rumble.Active = false;
                }
                else {
                    rumble.Active = true;
                    rumble.Position = pos;
                    rumble.Pitch = PitchFromRange(Engine.Def.RumblePitchRange);
                    rumble.Attenuation = AttenFromRange(Engine.Def.RumbleAttenRange);
                    rumble.Velocity = vel;
                }
                rumble.Update();
            }

            if (character != null)
            {
                if (Speed >= 0.901f) {
                    character.Active = false;
                }
                else {                    
                    character.Pitch = PitchFromRange(Engine.Def.CharacterPitchRange);
                    character.Active = true;
                    character.Position = pos;
                    character.Velocity = vel;
                }
                character.Update();
            }

            if (cruiseLoop != null)
            {
                if (Speed < 0.995f) {
                    if (cruiseLoop.Active)
                    {
                        cruiseEnd.Active = true;
                    }
                    cruiseLoop.Active = false;
                }
                else
                {
                    cruiseEnd.Active = false;
                    cruiseEnd.Played = false;
                    cruiseLoop.Active = true;
                    cruiseLoop.Position = pos;
                    cruiseLoop.Velocity = vel;
                }
                cruiseLoop.Update();
            }

            if (cruiseStart != null)
            {
                if (Speed <= 0.9f || Speed >= 0.995f)
                {
                    cruiseStart.Active = false;
                    cruiseStart.Played = false;
                }
                else
                {
                    cruiseEnd.Active = false;
                    cruiseEnd.Played = false;
                    cruiseStart.Active = true;
                    cruiseStart.Position = pos;
                    cruiseStart.Velocity = vel;
                }
                cruiseStart.Update();
            }
            
            if (cruiseEnd != null)
            {
                cruiseEnd.Position = pos;
                cruiseEnd.Velocity = vel;
                cruiseEnd.Update();
            }
            for (int i = 0; i < fireFx.Count; i++)
				fireFx[i].Update(parent, time, Speed);
            
		}
		public override void Register(Physics.PhysicsWorld physics)
        {
            GameDataManager gameData;
            if ((gameData = GetGameData()) != null)
            {
                var resman = GetResourceManager();
                var hps = parent.GetHardpoints();
                ParticleEffect trailFx = null;
                string trailFxName = Engine.Def.TrailEffect;
                if (Parent.Tag == GameObject.ClientPlayerTag && !string.IsNullOrEmpty(Engine.Def.TrailEffectPlayer))
                    trailFxName = Engine.Def.TrailEffectPlayer;
                if(!string.IsNullOrEmpty(trailFxName))
                    trailFx = gameData.GetEffect(trailFxName).GetEffect(resman);
                var fx = gameData.GetEffect(Engine.Def.FlameEffect).GetEffect(resman);

                foreach (var hp in hps)
                {
                    if (!hp.Name.Equals("hpengineglow", StringComparison.OrdinalIgnoreCase) &&
                        hp.Name.StartsWith("hpengine", StringComparison.OrdinalIgnoreCase))
                    {
                        if(trailFx != null)
                            fireFx.Add(new AttachedEffect(hp, new ParticleEffectRenderer(trailFx)));
                        fireFx.Add(new AttachedEffect(hp, new ParticleEffectRenderer(fx) { Index = 1 }));
                    }
                }

                for (int i = 0; i < fireFx.Count; i++)
                    Parent.ExtraRenderers.Add(fireFx[i].Effect);
            }

            SoundManager sound;
            if ((sound = GetSoundManager()) != null)
            {
                if (!string.IsNullOrWhiteSpace(Engine.Def.RumbleSound))
                {
                    rumble = new AttachedSound(sound)
                    {
                        Active = true, Sound = Engine.Def.RumbleSound
                    };
                }
                if (!string.IsNullOrWhiteSpace(Engine.Def.CharacterLoopSound))
                {
                    character = new AttachedSound(sound)
                    {
                        Active = true, Sound = Engine.Def.CharacterLoopSound
                    };
                }
                if (!string.IsNullOrWhiteSpace(Engine.Def.CruiseLoopSound))
                {
                    cruiseLoop = new AttachedSound(sound)
                    {
                        Active = false, Sound = Engine.Def.CruiseLoopSound
                    };
                }
                if (!string.IsNullOrWhiteSpace(Engine.Def.CruiseStartSound))
                {
                    cruiseStart = new AttachedSound(sound)
                    {
                        Active = false, Sound = Engine.Def.CruiseStartSound,
                        PlayOnce = true
                    };
                }
                if (!string.IsNullOrWhiteSpace(Engine.Def.CruiseStopSound))
                {
                    cruiseEnd = new AttachedSound(sound)
                    {
                        Active = false, Sound = Engine.Def.CruiseStopSound,
                        PlayOnce = true
                    };
                }
            }
        }
		public override void Unregister(Physics.PhysicsWorld physics)
		{
            for (int i = 0; i < fireFx.Count; i++)
                Parent.ExtraRenderers.Remove(fireFx[i].Effect);
            rumble.Kill();
            character.Kill();
        }

	}
}
