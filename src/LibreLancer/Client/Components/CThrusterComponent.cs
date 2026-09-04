// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Physics;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.Sounds;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Client.Components
{
	public class CThrusterComponent : ThrusterComponent
	{
        private List<ParticleEffectRenderer> fireFx = [];
		private AttachedSound? thrustSound;
		public CThrusterComponent(GameObject parent, ThrusterEquipment equip) : base(parent, equip) { }

		public override void Update(double time, GameWorld world)
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

            thrustSound.Position = Parent.WorldTransform.Position;
            thrustSound.Velocity = Parent.Parent?.PhysicsComponent?.Body?.LinearVelocity ?? Vector3.Zero;
            thrustSound.PlayIfInactive(true);
            thrustSound.Update();
        }

		public override void Register(GameWorld world)
        {
            if (GetGameData(world) != null)
            {
                var resman = GetResourceManager(world);
                var pfx = Equip.Particles?.GetEffect(resman!);
                foreach (var hp in Parent!.GetHardpoints()
                             .Where(x => x.Name.Equals(Equip.HpParticles, StringComparison.OrdinalIgnoreCase)))
                {
                    fireFx.Add(new ParticleEffectRenderer(pfx) { Attachment = hp, Active = false, SParam = 1 });
                }
            }

            var sound = GetSoundManager(world);
            if (sound != null)
            {
                var soundName = FindThrusterSound(sound);
                if (soundName != null)
                {
                    sound.LoadSound(soundName);
                    thrustSound = new AttachedSound(sound, soundName);
                }
            }

            foreach (var t in fireFx)
            {
                Parent!.ExtraRenderers.Add(t);
            }
        }

		public override void Unregister(GameWorld world)
        {
            foreach (var renderer in fireFx)
            {
                Parent!.ExtraRenderers.Remove(renderer);
            }

            thrustSound?.Stop();
        }

        private string? FindThrusterSound(SoundManager sound)
        {
            var isPlayer = (Parent.Parent?.Flags & GameObjectFlags.Player) == GameObjectFlags.Player;
            var soundNames = isPlayer
                ? new[] { "interior_thruster_sound", Equip.Nickname, "exterior_thruster_sound" }
                : new[] { Equip.Nickname, "exterior_thruster_sound" };

            foreach (var soundName in soundNames)
            {
                if (!string.IsNullOrWhiteSpace(soundName) && sound.GetEntry(soundName) != null)
                {
                    return soundName;
                }
            }

            return null;
        }
    }
}
