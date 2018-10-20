// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
namespace LibreLancer
{
	public class InputManager : IDisposable
	{
		public event Action<int> ToggleActivated;
		public event Action<int> ToggleUp;

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
			actions.Add(new InputAction(InputAction.ID_THRUST, "Thrust", true, Keys.Tab));
			actions.Add(new InputAction(InputAction.ID_TOGGLECRUISE, "Toggle Cruise", true, Keys.W, KeyModifiers.LeftShift));
			actions.Add(new InputAction(InputAction.ID_CANCEL, "Cancel", true, Keys.Escape));
			actions.Add(new InputAction(InputAction.ID_TOGGLEMOUSEFLIGHT, "Toggle Mouse Flight", true, Keys.Space));
			game.Keyboard.KeyDown += Keyboard_KeyDown;
			game.Keyboard.KeyUp += Keyboard_KeyUp;
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
				actions[i] = act;
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
			var mod = e.Modifiers;
			mod &= ~KeyModifiers.Numlock;
			mod &= ~KeyModifiers.Capslock;
			if (e.IsRepeat) return;
			for (int i = 0; i < actions.Count; i++)
			{
				var act = actions[i];
				if (!act.IsToggle) continue;
				if ((e.Key == act.Primary && mod == act.PrimaryModifiers) || 
				    (e.Key == act.Secondary && mod == act.SecondaryModifiers))
				{
					OnToggleActivated(act.ID);
					return;
				}
			}
		}

		void Keyboard_KeyUp(KeyEventArgs e)
		{
			var mod = e.Modifiers;
			mod &= ~KeyModifiers.Numlock;
			mod &= ~KeyModifiers.Capslock;
			for (int i = 0; i < actions.Count; i++)
			{
				var act = actions[i];
				if (!act.IsToggle) continue;
				if ((e.Key == act.Primary && mod == act.PrimaryModifiers) ||
					(e.Key == act.Secondary && mod == act.SecondaryModifiers))
				{
					OnToggleUp(act.ID);
					return;
				}
			}
		}

		void OnToggleActivated(int id)
		{
			if (ToggleActivated != null) ToggleActivated(id);
		}

		void OnToggleUp(int id)
		{
			if (ToggleUp != null) ToggleUp(id);
		}

		public void Dispose()
		{
			game.Keyboard.KeyDown -= Keyboard_KeyDown;
			game.Keyboard.KeyUp -= Keyboard_KeyUp;
		}
	}
}
