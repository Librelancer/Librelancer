using System.Text;
using LibreLancer.Data;

namespace LibreLancer.Input;

/// <summary>
/// Struct for containing a mouse button or keyboard combination
/// </summary>
public struct UserInput
{
    private int data;

    public bool NonEmpty => data != 0;
    
    internal static KeyModifiers SanitizeModifiers(KeyModifiers mod)
    {
        //Don't need these from SDL2
        mod &= ~KeyModifiers.Numlock;
        mod &= ~KeyModifiers.Capslock;
        //Left/Right shift - set both
        if ((mod & KeyModifiers.Control) != 0) mod |= KeyModifiers.Control;
        if ((mod & KeyModifiers.Shift) != 0) mod |= KeyModifiers.Shift;
        if ((mod & KeyModifiers.Alt) != 0) mod |= KeyModifiers.Alt;
        return mod;
    }

    public static UserInput FromKey(KeyModifiers mod, Keys key)
    {
        return new UserInput()
        {
            data = ((int) SanitizeModifiers(mod) << 16) | (int) key
        };
    }

    public static UserInput FromKey(Keys key)
    {
        return new UserInput()
        {
            data = (int) key
        };
    }

    public static UserInput FromMouse(MouseButtons buttons)
    {
        return new UserInput()
        {
            data = ((int) KeyModifiers.Reserved << 16) | (int) buttons
        };
    }

    public int ToInt32() => data;
    public static UserInput FromInt32(int data) => new UserInput() {data = data};

    public bool IsMouseButton => ((data >> 16) & (int) KeyModifiers.Reserved) == (int) KeyModifiers.Reserved;
    public MouseButtons Mouse => IsMouseButton ? (MouseButtons) (data & 0xFFFF) : MouseButtons.None;
    public Keys Key => IsMouseButton ? Keys.Unknown : (Keys) (data & 0xFFFF);
    public KeyModifiers Modifiers => IsMouseButton ? KeyModifiers.None : (KeyModifiers) ((data >> 16) & 0xFFFF);

    public string ToDisplayString(InfocardManager ic)
    {
        if (data == 0) return string.Empty;
        if (IsMouseButton)
        {
            switch (Mouse)
            {
                case MouseButtons.Left:
                    return ic.GetStringResource(1255); //Mouse 1
                case MouseButtons.Middle:
                    return ic.GetStringResource(1256); //Mouse 3
                case MouseButtons.Right:
                    return ic.GetStringResource(1257); //Mouse 2
                case MouseButtons.X1:
                    return "Mouse 4";
                case MouseButtons.X2:
                    return "Mouse 5";
            }
            return "!!BADMOUSE!!";
        }
        else
        {
            if (Modifiers != 0)
            {
                var builder = new StringBuilder();
                if ((Modifiers & KeyModifiers.Shift) != 0)
                    builder.Append(ic.GetStringResource(1259)); //Shift+
                if ((Modifiers & KeyModifiers.Alt) != 0)
                    builder.Append(ic.GetStringResource(1261)); //Alt+
                if ((Modifiers & KeyModifiers.Control) != 0)
                    builder.Append(ic.GetStringResource(1260)); //Ctrl+
                builder.Append(Key.GetDisplayName());
                return builder.ToString();
            }
            return Key.GetDisplayName();
        }
    }

    public override string ToString()
    {
        if (data == 0) return "EMPTY";
        else
        {
            if (IsMouseButton)
                return Mouse.ToString();
            else
            {
                var k = Key.ToString();
                if (Modifiers != KeyModifiers.None)
                {
                    return Modifiers.ToString() + " + " + k;
                }

                return k;
            }
        }
    }

    public bool Equals(UserInput other)
    {
        return data == other.data;
    }

    public override bool Equals(object obj)
    {
        return obj is UserInput other && Equals(other);
    }

    public override int GetHashCode()
    {
        return data;
    }

    public static bool operator ==(UserInput left, UserInput right)
    {
        return left.data == right.data;
    }

    public static bool operator !=(UserInput left, UserInput right)
    {
        return left.data != right.data;
    }
}