// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections;
using System.Collections.Generic;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class Stylesheet
    {
        [UiContent] public StyleCollection Styles { get; set; } = new();
    }

    [WattleScriptUserData]
    public class StyleCollection : IList
    {
        private List<XmlStyle> allStyles = new();
        private Dictionary<Type, XmlStyle> defaultStyles = new();
        private Dictionary<string, XmlStyle> namedStyles = new();

        IEnumerator IEnumerable.GetEnumerator() => allStyles.GetEnumerator();

        void ICollection.CopyTo(Array array, int index) => ((IList)allStyles).CopyTo(array, index);

        int ICollection.Count => allStyles.Count;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => allStyles;

        public void Add(XmlStyle style)
        {
            if (string.IsNullOrWhiteSpace(style.Name))
            {
                defaultStyles[style.GetType()] = style;
            }
            else
            {
                namedStyles[style.Name] = style;
            }
            allStyles.Add(style);
        }

        public T? DefaultStyle<T>() where T : XmlStyle
        {
            if (defaultStyles.TryGetValue(typeof(T), out var style))
                return (T)style;
            return null;
        }

        public XmlStyle? Get(string name)
        {
            if (namedStyles.TryGetValue(name, out var style))
                return style;
            return null;
        }

        XmlStyle Arg(object? value)
        {
            if (value is null)
                throw new InvalidOperationException("Argument cannot be null");
            if (value is not XmlStyle style)
                throw new InvalidOperationException("Argument must derive from XmlStyle");
            return style;
        }


        int IList.Add(object? value)
        {
            Add(Arg(value));
            return allStyles.Count - 1;
        }

        void IList.Clear()
        {
            allStyles = new();
            defaultStyles = new();
            namedStyles = new();
        }

        bool IList.Contains(object? value) => allStyles.Contains(Arg(value));

        int IList.IndexOf(object? value) => allStyles.IndexOf(Arg(value));

        void IList.Insert(int index, object? value) => allStyles.Insert(index, Arg(value));

        void IList.Remove(object? value) => throw new InvalidOperationException("Cannot remove element");

        void IList.RemoveAt(int index) => throw new InvalidOperationException("Cannot remove element");

        bool IList.IsFixedSize => false;

        bool IList.IsReadOnly => true;

        object? IList.this[int index]
        {
            get => allStyles[index];
            set => throw new InvalidOperationException("Cannot write to element at index");
        }
    }

    public class XmlStyle
    {
        public string Name { get; set; } = null!;
    }

    [UiLoadable]
    [WattleScriptUserData]
    public class ElementStyle : XmlStyle, IStyle
    {
        protected StyledProperty<float> WidthProperty = new("Width");
        protected StyledProperty<float> HeightProperty = new("Height");
        protected StyledProperty<UiRenderable?> BackgroundProperty = new("Background");
        protected StyledProperty<UiRenderable?> BorderProperty = new("Border");

        public float Width
        {
            get => WidthProperty.Value;
            set => WidthProperty.Set(value);
        }

        public float Height
        {
            get => HeightProperty.Value;
            set => HeightProperty.Set(value);
        }

        public UiRenderable? Background
        {
            get => BackgroundProperty.Value;
            set => BackgroundProperty.Set(value);
        }

        public UiRenderable? Border
        {
            get => BorderProperty.Value;
            set => BorderProperty.Set(value);
        }


        public virtual void Set(StyleResolver resolver) =>
            resolver.Add(WidthProperty)
                .Add(HeightProperty)
                .Add(BackgroundProperty)
                .Add(BorderProperty);

        public virtual void Create(StyleResolver resolver) =>
            resolver.Query(WidthProperty)
                .Query(HeightProperty)
                .Query(BackgroundProperty)
                .Query(BorderProperty);
    }

    [UiLoadable]
    [WattleScriptUserData]
    public class ButtonStyle : ElementStyle
    {
        private StyledProperty<string> mouseEnterSound = new("MouseEnterSound");
        private StyledProperty<string> mouseDownSound = new("MouseDownSound");
        private StyledProperty<ButtonAppearance> normal = new("Normal");
        private StyledProperty<ButtonAppearance> hover = new("Hover");
        private StyledProperty<ButtonAppearance> pressed = new("Pressed");
        private StyledProperty<ButtonAppearance> selected = new("Selected");
        private StyledProperty<ButtonAppearance> disabled = new("Disabled");

        public override void Set(StyleResolver resolver)
        {
            base.Set(resolver);
            resolver.Add(mouseEnterSound)
                .Add(mouseDownSound)
                .Add(normal)
                .Add(hover)
                .Add(pressed)
                .Add(selected)
                .Add(disabled);
        }

        public override void Create(StyleResolver resolver)
        {
            base.Create(resolver);
            resolver.Query(mouseEnterSound)
                .Query(mouseDownSound)
                .Query(normal)
                .Query(hover)
                .Query(pressed)
                .Query(selected)
                .Query(disabled);
        }

        public string? MouseEnterSound
        {
            get => mouseEnterSound.Value;
            set => mouseEnterSound.Set(value);
        }

        public string? MouseDownSound
        {
            get => mouseDownSound.Value;
            set => mouseDownSound.Set(value);
        }

        public ButtonAppearance? Normal
        {
            get => normal.Value;
            set => normal.Set(value);
        }

        public ButtonAppearance? Hover
        {
            get => hover.Value;
            set => hover.Set(value);
        }

        public ButtonAppearance? Pressed
        {
            get => pressed.Value;
            set => pressed.Set(value);
        }

        public ButtonAppearance? Selected
        {
            get => selected.Value;
            set => selected.Set(value);
        }

        public ButtonAppearance? Disabled
        {
            get => disabled.Value;
            set => disabled.Set(value);
        }
    }

    [UiLoadable]
    [WattleScriptUserData]
    public class ButtonAppearance : IStyle
    {
        private StyledProperty<UiRenderable?> background = new("Background");
        private StyledProperty<UiRenderable?> border = new("Border");
        private StyledProperty<float> textSize = new("TextSize");
        private StyledProperty<float> marginLeft = new("MarginLeft");
        private StyledProperty<float> marginRight = new("MarginRight");
        private StyledProperty<string> fontFamily = new("FontFamily");
        private StyledProperty<HorizontalAlignment> horizontalAlignment = new("HorizontalAlignment");
        private StyledProperty<VerticalAlignment> verticalAlignment = new("VerticalAlignment");
        private StyledProperty<InterfaceColor> textColor = new("TextColor");
        private StyledProperty<InterfaceColor?> textShadow = new("TextShadow");

        void IStyle.Set(StyleResolver resolver) =>
            resolver.Add(background)
                .Add(border)
                .Add(textSize)
                .Add(marginLeft)
                .Add(marginRight)
                .Add(fontFamily)
                .Add(horizontalAlignment)
                .Add(verticalAlignment)
                .Add(textColor)
                .Add(textShadow);

        void IStyle.Create(StyleResolver resolver) =>
            resolver.Query(background)
                .Query(border)
                .Query(textSize)
                .Query(marginLeft)
                .Query(marginRight)
                .Query(fontFamily)
                .Query(horizontalAlignment)
                .Query(verticalAlignment)
                .Query(textColor)
                .Query(textShadow);

        public UiRenderable? Background
        {
            get => background.Value;
            set => background.Set(value);
        }

        public UiRenderable? Border
        {
            get => border.Value;
            set => border.Set(value);
        }

        public float TextSize
        {
            get => textSize.Value;
            set => textSize.Set(value);
        }

        public float MarginLeft
        {
            get => marginLeft.Value;
            set => marginLeft.Set(value);
        }

        public float MarginRight
        {
            get => marginRight.Value;
            set => marginRight.Set(value);
        }

        public string? FontFamily
        {
            get => fontFamily.Value;
            set => fontFamily.Set(value);
        }

        public HorizontalAlignment HorizontalAlignment
        {
            get => horizontalAlignment.Value;
            set => horizontalAlignment.Set(value);
        }

        public VerticalAlignment VerticalAlignment
        {
            get => verticalAlignment.Value;
            set => verticalAlignment.Set(value);
        }

        public InterfaceColor? TextColor
        {
            get => textColor.Value;
            set => textColor.Set(value);
        }

        public InterfaceColor? TextShadow
        {
            get => textShadow.Value;
            set => textShadow.Set(value);
        }
    }

    [UiLoadable]
    [WattleScriptUserData]
    public class ScrollbarStyle : ElementStyle
    {
        private StyledProperty<ButtonStyle?> upButton = new("UpButton");
        private StyledProperty<ButtonStyle?> downButton = new("DownButton");
        private StyledProperty<ButtonStyle?> thumb = new("Thumb");
        private StyledProperty<ButtonStyle?> thumbTop = new("ThumbTop");
        private StyledProperty<ButtonStyle?> thumbBottom = new("ThumbBottom");
        private StyledProperty<UiRenderable?> trackArea = new("TrackArea");
        private StyledProperty<float> buttonMarginX = new("ButtonMarginX");
        private StyledProperty<float> trackMarginX = new("TrackMarginX");
        private StyledProperty<float> trackMarginY = new("TrackMarginY");

        public override void Set(StyleResolver resolver)
        {
            base.Set(resolver);
            resolver.Add(upButton)
                .Add(downButton)
                .Add(thumb)
                .Add(thumbTop)
                .Add(thumbBottom)
                .Add(trackArea)
                .Add(buttonMarginX)
                .Add(trackMarginX)
                .Add(trackMarginY);
        }

        public override void Create(StyleResolver resolver)
        {
            base.Create(resolver);
            resolver.Query(upButton)
                .Query(downButton)
                .Query(thumb)
                .Query(thumbTop)
                .Query(thumbBottom)
                .Query(trackArea)
                .Query(buttonMarginX)
                .Query(trackMarginX)
                .Query(trackMarginY);
        }


        public ButtonStyle? UpButton
        {
            get => upButton.Value;
            set => upButton.Set(value);
        }

        public ButtonStyle? DownButton
        {
            get => downButton.Value;
            set => downButton.Set(value);
        }

        public ButtonStyle? Thumb
        {
            get => thumb.Value;
            set => thumb.Set(value);
        }

        public ButtonStyle? ThumbTop
        {
            get => thumbTop.Value;
            set => thumbTop.Set(value);
        }

        public ButtonStyle? ThumbBottom
        {
            get => thumbBottom.Value;
            set => thumbBottom.Set(value);
        }

        public UiRenderable? TrackArea
        {
            get => trackArea.Value;
            set => trackArea.Set(value);
        }

        public float ButtonMarginX
        {
            get => buttonMarginX.Value;
            set =>  buttonMarginX.Set(value);
        }

        public float TrackMarginX
        {
            get => trackMarginX.Value;
            set => trackMarginX.Set(value);
        }

        public float TrackMarginY
        {
            get => trackMarginY.Value;
            set => trackMarginY.Set(value);
        }
    }

    [UiLoadable]
    [WattleScriptUserData]
    public class RolloverStyle : XmlStyle
    {
        public string? Font { get; set; } = null;
        public InterfaceColor? TextColor { get; set; } = null;
        public InterfaceColor? TextShadow { get; set; } = null;
        public UiRenderable? Background { get; set; } = null;
        public UiRenderable? Border { get; set; } = null;
    }

    [UiLoadable]
    [WattleScriptUserData]
    public class TooltipStyle : XmlStyle
    {
        public string? Font { get; set; } = null;
        public InterfaceColor? TextColor { get; set; } = null;
        public InterfaceColor? TextShadow { get; set; } = null;
        public UiRenderable? Background { get; set; } = null;
        public UiRenderable? Border { get; set; } = null;
        public float OffsetY { get; set; }
    }

    [UiLoadable]
    [WattleScriptUserData]
    public class HSliderStyle : ElementStyle
    {
        private StyledProperty<ButtonStyle?> leftButton = new("LeftButton");
        private StyledProperty<ButtonStyle?> rightButton = new("RightButton");
        private StyledProperty<ButtonStyle?> thumb = new("Thumb");

        private StyledProperty<ButtonStyle?> thumbLeft = new("ThumbLeft");
        private StyledProperty<ButtonStyle?> thumbRight = new("ThumbRight");

        private StyledProperty<UiRenderable?> trackArea = new("TrackArea");
        private StyledProperty<float> buttonMarginY = new("ButtonMarginY");
        private StyledProperty<float> trackMarginX = new("TrackMarginX");
        private StyledProperty<float> trackMarginY = new("TrackMarginY");

        public override void Set(StyleResolver resolver)
        {
            base.Set(resolver);
            resolver
                .Add(leftButton)
                .Add(rightButton)
                .Add(thumb)
                .Add(thumbLeft)
                .Add(thumbRight)
                .Add(trackArea)
                .Add(buttonMarginY)
                .Add(trackMarginX)
                .Add(trackMarginY);
        }

        public override void Create(StyleResolver resolver)
        {
            base.Create(resolver);
            resolver
                .Query(leftButton)
                .Query(rightButton)
                .Query(thumb)
                .Query(thumbLeft)
                .Query(thumbRight)
                .Query(trackArea)
                .Query(buttonMarginY)
                .Query(trackMarginX)
                .Query(trackMarginY);
        }

        public ButtonStyle? LeftButton
        {
            get => leftButton.Value;
            set => leftButton.Set(value);
        }

        public ButtonStyle? RightButton
        {
            get => rightButton.Value;
            set => rightButton.Set(value);
        }

        public ButtonStyle? Thumb
        {
            get => thumb.Value;
            set => thumb.Set(value);
        }

        public ButtonStyle? ThumbLeft
        {
            get => thumbLeft.Value;
            set => thumbLeft.Set(value);
        }

        public ButtonStyle? ThumbRight
        {
            get => thumbRight.Value;
            set => thumbRight.Set(value);
        }

        public UiRenderable? TrackArea
        {
            get => trackArea.Value;
            set => trackArea.Set(value);
        }

        public float ButtonMarginY
        {
            get => buttonMarginY.Value;
            set => buttonMarginY.Set(value);
        }

        public float TrackMarginX
        {
            get => trackMarginX.Value;
            set => trackMarginX.Set(value);
        }

        public float TrackMarginY
        {
            get => trackMarginY.Value;
            set => trackMarginY.Set(value);
        }
    }

    [UiLoadable]
    [WattleScriptUserData]
    public class NavmapStyle : ElementStyle
    {
        private StyledProperty<ButtonStyle?> zoomInButton = new("ZoomInButton");
        private StyledProperty<ButtonStyle?> zoomOutButton = new("ZoomOutButton");
        private StyledProperty<ButtonStyle?> selectorButton = new("SelectorButton");
        private StyledProperty<ButtonStyle?> addWaypointButton = new("AddWaypointButton");
        private StyledProperty<ButtonStyle?> bestPathButton = new("BestPathButton");
        private StyledProperty<float> userWaypointSize = new("UserWaypointSize", 28);
        private StyledProperty<float> userWaypointDigitWidth = new("UserWaypointDigitWidth", 6);
        private StyledProperty<float> userWaypointDigitHeight = new("UserWaypointDigitHeight", 10);
        private StyledProperty<int> userWaypointRouteThickness = new("UserWaypointRouteThickness", 2);
        private StyledProperty<InterfaceColor> userWaypointColor = new("UserWaypointColor", new Color4(1f, 0.2f, 1f, 1f));
        private StyledProperty<InterfaceColor> userWaypointDigitColor = new("UserWaypointDigitColor", Color4.Yellow);

        public override void Set(StyleResolver resolver)
        {
            base.Set(resolver);
            resolver
                .Add(zoomInButton)
                .Add(zoomOutButton)
                .Add(selectorButton)
                .Add(addWaypointButton)
                .Add(bestPathButton)
                .Add(userWaypointSize)
                .Add(userWaypointDigitWidth)
                .Add(userWaypointDigitHeight)
                .Add(userWaypointRouteThickness)
                .Add(userWaypointColor)
                .Add(userWaypointDigitColor);
        }

        public override void Create(StyleResolver resolver)
        {
            base.Create(resolver);
            resolver
                .Query(zoomInButton)
                .Query(zoomOutButton)
                .Query(selectorButton)
                .Query(addWaypointButton)
                .Query(bestPathButton)
                .Query(userWaypointSize)
                .Query(userWaypointDigitWidth)
                .Query(userWaypointDigitHeight)
                .Query(userWaypointRouteThickness)
                .Query(userWaypointColor)
                .Query(userWaypointDigitColor);
        }

        public ButtonStyle? ZoomInButton
        {
            get => zoomInButton.Value;
            set => zoomInButton.Set(value);
        }

        public ButtonStyle? ZoomOutButton
        {
            get => zoomOutButton.Value;
            set => zoomOutButton.Set(value);
        }

        public ButtonStyle? SelectorButton
        {
            get => selectorButton.Value;
            set => selectorButton.Set(value);
        }

        public ButtonStyle? AddWaypointButton
        {
            get => addWaypointButton.Value;
            set => addWaypointButton.Set(value);
        }

        public ButtonStyle? BestPathButton
        {
            get => bestPathButton.Value;
            set => bestPathButton.Set(value);
        }

        public float UserWaypointSize
        {
            get => userWaypointSize.Value;
            set => userWaypointSize.Set(value);
        }

        public float UserWaypointDigitWidth
        {
            get => userWaypointDigitWidth.Value;
            set => userWaypointDigitWidth.Set(value);
        }

        public float UserWaypointDigitHeight
        {
            get => userWaypointDigitHeight.Value;
            set => userWaypointDigitHeight.Set(value);
        }

        public int UserWaypointRouteThickness
        {
            get => userWaypointRouteThickness.Value;
            set => userWaypointRouteThickness.Set(value);
        }

        public InterfaceColor UserWaypointColor
        {
            get => userWaypointColor.Value!;
            set => userWaypointColor.Set(value);
        }

        public InterfaceColor UserWaypointDigitColor
        {
            get => userWaypointDigitColor.Value!;
            set => userWaypointDigitColor.Set(value);
        }
    }
}
