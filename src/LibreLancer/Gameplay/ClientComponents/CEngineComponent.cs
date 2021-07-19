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
	public class CEngineComponent : GameComponent
	{
		public EngineEquipment Engine;
		public float Speed = 1f;
		List<AttachedEffect> fireFx = new List<AttachedEffect>();
        private AttachedSound rumble;
        private AttachedSound character;
        private AttachedSound cruiseLoop;
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
                if (Speed > 0.91f) {
                    rumble.Active = false;
                    character.Active = false;
                }
                else {
                    rumble.Active = true;
                    character.Active = true;
                    rumble.Position = pos;
                    rumble.Pitch = PitchFromRange(Engine.Def.RumblePitchRange);
                    rumble.Attenuation = AttenFromRange(Engine.Def.RumbleAttenRange);
                    character.Position = pos;
                    character.Pitch = PitchFromRange(Engine.Def.CharacterPitchRange);
                    rumble.Velocity = vel;
                    character.Velocity = vel;
                }
                rumble.Update();
                character.Update();
            }

            if (cruiseLoop != null)
            {
                if (Speed < 0.98f) {
                    cruiseLoop.Active = false;
                }
                else {
                    cruiseLoop.Active = true;
                    cruiseLoop.Position = pos;
                    cruiseLoop.Velocity = vel;
                }
                cruiseLoop.Update();
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
                var fx = gameData.GetEffect(Engine.Def.FlameEffect).GetEffect(resman);

                foreach (var hp in hps)
                {
                    if (!hp.Name.Equals("hpengineglow", StringComparison.OrdinalIgnoreCase) &&
                        hp.Name.StartsWith("hpengine", StringComparison.OrdinalIgnoreCase))
                    {
                        fireFx.Add(new AttachedEffect(hp, new ParticleEffectRenderer(fx)));
                    }
                }

                for (int i = 0; i < fireFx.Count; i++)
                    Parent.ExtraRenderers.Add(fireFx[i].Effect);
            }

            SoundManager sound;
            if ((sound = GetSoundManager()) != null)
            {
                rumble = new AttachedSound(sound)
                {
                    Active = true, Sound = Engine.Def.RumbleSound
                };
                character = new AttachedSound(sound)
                {
                    Active = true, Sound = Engine.Def.CharacterLoopSound
                };
                cruiseLoop = new AttachedSound(sound)
                {
                    Active = false, Sound = Engine.Def.CruiseLoopSound
                };
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
