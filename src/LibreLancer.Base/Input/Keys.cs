// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using static LibreLancer.SDL.SDL_Keycode;
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
        internal static Dictionary<Keys, string> KeyNames = new Dictionary<Keys, string>();
        private static bool UseSDL = false;
        internal static void FillKeyNamesSDL()
        {
            UseSDL = true;
            foreach (var k in Enum.GetValues<Keys>())
            {
                KeyNames[k] = SDL.SDL_GetKeyName(SDL.SDL_GetKeyFromScancode((SDL.SDL_Scancode)k));
            }
        }

        public static string GetDisplayName(this Keys k)
        {
            string n;
            if (!KeyNames.TryGetValue(k, out n))
            {
                n = SDL.SDL_GetKeyName(SDL.SDL_GetKeyFromScancode((SDL.SDL_Scancode)k));
                KeyNames.Add(k, n);
            }
            return n;
        }
        //TODO: Finish this table
        static readonly SDL.SDL_Keycode[] defaultMapping =
        {
            0,0,0,0,SDLK_a, SDLK_b, SDLK_c, SDLK_d, SDLK_e, SDLK_f, SDLK_g, SDLK_h,
            SDLK_i, SDLK_j, SDLK_k, SDLK_l, SDLK_m, SDLK_n, SDLK_o, SDLK_p, SDLK_q, SDLK_r,
            SDLK_s, SDLK_t, SDLK_u, SDLK_v, SDLK_w, SDLK_x, SDLK_y, SDLK_z,SDLK_1, SDLK_2,
            SDLK_3, SDLK_4, SDLK_5, SDLK_6, SDLK_7, SDLK_8, SDLK_9, SDLK_0,SDLK_RETURN,SDLK_ESCAPE,
            SDLK_BACKSPACE, SDLK_TAB, SDLK_SPACE, SDLK_MINUS, SDLK_EQUALS, SDLK_LEFTBRACKET, SDLK_RIGHTBRACKET,
            SDLK_BACKSLASH, SDLK_HASH, SDLK_SEMICOLON, SDLK_QUOTE, SDLK_BACKQUOTE, SDLK_COMMA, SDLK_PERIOD,
            SDLK_SLASH, SDLK_CAPSLOCK, SDLK_F1, SDLK_F2, SDLK_F3, SDLK_F4, SDLK_F5, SDLK_F6, SDLK_F7, SDLK_F8, SDLK_F9,
            SDLK_F10, SDLK_F11, SDLK_F12, SDLK_PRINTSCREEN, SDLK_SCROLLLOCK, SDLK_PAUSE, SDLK_INSERT, SDLK_HOME,
            SDLK_PAGEUP, SDLK_DELETE, SDLK_END, SDLK_PAGEDOWN, SDLK_RIGHT, SDLK_LEFT, SDLK_DOWN, SDLK_UP, SDLK_NUMLOCKCLEAR,
            SDLK_KP_DIVIDE, SDLK_KP_MULTIPLY, SDLK_KP_MINUS, SDLK_KP_PLUS, SDLK_KP_ENTER, SDLK_KP_1,
            SDLK_KP_2, SDLK_KP_3, SDLK_KP_4, SDLK_KP_5, SDLK_KP_6, SDLK_KP_7, SDLK_KP_8, SDLK_KP_9, SDLK_KP_0,
            SDLK_KP_PERIOD
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
                return (Keys)SDL.SDL_GetScancodeFromKey(defaultMapping[(int)input]);
            }
            return input;
        }
        internal static void ResetKeyNames() //Keyboard layout changed
        {
            KeyNames = new Dictionary<Keys, string>();
        }
    }
}

