namespace LibreLancer.Input;

static class VKMap
{
    public static UserInput Map(int input, KeyModifiers modifiers)
    {
        Keys keys;
        switch (input)
        {
            //Yes Freelancer uses negative numbers in keymap instead of the VK constants
            case (int)VK.LeftButton: 
            case -1:
                return UserInput.FromMouse(MouseButtons.Left);
            case -2:
            case (int)VK.RightButton:
                return UserInput.FromMouse(MouseButtons.Right);
            case -3:
            case (int)VK.MiddleButton:
                return UserInput.FromMouse(MouseButtons.Middle);
            case -4:
            case (int)VK.ExtraButton1:
                return UserInput.FromMouse(MouseButtons.X1);
            case -5:
            case (int)VK.ExtraButton2:
                return UserInput.FromMouse(MouseButtons.X2);
        }
        return UserInput.FromKey(modifiers, GetKey((VK)input));
    }

    static Keys GetKey(VK vk)
    {
        switch (vk)
        {
            case VK.Cancel: return Keys.Cancel;
            case VK.Backspace: return Keys.AppBack;
            case VK.Tab: return Keys.Tab;
            case VK.Clear: return Keys.Clear;
            case VK.Return: return Keys.Enter;
            case VK.Shift: return Keys.LeftShift;
            case VK.Control: return Keys.LeftControl;
            case VK.Menu: return Keys.Menu;
            case VK.Pause: return Keys.Pause;
            case VK.CapsLock: return Keys.CapsLock;
            case VK.Kana:
            case VK.Junja:
            case VK.Final:
            case VK.Hanja:
                return Keys.Lang1;
            case VK.Escape: return Keys.Escape;
            case VK.Convert: return Keys.Lang2; //?
            case VK.NonConvert: return Keys.Lang3; //?
            case VK.Accept: break; //?
            case VK.ModeChange: break; //?
            case VK.Space: return Keys.Space;
            case VK.A: return Keys.A;
            case VK.B: return Keys.B;
            case VK.C: return Keys.C;
            case VK.D: return Keys.D;
            case VK.E: return Keys.E;
            case VK.F: return Keys.F;
            case VK.G: return Keys.G;
            case VK.H: return Keys.H;
            case VK.I: return Keys.I;
            case VK.J: return Keys.J;
            case VK.K: return Keys.K;
            case VK.L: return Keys.L;
            case VK.M: return Keys.M;
            case VK.N: return Keys.N;
            case VK.O: return Keys.O;
            case VK.P: return Keys.P;
            case VK.Q: return Keys.Q;
            case VK.R: return Keys.R;
            case VK.S: return Keys.S;
            case VK.T: return Keys.T;
            case VK.U: return Keys.U;
            case VK.V: return Keys.V;
            case VK.W: return Keys.W;
            case VK.X: return Keys.X;
            case VK.Y: return Keys.Y;
            case VK.Z: return Keys.Z;
            case VK.N0: return Keys.D0;
            case VK.N1: return Keys.D1;
            case VK.N2: return Keys.D2;
            case VK.N3: return Keys.D3;
            case VK.N4: return Keys.D4;
            case VK.N5: return Keys.D5;
            case VK.N6: return Keys.D6;
            case VK.N7: return Keys.D7;
            case VK.N8: return Keys.D8;
            case VK.N9: return Keys.D9;
            case VK.F1: return Keys.F1;
            case VK.F2: return Keys.F2; 
            case VK.F3: return Keys.F3;
            case VK.F4: return Keys.F4;
            case VK.F5: return Keys.F5;
            case VK.F6: return Keys.F6;
            case VK.F7: return Keys.F7;
            case VK.F8: return Keys.F8;
            case VK.F9: return Keys.F9;
            case VK.F10: return Keys.F10;
            case VK.F11: return Keys.F11;
            case VK.F12: return Keys.F12;
            case VK.F13: return Keys.F13;
            case VK.F14: return Keys.F14;
            case VK.F15: return Keys.F15;
            case VK.F16: return Keys.F16;
            case VK.F17: return Keys.F17;
            case VK.F18: return Keys.F18;
            case VK.F19: return Keys.F19;
            case VK.F20: return Keys.F20;
            case VK.F21: return Keys.F21;
            case VK.F22: return Keys.F22;
            case VK.F23: return Keys.F23;
            case VK.F24: return Keys.F24;
            case VK.Numpad0: return Keys.Keypad0;
            case VK.Numpad1: return Keys.Keypad1;
            case VK.Numpad2: return Keys.Keypad2;
            case VK.Numpad3: return Keys.Keypad3;
            case VK.Numpad4: return Keys.Keypad4;
            case VK.Numpad5: return Keys.Keypad5;
            case VK.Numpad6: return Keys.Keypad6;
            case VK.Numpad7: return Keys.Keypad7;
            case VK.Numpad8: return Keys.Keypad8;
            case VK.Numpad9: return Keys.Keypad9;
            case VK.LeftShift: return Keys.LeftShift;
            case VK.RightShift: return Keys.RightShift;
            case VK.LeftControl: return Keys.LeftControl;
            case VK.RightControl: return Keys.RightControl;
            case VK.LeftWindows: return Keys.LeftGui;
            case VK.RightWindows: return Keys.RightGui;
            case VK.Left: return Keys.Left;
            case VK.Right: return Keys.Right;
            case VK.Up: return Keys.Up;
            case VK.Down: return Keys.Down;
            case VK.Insert: return Keys.Insert;
            case VK.Delete: return Keys.Delete;
            case VK.Help: return Keys.Help;
            case VK.Home: return Keys.NavHome;
            case VK.End: return Keys.NavEnd;
            case VK.Prior: return Keys.NavPageUp;
            case VK.Next: return Keys.NavPageDown;
            case VK.Select: return Keys.Select;
            case VK.Print: break; //no mapping for PRINT key
            case VK.Execute: return Keys.Execute;
            case VK.Snapshot: return Keys.PrintScreen;
            case VK.Sleep: return Keys.Sleep;
            case VK.Add: return Keys.KeypadPlus;
            case VK.Separator: return Keys.Separator;
            case VK.Subtract: return Keys.KeypadMinus;
            case VK.Decimal: return Keys.KeypadDecimal;
            case VK.Divide: return Keys.KeypadDivide;
            case VK.Multiply: return Keys.KeypadMultiply;
            case VK.OEMPlus: return Keys.Equals;
            case VK.OEMComma: return Keys.Comma;
            case VK.OEMMinus: return Keys.Minus;
            case VK.OEMPeriod: return Keys.Period;
            case VK.OEM1: return Keys.Semicolon;
            case VK.OEM2: return Keys.Slash;
            case VK.OEM3: return Keys.Grave;
            case VK.OEM4: return Keys.LeftBracket;
            case VK.OEM5: return Keys.Backslash;
            case VK.OEM6: return Keys.RightBracket;
            case VK.OEM7: return Keys.Apostrophe;
            case VK.NumLock: return Keys.NumLockClear;
            case VK.ScrollLock: return Keys.ScrollLock;
        }

        return Keys.Unknown;
    }

   
    enum VK : int
    {
        LeftButton = 0x01,
        RightButton = 0x02,
        Cancel = 0x03,
        MiddleButton = 0x04,
        ExtraButton1 = 0x05,
        ExtraButton2 = 0x06,
        Backspace = 0x08,
        Tab = 0x09,
        Clear = 0x0C,
        Return = 0x0D,
        Shift = 0x10,
        Control = 0x11,
        /// <summary></summary>
        Menu = 0x12,
        /// <summary></summary>
        Pause = 0x13,
        /// <summary></summary>
        CapsLock = 0x14,
        /// <summary></summary>
        Kana = 0x15,
        /// <summary></summary>
        Hangeul = 0x15,
        /// <summary></summary>
        Hangul = 0x15,
        /// <summary></summary>
        Junja = 0x17,
        /// <summary></summary>
        Final = 0x18,
        /// <summary></summary>
        Hanja = 0x19,
        /// <summary></summary>
        Kanji = 0x19,
        /// <summary></summary>
        Escape = 0x1B,
        /// <summary></summary>
        Convert = 0x1C,
        /// <summary></summary>
        NonConvert = 0x1D,
        /// <summary></summary>
        Accept = 0x1E,
        /// <summary></summary>
        ModeChange = 0x1F,
        /// <summary></summary>
        Space = 0x20,
        /// <summary></summary>
        Prior = 0x21,
        /// <summary></summary>
        Next = 0x22,
        /// <summary></summary>
        End = 0x23,
        /// <summary></summary>
        Home = 0x24,
        /// <summary></summary>
        Left = 0x25,
        /// <summary></summary>
        Up = 0x26,
        /// <summary></summary>
        Right = 0x27,
        /// <summary></summary>
        Down = 0x28,
        /// <summary></summary>
        Select = 0x29,
        /// <summary></summary>
        Print = 0x2A,
        /// <summary></summary>
        Execute = 0x2B,
        /// <summary></summary>
        Snapshot = 0x2C,
        /// <summary></summary>
        Insert = 0x2D,
        /// <summary></summary>
        Delete = 0x2E,
        /// <summary></summary>
        Help = 0x2F,
        /// <summary></summary>
        N0 = 0x30,
        /// <summary></summary>
        N1 = 0x31,
        /// <summary></summary>
        N2 = 0x32,
        /// <summary></summary>
        N3 = 0x33,
        /// <summary></summary>
        N4 = 0x34,
        /// <summary></summary>
        N5 = 0x35,
        /// <summary></summary>
        N6 = 0x36,
        /// <summary></summary>
        N7 = 0x37,
        /// <summary></summary>
        N8 = 0x38,
        /// <summary></summary>
        N9 = 0x39,
        /// <summary></summary>
        A = 0x41,
        /// <summary></summary>
        B = 0x42,
        /// <summary></summary>
        C = 0x43,
        /// <summary></summary>
        D = 0x44,
        /// <summary></summary>
        E = 0x45,
        /// <summary></summary>
        F = 0x46,
        /// <summary></summary>
        G = 0x47,
        /// <summary></summary>
        H = 0x48,
        /// <summary></summary>
        I = 0x49,
        /// <summary></summary>
        J = 0x4A,
        /// <summary></summary>
        K = 0x4B,
        /// <summary></summary>
        L = 0x4C,
        /// <summary></summary>
        M = 0x4D,
        /// <summary></summary>
        N = 0x4E,
        /// <summary></summary>
        O = 0x4F,
        /// <summary></summary>
        P = 0x50,
        /// <summary></summary>
        Q = 0x51,
        /// <summary></summary>
        R = 0x52,
        /// <summary></summary>
        S = 0x53,
        /// <summary></summary>
        T = 0x54,
        /// <summary></summary>
        U = 0x55,
        /// <summary></summary>
        V = 0x56,
        /// <summary></summary>
        W = 0x57,
        /// <summary></summary>
        X = 0x58,
        /// <summary></summary>
        Y = 0x59,
        /// <summary></summary>
        Z = 0x5A,
        /// <summary></summary>
        LeftWindows = 0x5B,
        /// <summary></summary>
        RightWindows = 0x5C,
        /// <summary></summary>
        Application = 0x5D,
        /// <summary></summary>
        Sleep = 0x5F,
        /// <summary></summary>
        Numpad0 = 0x60,
        /// <summary></summary>
        Numpad1 = 0x61,
        /// <summary></summary>
        Numpad2 = 0x62,
        /// <summary></summary>
        Numpad3 = 0x63,
        /// <summary></summary>
        Numpad4 = 0x64,
        /// <summary></summary>
        Numpad5 = 0x65,
        /// <summary></summary>
        Numpad6 = 0x66,
        /// <summary></summary>
        Numpad7 = 0x67,
        /// <summary></summary>
        Numpad8 = 0x68,
        /// <summary></summary>
        Numpad9 = 0x69,
        /// <summary></summary>
        Multiply = 0x6A,
        /// <summary></summary>
        Add = 0x6B,
        /// <summary></summary>
        Separator = 0x6C,
        /// <summary></summary>
        Subtract = 0x6D,
        /// <summary></summary>
        Decimal = 0x6E,
        /// <summary></summary>
        Divide = 0x6F,
        /// <summary></summary>
        F1 = 0x70,
        /// <summary></summary>
        F2 = 0x71,
        /// <summary></summary>
        F3 = 0x72,
        /// <summary></summary>
        F4 = 0x73,
        /// <summary></summary>
        F5 = 0x74,
        /// <summary></summary>
        F6 = 0x75,
        /// <summary></summary>
        F7 = 0x76,
        /// <summary></summary>
        F8 = 0x77,
        /// <summary></summary>
        F9 = 0x78,
        /// <summary></summary>
        F10 = 0x79,
        /// <summary></summary>
        F11 = 0x7A,
        /// <summary></summary>
        F12 = 0x7B,
        /// <summary></summary>
        F13 = 0x7C,
        /// <summary></summary>
        F14 = 0x7D,
        /// <summary></summary>
        F15 = 0x7E,
        /// <summary></summary>
        F16 = 0x7F,
        /// <summary></summary>
        F17 = 0x80,
        /// <summary></summary>
        F18 = 0x81,
        /// <summary></summary>
        F19 = 0x82,
        /// <summary></summary>
        F20 = 0x83,
        /// <summary></summary>
        F21 = 0x84,
        /// <summary></summary>
        F22 = 0x85,
        /// <summary></summary>
        F23 = 0x86,
        /// <summary></summary>
        F24 = 0x87,
        /// <summary></summary>
        NumLock = 0x90,
        /// <summary></summary>
        ScrollLock = 0x91,
        /// <summary></summary>
        NEC_Equal = 0x92,
        /// <summary></summary>
        Fujitsu_Jisho = 0x92,
        /// <summary></summary>
        Fujitsu_Masshou = 0x93,
        /// <summary></summary>
        Fujitsu_Touroku = 0x94,
        /// <summary></summary>
        Fujitsu_Loya = 0x95,
        /// <summary></summary>
        Fujitsu_Roya = 0x96,
        /// <summary></summary>
        LeftShift = 0xA0,
        /// <summary></summary>
        RightShift = 0xA1,
        /// <summary></summary>
        LeftControl = 0xA2,
        /// <summary></summary>
        RightControl = 0xA3,
        /// <summary></summary>
        LeftMenu = 0xA4,
        /// <summary></summary>
        RightMenu = 0xA5,
        /// <summary></summary>
        BrowserBack = 0xA6,
        /// <summary></summary>
        BrowserForward = 0xA7,
        /// <summary></summary>
        BrowserRefresh = 0xA8,
        /// <summary></summary>
        BrowserStop = 0xA9,
        /// <summary></summary>
        BrowserSearch = 0xAA,
        /// <summary></summary>
        BrowserFavorites = 0xAB,
        /// <summary></summary>
        BrowserHome = 0xAC,
        /// <summary></summary>
        VolumeMute = 0xAD,
        /// <summary></summary>
        VolumeDown = 0xAE,
        /// <summary></summary>
        VolumeUp = 0xAF,
        /// <summary></summary>
        MediaNextTrack = 0xB0,
        /// <summary></summary>
        MediaPrevTrack = 0xB1,
        /// <summary></summary>
        MediaStop = 0xB2,
        /// <summary></summary>
        MediaPlayPause = 0xB3,
        /// <summary></summary>
        LaunchMail = 0xB4,
        /// <summary></summary>
        LaunchMediaSelect = 0xB5,
        /// <summary></summary>
        LaunchApplication1 = 0xB6,
        /// <summary></summary>
        LaunchApplication2 = 0xB7,
        /// <summary></summary>
        OEM1 = 0xBA,
        /// <summary></summary>
        OEMPlus = 0xBB,
        /// <summary></summary>
        OEMComma = 0xBC,
        /// <summary></summary>
        OEMMinus = 0xBD,
        /// <summary></summary>
        OEMPeriod = 0xBE,
        /// <summary></summary>
        OEM2 = 0xBF,
        /// <summary></summary>
        OEM3 = 0xC0,
        /// <summary></summary>
        OEM4 = 0xDB,
        /// <summary></summary>
        OEM5 = 0xDC,
        /// <summary></summary>
        OEM6 = 0xDD,
        /// <summary></summary>
        OEM7 = 0xDE,
        /// <summary></summary>
        OEM8 = 0xDF,
        /// <summary></summary>
        OEMAX = 0xE1,
        /// <summary></summary>
        OEM102 = 0xE2,
        /// <summary></summary>
        ICOHelp = 0xE3,
        /// <summary></summary>
        ICO00 = 0xE4,
        /// <summary></summary>
        ProcessKey = 0xE5,
        /// <summary></summary>
        ICOClear = 0xE6,
        /// <summary></summary>
        Packet = 0xE7,
        /// <summary></summary>
        OEMReset = 0xE9,
        /// <summary></summary>
        OEMJump = 0xEA,
        /// <summary></summary>
        OEMPA1 = 0xEB,
        /// <summary></summary>
        OEMPA2 = 0xEC,
        /// <summary></summary>
        OEMPA3 = 0xED,
        /// <summary></summary>
        OEMWSCtrl = 0xEE,
        /// <summary></summary>
        OEMCUSel = 0xEF,
        /// <summary></summary>
        OEMATTN = 0xF0,
        /// <summary></summary>
        OEMFinish = 0xF1,
        /// <summary></summary>
        OEMCopy = 0xF2,
        /// <summary></summary>
        OEMAuto = 0xF3,
        /// <summary></summary>
        OEMENLW = 0xF4,
        /// <summary></summary>
        OEMBackTab = 0xF5,
        /// <summary></summary>
        ATTN = 0xF6,
        /// <summary></summary>
        CRSel = 0xF7,
        /// <summary></summary>
        EXSel = 0xF8,
        /// <summary></summary>
        EREOF = 0xF9,
        /// <summary></summary>
        Play = 0xFA,
        /// <summary></summary>
        Zoom = 0xFB,
        /// <summary></summary>
        Noname = 0xFC,
        /// <summary></summary>
        PA1 = 0xFD,
        /// <summary></summary>
        OEMClear = 0xFE,
        
        COUNT = 0xFF,
    }
}