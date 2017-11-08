/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Collections.Generic;
namespace LibreLancer
{
	public class ThrusterComponent : GameComponent
	{
		public GameData.Items.ThrusterEquipment Equip;
		public bool Enabled;
		List<AttachedEffect> fireFx = new List<AttachedEffect>();
		public ThrusterComponent(GameObject parent, GameData.Items.ThrusterEquipment equip) : base(parent)
		{
			Equip = equip;
			var hps = parent.GetHardpoints();
			foreach (var hp in hps)
			{
				if (!hp.Name.Equals(Equip.HpParticles, StringComparison.OrdinalIgnoreCase))
				{
					fireFx.Add(new AttachedEffect(hp, new ParticleEffectRenderer(Equip.Particles)));
				}
			}
		}

		public override void Update(TimeSpan time)
		{
			for (int i = 0; i < fireFx.Count; i++)
				fireFx[i].Update(Parent, time, Enabled ? 1 : 0);
		}
		public override void Register(SystemRenderer renderer, LibreLancer.Jitter.World physics)
		{
			for (int i = 0; i < fireFx.Count; i++)
				fireFx[i].Effect.Register(renderer);
		}
		public override void Unregister()
		{
			for (int i = 0; i < fireFx.Count; i++)
				fireFx[i].Effect.Unregister();
		}
	}
}
