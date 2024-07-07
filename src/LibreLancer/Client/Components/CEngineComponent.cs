// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Fx;
using LibreLancer.GameData.Items;
using LibreLancer.Render;
using LibreLancer.Server.Components;
using LibreLancer.Sounds;
using LibreLancer.World;

namespace LibreLancer.Client.Components
{
	public class  CEngineComponent : SEngineComponent
	{
        List<ParticleEffectRenderer> fireFx = new List<ParticleEffectRenderer>();
        private AttachedSound rumble;
        private AttachedSound character;
        private AttachedSound cruiseLoop;
        private AttachedSound cruiseStart;
        private AttachedSound cruiseEnd;
		GameObject Ship;

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

		public CEngineComponent(GameObject parent, EngineEquipment engine) : base(parent)
		{
            this.Ship = parent;
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
            var tr = Ship.WorldTransform;
            var pos = tr.Position;
            var vel = Vector3.Zero;
            if (Ship.PhysicsComponent != null)
            {
                vel = Ship.PhysicsComponent.Body.LinearVelocity;
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
                fireFx[i].SParam = MathHelper.Clamp(Speed, 0, 1);
        }
		public override void Register(Physics.PhysicsWorld physics)
        {
            GameDataManager gameData;
            if ((gameData = GetGameData()) != null)
            {
                var resman = GetResourceManager();
                var hps = Ship.GetHardpoints();
                ParticleEffect trailFx = null;
                string trailFxName = Engine.Def.TrailEffect;
                if (Parent.Tag == GameObject.ClientPlayerTag && !string.IsNullOrEmpty(Engine.Def.TrailEffectPlayer))
                    trailFxName = Engine.Def.TrailEffectPlayer;
                if(!string.IsNullOrEmpty(trailFxName))
                    trailFx = gameData.GetEffect(trailFxName).GetEffect(resman);
                ParticleEffect flameFx = null;
                if(!string.IsNullOrEmpty(Engine.Def.FlameEffect))
                    flameFx = gameData.GetEffect(Engine.Def.FlameEffect).GetEffect(resman);
                foreach (var hp in hps)
                {
                    if (!hp.Name.Equals("hpengineglow", StringComparison.OrdinalIgnoreCase) &&
                        hp.Name.StartsWith("hpengine", StringComparison.OrdinalIgnoreCase))
                    {
                        if(trailFx != null)
                            fireFx.Add(new ParticleEffectRenderer(trailFx) { Attachment = hp });
                        if (flameFx != null)
                            fireFx.Add(new ParticleEffectRenderer(flameFx) { Index = 1, Attachment = hp });
                    }
                }

                for (int i = 0; i < fireFx.Count; i++)
                {
                    Parent.ExtraRenderers.Add(fireFx[i]);
                    fireFx[i].Active = Active;
                }
            }

            SoundManager sound;
            if (PlaySound && (sound = GetSoundManager()) != null)
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
                Parent.ExtraRenderers.Remove(fireFx[i]);
            rumble?.Kill();
            character?.Kill();
            cruiseLoop?.Kill();
        }

	}
}
