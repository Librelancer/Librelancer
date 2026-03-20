// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class Stylesheet
    {
        [UiContent]
        public List<XmlStyle> Styles { get; set; } = [];

        public T? Lookup<T>(string? name) where T : XmlStyle
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            {
                return DefaultStyle<T>();
            }

            var correct = Styles.OfType<T>().FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return correct ?? DefaultStyle<T>();
        }

        private T? DefaultStyle<T>() where T:  XmlStyle => Styles.OfType<T>().FirstOrDefault(x => string.IsNullOrEmpty(x.Name));
    }

    public class XmlStyle
    {
       public string Name { get; set; } = null!;
    }

    [UiLoadable]
    [WattleScriptUserData]
    public class ButtonStyle : XmlStyle
    {

        public float Width { get; set; }
        public float Height { get; set; }
        public string MouseEnterSound { get; set; } = null!;
        public string MouseDownSound { get; set; } = null!;
        public ButtonAppearance Normal { get; set; } = null!;
        public ButtonAppearance Hover { get; set; } = null!;
        public ButtonAppearance Pressed { get; set; } = null!;
        public ButtonAppearance Selected { get; set; } = null!;
        public ButtonAppearance Disabled { get; set; } = null!;
    }
    [UiLoadable]
    [WattleScriptUserData]
    public class ButtonAppearance
    {
        public UiRenderable Background { get; set; } = null!;
        public UiRenderable Border { get; set; } = null!;
        public float TextSize { get; set; }
        public float MarginLeft { get; set; }
        public float MarginRight { get; set; }
        public string FontFamily { get; set; } = null!;
        public HorizontalAlignment HorizontalAlignment { get; set; }
        public VerticalAlignment VerticalAlignment { get; set; }
        public InterfaceColor TextColor { get; set; } = null!;
        public InterfaceColor TextShadow { get; set; } = null!;
    }

    [UiLoadable]
    [WattleScriptUserData]
    public class ScrollbarStyle : XmlStyle
    {
        public ButtonStyle UpButton { get; set; } = null!;
        public ButtonStyle DownButton { get; set; } = null!;
        public ButtonStyle Thumb { get; set; } = null!;

        public ButtonStyle ThumbTop { get; set; } = null!;

        public ButtonStyle ThumbBottom { get; set; } = null!;
        public UiRenderable Background { get; set; } = null!;
        public UiRenderable TrackArea { get; set; } = null!;
        public float ButtonMarginX { get; set; }
        public float TrackMarginX { get; set; }
        public float TrackMarginY { get; set; }
        public float Width { get; set; }
    }

    [UiLoadable]
    [WattleScriptUserData]
    public class HSliderStyle : XmlStyle
    {
        public ButtonStyle? LeftButton { get; set; }
        public ButtonStyle? RightButton { get; set; }
        public ButtonStyle? Thumb { get; set; }

        public ButtonStyle? ThumbLeft { get; set; }
        public ButtonStyle? ThumbRight { get; set; }

        public UiRenderable? Background { get; set; } = null!;
        public UiRenderable? TrackArea { get; set; } = null!;
        public float ButtonMarginY { get; set; }
        public float TrackMarginX { get; set; }
        public float TrackMarginY { get; set; }
        public float Height { get; set; }
    }
}
