// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer;

public delegate void TextInputHandler (string text);
public delegate void KeyEventHandler (KeyEventArgs e);
public class Keyboard
{
    public event TextInputHandler? TextInput;
    public event KeyEventHandler? KeyDown;
    public event KeyEventHandler? KeyUp;
    private BitArray512 keysDown = new BitArray512();

    internal Keyboard ()
    {
    }

    internal void OnTextInput(string text)
    {
        TextInput?.Invoke(text);
    }

    internal void OnKeyDown (Keys key, KeyModifiers mod, bool isRepeat)
    {
        KeyDown?.Invoke(new KeyEventArgs (key, mod, isRepeat));
        keysDown [(int)key] = true;
    }

    internal void OnKeyUp (Keys key, KeyModifiers mod)
    {
        KeyUp?.Invoke(new KeyEventArgs (key, mod, false));
        keysDown [(int)key] = false;
    }

    public bool IsKeyDown(Keys key) => (int) key > 0 && (int) key < 512 && keysDown[(int)key];
    public bool IsKeyUp (Keys key) => (int) key < 0 || (int) key > 511 || !keysDown[(int) key];
    public bool AnyKeyDown() => keysDown.Any();
}
