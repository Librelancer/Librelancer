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
		GameObject parent;
		public CEngineComponent(GameObject parent, EngineEquipment engine) : base(parent)
		{
			//var fx = game.GameData.GetEffect(engine.Def.FlameEffect);
			
			this.parent = parent;
			Engine = engine;
           
        }

        float PitchFromRange(Vector2 range)
        {
            if (range == Vector2.Zero) return 1;
            return 1.0f + MathHelper.Lerp(range.X, range.Y, Speed) / 100f;
        }
		public override void Update(TimeSpan time)
        {
            var tr = parent.GetTransform();
            var pos = Vector3.Transform(Vector3.Zero,tr);
            if (rumble != null)
            {
                rumble.Position = pos;
                rumble.Pitch = PitchFromRange(Engine.Def.RumblePitchRange);
                rumble.Update();
                character.Position = pos;
                character.Pitch = PitchFromRange(Engine.Def.CharacterPitchRange);
                character.Update();
            }
            for (int i = 0; i < fireFx.Count; i++)
				fireFx[i].Update(parent, time, Speed);
            
		}
		public override void Register(Physics.PhysicsWorld physics)
        {
            GameDataManager gameData;
            if ((gameData = GetGameData()) != null)
            {
                var hps = parent.GetHardpoints();
                foreach (var hp in hps)
                {
                    if (!hp.Name.Equals("hpengineglow", StringComparison.OrdinalIgnoreCase) &&
                        hp.Name.StartsWith("hpengine", StringComparison.OrdinalIgnoreCase))
                    {
                        //fireFx.Add(new AttachedEffect(hp, new ParticleEffectRenderer(gameData.GetEffect(Engine.Def.FlameEffect))));
                    }
                }

                for (int i = 0; i < fireFx.Count; i++)
                    Parent.ForceRenderCheck.Add(fireFx[i].Effect);
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
            }
        }
		public override void Unregister(Physics.PhysicsWorld physics)
		{
            for (int i = 0; i < fireFx.Count; i++)
                Parent.ForceRenderCheck.Remove(fireFx[i].Effect);
            rumble.Kill();
            character.Kill();
        }

	}
}
