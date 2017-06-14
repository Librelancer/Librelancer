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
using System.Linq;

namespace LibreLancer
{
	public class Hud
	{
		UIManager manager;

		HudModelElement shipinfo;
		HudModelElement contactslist;
		HudModelElement navbuttons;
		HudNumberBoxElement numberbox;
		HudGaugeElement gauge;

		public float PowerPercentage = 1f;
		public float ShieldPercentage = 1f;
		public float HullPercentage = 1f;
		public float Velocity;
		public float ThrustAvailable = 1f;

		HudToggleButtonElement maneuverA;
		HudToggleButtonElement maneuverB;
		HudToggleButtonElement maneuverC;
		HudToggleButtonElement maneuverD;

		public Hud(FreelancerGame game)
		{
			manager = new UIManager(game);
			//backgrounds
			contactslist = new HudModelElement(manager, "hud_target.cmp", -0.73f, -0.69f, 2.1f, 2.9f);
			manager.Elements.Add(contactslist);

			navbuttons = new HudModelElement(manager, "hud_maneuverbox4.cmp", 0, 0.925f, 4.5f, 6f);
			manager.Elements.Add(navbuttons);

			shipinfo = new HudModelElement(manager, "hud_shipinfo.cmp", 0.73f, -0.69f, 2.1f, 2.9f);
			manager.Elements.Add(shipinfo);

			numberbox = new HudNumberBoxElement(manager);
			manager.Elements.Add(numberbox);

			gauge = new HudGaugeElement(manager);
			manager.Elements.Add(gauge);

			//Maneuvers
			var mnvs = game.GameData.GetManeuvers().ToList();
			if (mnvs.Count != 4) throw new NotImplementedException();

			maneuverA = new HudToggleButtonElement(manager, mnvs[0].ActiveModel, mnvs[0].InactiveModel, -0.218f, 0.925f, 4.26f, 5.48f);
			manager.Elements.Add(maneuverA);

			maneuverB = new HudToggleButtonElement(manager, mnvs[1].ActiveModel, mnvs[1].InactiveModel, -0.063f, 0.925f, 4.26f, 5.48f);
			maneuverB.State = ToggleState.Inactive;
			manager.Elements.Add(maneuverB);

			maneuverC = new HudToggleButtonElement(manager, mnvs[2].ActiveModel, mnvs[2].InactiveModel, 0.071f, 0.914f, 4.26f, 5.48f);
			maneuverC.State = ToggleState.Inactive;
			manager.Elements.Add(maneuverC);

			maneuverD = new HudToggleButtonElement(manager, mnvs[3].ActiveModel, mnvs[3].InactiveModel, 0.228f, 0.925f, 4.26f, 5.48f);
			maneuverD.State = ToggleState.Inactive;
			manager.Elements.Add(maneuverD);
		}



		public void Update(TimeSpan delta)
		{
			
			manager.Update(delta);
		}

		public void Draw()
		{
			manager.Draw();
		}
	}
}