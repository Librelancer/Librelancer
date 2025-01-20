// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using static LibreLancer.SDL2.SDL_Keycode;
namespace LibreLancer
{
    /* SDL_Scancode with entries renamed */
    public enum Keys
    {
        Unknown = 0,

        A = 4,
        B = 5,
        C = 6,
        D = 7,
        E = 8,
        F = 9,
        G = 10,
        H = 11,
        I = 12,
        J = 13,
        K = 14,
        L = 15,
        M = 16,
        N = 17,
        O = 18,
        P = 19,
        Q = 20,
        R = 21,
        S = 22,
        T = 23,
        U = 24,
        V = 25,
        W = 26,
        X = 27,
        Y = 28,
        Z = 29,

        D1 = 30,
        D2 = 31,
        D3 = 32,
        D4 = 33,
        D5 = 34,
        D6 = 35,
        D7 = 36,
        D8 = 37,
        D9 = 38,
        D0 = 39,

        Enter = 40,
        Escape = 41,
        Backspace = 42,
        Tab = 43,
        Space = 44,

        Minus = 45,
        Equals = 46,
        LeftBracket = 47,
        RightBracket = 48,
        Backslash = 49,
        NonUSHash = 50,
        Semicolon = 51,
        Apostrophe = 52,
        Grave = 53,
        Comma = 54,
        Period = 55,
        Slash = 56,

        CapsLock = 57,

        F1 = 58,
        F2 = 59,
        F3 = 60,
        F4 = 61,
        F5 = 62,
        F6 = 63,
        F7 = 64,
        F8 = 65,
        F9 = 66,
        F10 = 67,
        F11 = 68,
        F12 = 69,

        PrintScreen = 70,
        ScrollLock = 71,
        Pause = 72,
        Insert = 73,
        NavHome = 74,
        NavPageUp = 75,
        Delete = 76,
        NavEnd = 77,
        NavPageDown = 78,
        Right = 79,
        Left = 80,
        Down = 81,
        Up = 82,

        NumLockClear = 83,
        KeypadDivide = 84,
        KeypadMultiply = 85,
        KeypadMinus = 86,
        KeypadPlus = 87,
        KeypadEnter = 88,
        Keypad1 = 89,
        Keypad2 = 90,
        Keypad3 = 91,
        Keypad4 = 92,
        Keypad5 = 93,
        Keypad6 = 94,
        Keypad7 = 95,
        Keypad8 = 96,
        Keypad9 = 97,
        Keypad0 = 98,
        KeypadPeriod = 99,

        NonUSBackslash = 100,
        Application = 101,
        Power = 102,
        KeypadEquals = 103,
        F13 = 104,
        F14 = 105,
        F15 = 106,
        F16 = 107,
        F17 = 108,
        F18 = 109,
        F19 = 110,
        F20 = 111,
        F21 = 112,
        F22 = 113,
        F23 = 114,
        F24 = 115,
        Execute = 116,
        Help = 117,
        Menu = 118,
        Select = 119,
        Stop = 120,
        Again = 121,
        Undo = 122,
        Cut = 123,
        Copy = 124,
        Paste = 125,
        Find = 126,
        Mute = 127,
        VolumeUp = 128,
        VolumeDown = 129,
        KeypadComma = 133,
        KeypadEqualsAS400 = 134,

        International1 = 135,
        International2 = 136,
        International3 = 137,
        International4 = 138,
        International5 = 139,
        International6 = 140,
        International7 = 141,
        International8 = 142,
        International9 = 143,
        Lang1 = 144,
        Lang2 = 145,
        Lang3 = 146,
        Lang4 = 147,
        Lang5 = 148,
        Lang6 = 149,
        Lang7 = 150,
        Lang8 = 151,
        Lang9 = 152,

        AltErase = 153,
        SysReq = 154,
        Cancel = 155,
        Clear = 156,
        Prior = 157,
        Return2 = 158,
        Separator = 159,
        Out = 160,
        Oper = 161,
        ClearAgain = 162,
        CRSEL = 163,
        EXSEL = 164,

        Keypad00 = 176,
        Keypad000 = 177,
        KeypadThousandsSeparator = 178,
        KeypadDecimalSeparator = 179,
        KeypadCurrencyUnit = 180,
        KeypadCurrencySubunit = 181,
        KeypadLeftParen = 182,
        KeypadRightParen = 183,
        KeypadLeftBrace = 184,
        KeypadRightBrace = 185,
        KeypadTab = 186,
        KeypadBackspace = 187,
        KeypadA = 188,
        KeypadB = 189,
        KeypadC = 190,
        KeypadD = 191,
        KeypadE = 192,
        KeypadF = 193,
        KeypadXor = 194,
        KeypadPower = 195,
        KeypadPercent = 196,
        KeypadLess = 197,
        KeypadGreater = 198,
        KeypadAmpersand = 199,
        KeypadDblAmpersand = 200,
        KeypadVerticalBar = 201,
        KeypadDblVerticalBar = 202,
        KeypadColon = 203,
        KeypadHash = 204,
        KeypadSpace = 205,
        KeypadAt = 206,
        SDL_SCANCODE_KP_EXCLAM = 207,
        SDL_SCANCODE_KP_MEMSTORE = 208,
        SDL_SCANCODE_KP_MEMRECALL = 209,
        SDL_SCANCODE_KP_MEMCLEAR = 210,
        SDL_SCANCODE_KP_MEMADD = 211,
        SDL_SCANCODE_KP_MEMSUBTRACT = 212,
        SDL_SCANCODE_KP_MEMMULTIPLY = 213,
        SDL_SCANCODE_KP_MEMDIVIDE = 214,
        SDL_SCANCODE_KP_PLUSMINUS = 215,
        SDL_SCANCODE_KP_CLEAR = 216,
        SDL_SCANCODE_KP_CLEARENTRY = 217,
        KeypadBinary = 218,
        KeypadOctal = 219,
        KeypadDecimal = 220,
        KeypadHexacdecimal = 221,

        LeftControl = 224,
        LeftShift = 225,
        LeftAlt = 226,
        LeftGui = 227,
        RightControl = 228,
        RightShift = 229,
        RightAlt = 230,
        RightGui = 231,

        Mode = 257,

        AudioNext = 258,
        AudioPrev = 259,
        AudioStop = 260,
        AudioPlay = 261,
        AudioMute = 262,
        MediaSelect = 263,
        AppWww = 264,
        AppMail = 265,
        AppCalculator = 266,
        AppComputer = 267,
        AppSearch = 268,
        AppHome = 269,
        AppBack = 270,
        AppForward = 271,
        AppStop = 272,
        AppRefresh = 273,
        AppBookmarks = 274,

        BrightnessDown = 275,
        BrightnessUp = 276,
        DisplaySwitch = 277,
        KbdToggle = 278,
        KbdIllumDown = 279,
        KbdIllumUp = 280,
        Eject = 281,
        Sleep = 282,

        App1 = 283,
        App2 = 284,


        NumKeys = 512
    }
    public static class KeysExtensions
    {

        static SDL3.SDL_Scancode ToSDL3(Keys scancode)
        {
            if (scancode <= Keys.Mode) {
                // They're the same
                return (SDL3.SDL_Scancode)scancode;
            }

            switch (scancode) {
                case Keys.AudioMute:
                    return SDL3.SDL_Scancode.SDL_SCANCODE_MUTE;
                case Keys.AudioNext:
                    return SDL3.SDL_Scancode.SDL_SCANCODE_MEDIA_NEXT_TRACK;
                case Keys.AudioPlay:
                    return SDL3.SDL_Scancode.SDL_SCANCODE_MEDIA_PLAY;
                case Keys.AudioPrev:
                    return SDL3.SDL_Scancode.SDL_SCANCODE_MEDIA_PREVIOUS_TRACK;
                case Keys.AudioStop:
                    return SDL3.SDL_Scancode.SDL_SCANCODE_MEDIA_STOP;
                case Keys.Eject:
                    return SDL3.SDL_Scancode.SDL_SCANCODE_MEDIA_EJECT;
                case Keys.MediaSelect:
                    return SDL3.SDL_Scancode.SDL_SCANCODE_MEDIA_SELECT;
                default:
                    return SDL3.SDL_Scancode.SDL_SCANCODE_UNKNOWN;
            }
        }

        internal static Dictionary<Keys, string> KeyNames = new Dictionary<Keys, string>();
        private static bool UseSDL = false;
        internal static void FillKeyNamesSDL()
        {
            UseSDL = true;
            if (SDL3.Supported)
            {

            }
            else
            {
                foreach (var k in Enum.GetValues<Keys>())
                {
                    KeyNames[k] = SDL2.SDL_GetKeyName(SDL2.SDL_GetKeyFromScancode((SDL2.SDL_Scancode)k));
                }
            }

        }

        public static string GetDisplayName(this Keys k)
        {
            string n;
            if (!KeyNames.TryGetValue(k, out n))
            {
                if (SDL3.Supported)
                    n = SDL3.SDL_GetKeyName(SDL3.SDL_GetKeyFromScancode(ToSDL3(k), 0, false));
                else
                    n = SDL2.SDL_GetKeyName(SDL2.SDL_GetKeyFromScancode((SDL2.SDL_Scancode)k));
                KeyNames.Add(k, n);
            }
            return n;
        }
        static readonly SDL2.SDL_Keycode[] defaultMapping =
        {
            0, 0, 0, 0, SDLK_a, SDLK_b, SDLK_c, SDLK_d, SDLK_e, SDLK_f, SDLK_g, SDLK_h,
            SDLK_i, SDLK_j, SDLK_k, SDLK_l, SDLK_m, SDLK_n, SDLK_o, SDLK_p, SDLK_q, SDLK_r,
            SDLK_s, SDLK_t, SDLK_u, SDLK_v, SDLK_w, SDLK_x, SDLK_y, SDLK_z, SDLK_1, SDLK_2,
            SDLK_3, SDLK_4, SDLK_5, SDLK_6, SDLK_7, SDLK_8, SDLK_9, SDLK_0, SDLK_RETURN, SDLK_ESCAPE,
            SDLK_BACKSPACE, SDLK_TAB, SDLK_SPACE, SDLK_MINUS, SDLK_EQUALS, SDLK_LEFTBRACKET, SDLK_RIGHTBRACKET,
            SDLK_BACKSLASH, SDLK_HASH, SDLK_SEMICOLON, SDLK_QUOTE, SDLK_BACKQUOTE, SDLK_COMMA, SDLK_PERIOD,
            SDLK_SLASH, SDLK_CAPSLOCK, SDLK_F1, SDLK_F2, SDLK_F3, SDLK_F4, SDLK_F5, SDLK_F6, SDLK_F7, SDLK_F8, SDLK_F9,
            SDLK_F10, SDLK_F11, SDLK_F12, SDLK_PRINTSCREEN, SDLK_SCROLLLOCK, SDLK_PAUSE, SDLK_INSERT, SDLK_HOME,
            SDLK_PAGEUP, SDLK_DELETE, SDLK_END, SDLK_PAGEDOWN, SDLK_RIGHT, SDLK_LEFT, SDLK_DOWN, SDLK_UP, SDLK_NUMLOCKCLEAR,
            SDLK_KP_DIVIDE, SDLK_KP_MULTIPLY, SDLK_KP_MINUS, SDLK_KP_PLUS, SDLK_KP_ENTER, SDLK_KP_1,
            SDLK_KP_2, SDLK_KP_3, SDLK_KP_4, SDLK_KP_5, SDLK_KP_6, SDLK_KP_7, SDLK_KP_8, SDLK_KP_9, SDLK_KP_0,
            SDLK_KP_PERIOD, 0, SDLK_APPLICATION, SDLK_POWER, SDLK_KP_EQUALS, SDLK_F13, SDLK_F14, SDLK_F15,
            SDLK_F16, SDLK_F17, SDLK_F18, SDLK_F19, SDLK_F20, SDLK_F21, SDLK_F22, SDLK_F23,
            SDLK_F24, SDLK_EXECUTE, SDLK_HELP, SDLK_MENU, SDLK_SELECT, SDLK_STOP, SDLK_AGAIN, SDLK_UNDO,
            SDLK_CUT, SDLK_COPY, SDLK_PASTE, SDLK_FIND, SDLK_MUTE, SDLK_VOLUMEUP, SDLK_VOLUMEDOWN,
            0, 0, 0, SDLK_KP_COMMA, SDLK_KP_EQUALSAS400, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            SDLK_ALTERASE, SDLK_SYSREQ, SDLK_CANCEL, SDLK_CLEAR, SDLK_PRIOR, SDLK_RETURN2, SDLK_SEPARATOR,
            SDLK_OUT, SDLK_OPER, SDLK_CLEARAGAIN, SDLK_CRSEL, SDLK_EXSEL, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            SDLK_KP_00, SDLK_KP_000, SDLK_THOUSANDSSEPARATOR, SDLK_DECIMALSEPARATOR, SDLK_CURRENCYUNIT,
            SDLK_CURRENCYSUBUNIT, SDLK_KP_LEFTPAREN, SDLK_KP_RIGHTPAREN, SDLK_KP_LEFTBRACE, SDLK_KP_RIGHTBRACE,
            SDLK_KP_TAB, SDLK_KP_BACKSPACE, SDLK_KP_A, SDLK_KP_B, SDLK_KP_C, SDLK_KP_D, SDLK_KP_E, SDLK_KP_F,
            SDLK_KP_XOR, SDLK_KP_POWER, SDLK_KP_PERCENT, SDLK_KP_LESS, SDLK_KP_GREATER, SDLK_KP_AMPERSAND,
            SDLK_KP_DBLAMPERSAND, SDLK_KP_VERTICALBAR, SDLK_KP_DBLVERTICALBAR, SDLK_KP_COLON, SDLK_KP_HASH,
            SDLK_KP_SPACE, SDLK_KP_AT, SDLK_KP_EXCLAM, SDLK_KP_MEMSTORE, SDLK_KP_MEMRECALL,
            SDLK_KP_MEMCLEAR, SDLK_KP_MEMADD, SDLK_KP_MEMSUBTRACT, SDLK_KP_MEMMULTIPLY, SDLK_KP_MEMDIVIDE,
            SDLK_KP_PLUSMINUS, SDLK_KP_CLEAR, SDLK_KP_CLEARENTRY, SDLK_KP_BINARY, SDLK_KP_OCTAL, SDLK_KP_DECIMAL,
            SDLK_KP_HEXADECIMAL, 0, 0, SDLK_LCTRL, SDLK_LSHIFT, SDLK_LALT, SDLK_LGUI, SDLK_RCTRL, SDLK_RSHIFT,
            SDLK_RALT, SDLK_RGUI,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            SDLK_MODE, SDLK_AUDIONEXT, SDLK_AUDIOPREV, SDLK_AUDIOSTOP, SDLK_AUDIOPLAY, SDLK_AUDIOMUTE,
            SDLK_MEDIASELECT, SDLK_WWW, SDLK_MAIL, SDLK_CALCULATOR, SDLK_COMPUTER, SDLK_AC_SEARCH,
            SDLK_AC_HOME, SDLK_AC_BACK, SDLK_AC_FORWARD, SDLK_AC_STOP, SDLK_AC_REFRESH, SDLK_AC_BOOKMARKS,
            SDLK_BRIGHTNESSDOWN, SDLK_BRIGHTNESSUP, SDLK_DISPLAYSWITCH, SDLK_KBDILLUMTOGGLE,
            SDLK_KBDILLUMDOWN, SDLK_KBDILLUMUP, SDLK_EJECT, SDLK_SLEEP,
        };
        /// <summary>
        /// Interprets the key as a keycode and returns the current Keys for the layout
        /// </summary>
        /// <returns>A mapped keys for the symbol</returns>
        /// <param name="input">The symbol to map</param>
        public static Keys Map(this Keys input)
        {
            if (UseSDL)
            {
                var idx = (int)input;
                if (idx < 0 || idx > defaultMapping.Length)
                    return input;
                if (SDL3.Supported)
                    return (Keys)SDL3.SDL_GetScancodeFromKey((uint)defaultMapping[(int)input], IntPtr.Zero);
                return (Keys)SDL2.SDL_GetScancodeFromKey(defaultMapping[(int)input]);
            }
            return input;
        }
        internal static void ResetKeyNames() //Keyboard layout changed
        {
            KeyNames = new Dictionary<Keys, string>();
        }
    }
}

