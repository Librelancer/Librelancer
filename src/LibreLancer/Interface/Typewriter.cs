// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Media;
using LibreLancer.Sounds;

namespace LibreLancer.Interface
{
    public sealed class TypewriterStyle
    {
        public string FontNickname { get; init; } = "MissionObjective";
        public float FontSize { get; init; } = 16;
        public Vector2 Position { get; init; } = new(30, 30);
        public Color4 Color { get; init; } = Color4.LightGreen;
        public Color4 ShadowColor { get; init; } = Color4.Black;
        public Vector2 ShadowOffset { get; init; } = new(2, 2);
        public double TypingDuration { get; init; } = 3;
        public double DisplayDuration { get; init; } = 5;
        public double CharactersPerSecond { get; init; }
        public bool Uppercase { get; init; }

        public static readonly TypewriterStyle LocationEntry = new()
        {
            FontSize = 37,
            Position = new Vector2(60, 200),
            Color = new Color4(0.627451f, 0.76862746f, 0.8235294f, 1),
            CharactersPerSecond = 18,
            DisplayDuration = 4,
            Uppercase = true
        };
    }

    public class Typewriter
    {
        private static readonly TypewriterStyle DefaultStyle = new();
        private readonly Queue<(string Text, TypewriterStyle Style)> strings = new();
        private readonly Game game;
        private double time;
        private string? currentString;
        private TypewriterStyle currentStyle = new();
        private SoundInstance? typingSound;

        public Typewriter(Game game)
        {
            this.game = game;
        }

        private void SetString(string str, TypewriterStyle style)
        {
            typingSound?.Stop();
            currentString = style.Uppercase ? str.ToUpperInvariant() : str;
            currentStyle = style;
            time = 0;
        }

        public void PlayString(string str, TypewriterStyle? style = null)
        {
            strings.Enqueue((str, style ?? DefaultStyle));
        }

        public void Clear()
        {
            strings.Clear();
            currentString = null;
            time = 0;
            typingSound?.Stop();
            typingSound = null;
        }

        public void Update(double delta)
        {
            bool playSound = false;
            while (strings.Count > 0)
            {
                var entry = strings.Dequeue();
                SetString(entry.Text, entry.Style);
                playSound = true;
            }
            if (playSound)
            {
                var sm = game.GetService<SoundManager>();
                typingSound = sm?.GetInstance("ui_typing_characters");
                typingSound?.Play();
            }

            if (currentString != null)
            {
                time += delta;
                if (time >= TypingTime())
                {
                    typingSound?.Stop();
                    typingSound = null;
                }
                if (time >= currentStyle.DisplayDuration)
                    currentString = null;
            }
            else
                time = 0;
        }

        private double TypingTime() => currentStyle.CharactersPerSecond > 0
            ? currentString!.Length / currentStyle.CharactersPerSecond
            : currentStyle.TypingDuration;

        public void Render()
        {
            if (string.IsNullOrEmpty(currentString))
                return;

            var displayedCharacters = currentStyle.CharactersPerSecond > 0
                ? (int)(time * currentStyle.CharactersPerSecond)
                : (int)(time / currentStyle.TypingDuration * currentString.Length);
            var str = currentString.Substring(0, MathHelper.Clamp(displayedCharacters, 0, currentString.Length));
            if (string.IsNullOrWhiteSpace(str))
                return;

            var fonts = game.GetService<FontManager>()!;
            var font = fonts.ResolveNickname(currentStyle.FontNickname);
            var drawList = game.RenderContext.Renderer2D.CreateDrawList();
            drawList.DrawStringBaseline(font, currentStyle.FontSize, str,
                currentStyle.Position + currentStyle.ShadowOffset, currentStyle.ShadowColor);
            drawList.DrawStringBaseline(font, currentStyle.FontSize, str, currentStyle.Position, currentStyle.Color);
            drawList.Render();
        }
    }
}
