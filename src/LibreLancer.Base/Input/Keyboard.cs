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
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Collections.Generic;
namespace LibreLancer
{
	public delegate void TextInputHandler (string text);
	public delegate void KeyEventHandler (KeyEventArgs e);
	public class Keyboard
	{
		public event TextInputHandler TextInput;
		public event KeyEventHandler KeyDown;
		public event KeyEventHandler KeyUp;
		Dictionary<Keys, bool> keysDown = new Dictionary<Keys, bool>();

		internal Keyboard ()
		{
		}

		internal void OnTextInput(string text)
		{
			if (TextInput != null)
				TextInput (text);
		}

		internal void OnKeyDown (Keys key, KeyModifiers mod, bool isRepeat)
		{
			if (KeyDown != null)
				KeyDown (new KeyEventArgs (key, mod, isRepeat));
			keysDown [key] = true;
		}

		internal void OnKeyUp (Keys key, KeyModifiers mod)
		{
			if (KeyUp != null)
				KeyUp (new KeyEventArgs (key, mod, false));
			keysDown [key] = false;
		}

		public bool IsKeyDown(Keys key)
		{
			return keysDown.ContainsKey (key) && keysDown [key];
		}

		public bool IsKeyUp (Keys key)
		{
			if (keysDown.ContainsKey (key))
				return !keysDown [key];
			return true;
		}
	}
}

