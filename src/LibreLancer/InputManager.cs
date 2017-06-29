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
	public class InputManager : IDisposable
	{
		public event Action<int> ToggleActivated;

		List<InputAction> actions = new List<InputAction>();
		Game game;
		public InputManager(Game game)
		{
			actions.Add(new InputAction(InputAction.ID_STRAFEUP, "Strafe Up", false, Keys.W));
			actions.Add(new InputAction(InputAction.ID_STRAFEDOWN, "Strafe Down", false, Keys.S));
			actions.Add(new InputAction(InputAction.ID_STRAFELEFT, "Strafe Left", false, Keys.A));
			actions.Add(new InputAction(InputAction.ID_STRAFERIGHT, "Strafe Right", false, Keys.D));
			actions.Add(new InputAction(InputAction.ID_THROTTLEUP, "Increase Throttle", false, Keys.Up));
			actions.Add(new InputAction(InputAction.ID_THROTTLEDOWN, "Decrease Throttle", false, Keys.Down));
			actions.Add(new InputAction(InputAction.ID_THRUST, "Thrust", false, Keys.Tab));
			actions.Add(new InputAction(InputAction.ID_TOGGLECRUISE, "Toggle Cruise", true, Keys.W, KeyModifiers.LeftShift));
			actions.Add(new InputAction(InputAction.ID_CANCEL, "Cancel", true, Keys.Escape));
			actions.Add(new InputAction(InputAction.ID_TOGGLEMOUSEFLIGHT, "Toggle Mouse Flight", true, Keys.Space));
			game.Keyboard.KeyDown += Keyboard_KeyDown;
			this.game = game;
		}

		public void Update()
		{
			for (int i = 0; i < actions.Count; i++)
			{
				var act = actions[i];
				if (act.IsToggle) continue;
				act.IsDown = (
					(act.Primary != Keys.Unknown && game.Keyboard.IsKeyDown(act.Primary)) ||
					(act.Secondary != Keys.Unknown && game.Keyboard.IsKeyDown(act.Secondary))
				);
			}
		}

		public bool ActionDown(int id)
		{
			for (int i = 0; i < actions.Count; i++)
			{
				if (actions[i].ID == id) return actions[i].IsDown;
			}
			return false;
		}

		void Keyboard_KeyDown(KeyEventArgs e)
		{
			if (e.IsRepeat) return;
			for (int i = 0; i < actions.Count; i++)
			{
				var act = actions[i];
				if (!act.IsToggle) continue;
				if ((e.Key == act.Primary && e.Modifiers == act.PrimaryModifiers) || 
				    (e.Key == act.Secondary && e.Modifiers == act.SecondaryModifiers))
				{
					OnToggleActivated(act.ID);
					return;
				}
			}
		}

		void OnToggleActivated(int id)
		{
			if (ToggleActivated != null) ToggleActivated(id);
		}

		public void Dispose()
		{
			game.Keyboard.KeyDown -= Keyboard_KeyDown;
		}
	}
}
