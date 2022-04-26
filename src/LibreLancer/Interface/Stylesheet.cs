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
        public List<XmlStyle> Styles { get; set; } = new List<XmlStyle>();

        public T Lookup<T>(string name) where T : XmlStyle
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
                return DefaultStyle<T>();
            var correct = Styles.OfType<T>().FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (correct is null) return DefaultStyle<T>();
            return correct;
        }
        T DefaultStyle<T>() where T:  XmlStyle => Styles.OfType<T>().FirstOrDefault(x => string.IsNullOrEmpty(x.Name));
    }

    public class XmlStyle
    {
       public string Name { get; set; }
    }

    [UiLoadable]
    [WattleScriptUserData]
    public class ButtonStyle : XmlStyle
    {
        
        public float Width { get; set; }
        public float Height { get; set; }
        public string MouseEnterSound { get; set; }
        public string MouseDownSound { get; set; }
        public ButtonAppearance Normal { get; set; }
        public ButtonAppearance Hover { get; set; }
        public ButtonAppearance Pressed { get; set; }
        public ButtonAppearance Selected { get; set; }
        public ButtonAppearance Disabled { get; set; }
    }
    [UiLoadable]
    [WattleScriptUserData]
    public class ButtonAppearance
    {
        public UiRenderable Background { get; set; }
        public UiRenderable Border { get; set; }
        public float TextSize { get; set; }
        public float MarginLeft { get; set; }
        public float MarginRight { get; set; }
        public string FontFamily { get; set; }
        public HorizontalAlignment HorizontalAlignment { get; set; }
        public VerticalAlignment VerticalAlignment { get; set; }
        public InterfaceColor TextColor { get; set; }
        public InterfaceColor TextShadow { get; set; }
    }
    
    [UiLoadable]
    [WattleScriptUserData]
    public class ScrollbarStyle : XmlStyle
    {
        public ButtonStyle UpButton { get; set; }
        public ButtonStyle DownButton { get; set; }
        public ButtonStyle Thumb { get; set; }
        
        public ButtonStyle ThumbTop { get; set; }
        
        public ButtonStyle ThumbBottom { get; set; }
        public UiRenderable Background { get; set; }
        public UiRenderable TrackArea { get; set; }
        public float ButtonMarginX { get; set; }
        public float TrackMarginX { get; set; }
        public float TrackMarginY { get; set; }
        public float Width { get; set; }
    }
    
    [UiLoadable]
    [WattleScriptUserData]
    public class HSliderStyle : XmlStyle
    {
        public ButtonStyle LeftButton { get; set; }
        public ButtonStyle RightButton { get; set; }
        public ButtonStyle Thumb { get; set; }
        
        public ButtonStyle ThumbLeft { get; set; }
        public ButtonStyle ThumbRight { get; set; }
        
        public UiRenderable Background { get; set; }
        public UiRenderable TrackArea { get; set; }
        public float ButtonMarginY { get; set; }
        public float TrackMarginX { get; set; }
        public float TrackMarginY { get; set; }
        public float Height { get; set; }
    }
}