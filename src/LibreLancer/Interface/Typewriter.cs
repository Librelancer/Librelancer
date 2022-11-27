// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Sounds;

namespace LibreLancer.Interface
{
    public class Typewriter
    {
        private Queue<string> strings = new Queue<string>();
        private Game game;
        public Typewriter(Game game)
        {
            this.game = game;
        }

        private const double DISPLAY_MAX = 5;
        private const double FULL_TIME = 3;
        private double time = 0;
        private string currentString = null;
        
        void SetString(string str)
        {
            currentString = str;
        }

        public void PlayString(string str)
        {
            strings.Enqueue(str);
        }
        public void Update(double delta)
        {
            bool playSound = false;
            while (strings.Count > 0)
            {
                SetString(strings.Dequeue());
                playSound = true;
            }
            if (playSound)
            {
                var sm = game.GetService<SoundManager>();
                sm?.PlayOneShot("ui_typing_characters");
            }

            if (currentString != null)
            {
                time += delta;
                if (time >= DISPLAY_MAX)
                {
                    currentString = null;
                }
            }
            else
                time = 0;
        }

        public void Render()
        {
            if (!string.IsNullOrEmpty(currentString)) {
               
                var pct = MathHelper.Clamp(time / FULL_TIME, 0, 1);
                var str = currentString.Substring(0,
                    MathHelper.Clamp((int) (pct * currentString.Length), 0, currentString.Length));
                if (!string.IsNullOrWhiteSpace(str))
                {
                    var fonts = game.GetService<FontManager>();
                    var fnt = fonts.ResolveNickname("MissionObjective");
                    var r2d = game.RenderContext.Renderer2D;
                    var off = new Vector2(30,30);
                    var shadow = off + new Vector2(2, 2);
                    r2d.DrawString(fnt, 16, str, shadow, Color4.Black);
                    r2d.DrawString(fnt, 16, str, off, Color4.LightGreen);
                }
            }
        }
    }
}