// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Interface;
using SharpDX.DirectWrite;

namespace LibreLancer.Input
{
	public class InputManager : IDisposable
	{
		public event Action<InputAction> ActionDown;
		public event Action<InputAction> ActionUp;

		Game game;

        private InputMap map;
        private bool[] _isActionDown;

        public KeyCaptureContext KeyCapture;

		public InputManager(Game game, InputMap map)
        {
            this.map = map;
            _isActionDown = new bool[(int) InputAction.COUNT];
            game.Keyboard.KeyDown += Keyboard_KeyDown;
			game.Keyboard.KeyUp += Keyboard_KeyUp;
            game.Mouse.MouseDown += Mouse_MouseDown;
            game.Mouse.MouseUp += Mouse_MouseUp;
			this.game = game;
        }


        bool IsDown(UserInput check)
        {
            if (!check.NonEmpty) return false; //Empty = nothing to check
            if (game.TextInputEnabled && !AllowedWhenTextInput(check))
                return false;
            if (check.IsMouseButton) {
                return game.Mouse.IsButtonDown(check.Mouse);
            }
            else {
                return game.Keyboard.IsKeyDown(check.Key);
            }
        }

		public void Update()
		{
            if (KeyCaptureContext.Capturing(KeyCapture))
            {
                for (int i = 0; i < map.Actions.Length; i++)
                {
                    _isActionDown[i] = false;
                }
            }
            else
            {
                for (int i = 0; i < map.Actions.Length; i++)
                {
                    _isActionDown[i] = IsDown(map.Actions[i].Primary) ||
                                       IsDown(map.Actions[i].Secondary);
                }
            }
        }

		public bool IsActionDown(InputAction action)
        {
            return _isActionDown[(int)action];
        }



        bool TryGetAction(UserInput input, out InputAction act)
        {
            for (int i = 0; i < map.Actions.Length; i++)
            {
                if (input == map.Actions[i].Primary ||
                    input == map.Actions[i].Secondary)
                {
                    act = (InputAction) i;
                    return true;
                }
            }
            act = InputAction.COUNT;
            return false;
        }

		void Keyboard_KeyDown(KeyEventArgs e)
        {
            if (KeyCaptureContext.Capturing(KeyCapture)) return;
            var input = UserInput.FromKey(e.Modifiers, e.Key);
			if (e.IsRepeat) return;
            if (game.TextInputEnabled && !AllowedWhenTextInput(input))
                return;
            if(TryGetAction(input, out var act))
                ActionDown?.Invoke(act);
        }

        static bool AllowedWhenTextInput(UserInput input)
        {
            if (input.IsMouseButton) return true;
            if ((input.Modifiers & KeyModifiers.Control) != 0 ||
                (input.Modifiers & KeyModifiers.Alt) != 0)
                return true;
            switch (input.Key)
            {
                case Keys.LeftControl:
                case Keys.RightControl:
                case Keys.LeftAlt:
                case Keys.RightAlt:
                case Keys.F1:
                case Keys.F2:
                case Keys.F3:
                case Keys.F4:
                case Keys.F5:
                case Keys.F6:
                case Keys.F7:
                case Keys.F8:
                case Keys.F9:
                case Keys.F10:
                case Keys.F11:
                case Keys.F12:
                    return true;
            }
            return false;
        }

        void Keyboard_KeyUp(KeyEventArgs e)
        {
            if (KeyCaptureContext.Capturing(KeyCapture))
            {
                if(e.Key == Keys.Escape || e.Key == Keys.F1)
                    KeyCapture.Cancel();
                else
                {
                    KeyCapture.Set(UserInput.FromKey(e.Modifiers, e.Key));
                }
            }
            else
            {
                var input = UserInput.FromKey(e.Modifiers, e.Key);
                if (game.TextInputEnabled && !AllowedWhenTextInput(input))
                    return;
                if (TryGetAction(input, out var act))
                    ActionUp?.Invoke(act);
            }
        }

        private void Mouse_MouseUp(MouseEventArgs e)
        {
            if (KeyCaptureContext.Capturing(KeyCapture))
            {
                if(e.Buttons != MouseButtons.Left)
                    KeyCapture.Set(UserInput.FromMouse(e.Buttons));
            }
            else
            {
                var input = UserInput.FromMouse(e.Buttons);
                if (TryGetAction(input, out var act))
                    ActionUp?.Invoke(act);
            }
        }

        private void Mouse_MouseDown(MouseEventArgs e)
        {
            if (KeyCaptureContext.Capturing(KeyCapture)) return;
            var input = UserInput.FromMouse(e.Buttons);
            if(TryGetAction(input, out var act))
                ActionDown?.Invoke(act);
        }

		public void Dispose()
		{
			game.Keyboard.KeyDown -= Keyboard_KeyDown;
			game.Keyboard.KeyUp -= Keyboard_KeyUp;
            game.Mouse.MouseDown -= Mouse_MouseDown;
            game.Mouse.MouseUp -= Mouse_MouseUp;
        }
	}
}
