using BlurgText;
using ImGuiNET;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Text;

namespace LibreLancer.Interface;

public class TextEditBase
{
    private BuiltRichText richText;
    private bool richTextDirty = true;
    private int richTextWidth = -1;

    private string _text = "";
    private GraphemeBreak[] breaks = [];
    private string _fontName = "Arial";
    private float _fontSize = 12;
    private Color4 _fontColor = Color4.White;
    private OptionalColor _fontShadow = default(OptionalColor);

    private bool _allSelected = false;

    public bool Selected => _allSelected;

    public bool Focused { get; set; } = false;

    public string Text
    {
        get => _text;
        set {
            if (_text != value)
            {
                _allSelected = false;
                SetText(value);
            }
        }
    }

    void SetText(string v)
    {
        _text = v;
        breaks = GraphemeBreaks.Get(_text);
        richTextDirty = true;
        if (CaretPosition > _text.Length)
            CaretPosition = _text.Length;
        if (CaretPosition < 0)
            CaretPosition = 0;
    }

    private bool _wrap = true;

    public bool Wrap
    {
        get => _wrap;
        set
        {
            if (_wrap != value) {
                _wrap = value;
                richTextDirty = true;
            }
        }
    }

    private bool _mask = false;
    public bool Mask
    {
        get => _mask;
        set
        {
            if (_mask != value) {
                _mask = value;
                richTextDirty = true;
            }
        }
    }

    public string FontName
    {
        get => _fontName;
        set => TrySetValue(ref _fontName, value);
    }

    public float FontSize
    {
        get => _fontSize;
        set => TrySetValue(ref _fontSize, value);
    }

    public Color4 FontColor
    {
        get => _fontColor;
        set => TrySetValue(ref _fontColor, value);
    }

    public OptionalColor FontShadow
    {
        get => _fontShadow;
        set => TrySetValue(ref _fontShadow, value);
    }


    public int CaretPosition;

    private RichTextNode[] nodes;

    void TrySetValue(ref string target, string value)
    {
        if (target != value)
        {
            richTextDirty = true;
            target = value;
        }
        if (CaretPosition > _text.Length)
            CaretPosition = _text.Length;
    }

    void TrySetValue(ref float target, float value)
    {
        if (target != value)
        {
            richTextDirty = true;
            target = value;
        }
    }

    void TrySetValue(ref Color4 target, Color4 value)
    {
        if (target != value)
        {
            richTextDirty = true;
            target = value;
        }
    }

    void TrySetValue(ref OptionalColor target, OptionalColor value)
    {
        if (target != value)
        {
            richTextDirty = true;
            target = value;
        }
    }

    public RichTextNode LeadingNode
    {
        get => nodes[0];
        set
        {
            nodes[0] = value;
            richTextDirty = true;
        }
    }

    public TextEditBase(bool leadingNode)
    {
        if (leadingNode)
            nodes = new RichTextNode[2];
        else
            nodes = new RichTextNode[1];
    }

    public void TextEntered(string chars)
    {
        if (string.IsNullOrEmpty(chars) && !_allSelected)
            return;
        if (_allSelected)
        {
            Text = chars;
        }
        else
        {
            if (CaretPosition == Text.Length)
            {
                SetText(_text + chars);
            }
            else
            {
                SetText(_text.Insert(CaretPosition, chars));
            }
            CaretPosition += chars.Length;
        }
    }

    public void SelectAll()
    {
        if (!_allSelected)
            richTextDirty = true;
        _allSelected = true;
    }

    public void Unselect()
    {
        if (_allSelected)
            richTextDirty = true;
        _allSelected = false;
    }

    public void CaretLeft()
    {
        if (_allSelected)
        {
            CaretPosition = 0;
            _allSelected = false;
            richTextDirty = true;
        }
        else
        {
            if (CaretPosition > 0)
            {
                var x = CaretPosition - 1;
                x--;
                while (x >= 0 && breaks[x] != GraphemeBreak.Break)
                {
                    x--;
                }
                CaretPosition = x + 1;
            }
        }
    }

    int GetNextCaret()
    {
        var x = CaretPosition - 1;
        x++;
        while (x < breaks.Length && breaks[x] != GraphemeBreak.Break)
            x++;
        return x + 1;
    }

    public void CaretRight()
    {
        if (_allSelected)
        {
            CaretPosition = Text.Length;
            _allSelected = false;
            richTextDirty = true;
        }
        else
        {
            if (CaretPosition < Text.Length)
            {
                var x = CaretPosition - 1;
                x++;
                while (x < breaks.Length && breaks[x] != GraphemeBreak.Break)
                    x++;
                CaretPosition = GetNextCaret();
            }
        }
    }

    public void Backspace()
    {
        if(_allSelected)
        {
            Text = "";
        }
        else if (Text.Length > 0 && CaretPosition == Text.Length)
        {
            CaretLeft();
            SetText(Text.Substring(0, CaretPosition));
        }
        else if (Text.Length > 0 && CaretPosition > 0)
        {
            var p = CaretPosition;
            CaretLeft();
            SetText(Text.Remove(CaretPosition, p - CaretPosition));
        }
    }

    public void Delete()
    {
        if(_allSelected)
        {
            Text = "";
        }
        else if (Text.Length > 0 && CaretPosition < Text.Length)
        {
            SetText(Text.Remove(CaretPosition, GetNextCaret() - CaretPosition));
        }
    }

    private int x;
    private int y;
    private int width;
    private int height;

    public void SetRectangle(Rectangle rect) =>
        SetRectangle(rect.X, rect.Y, rect.Width, rect.Height);

    public void SetRectangle(int x, int y, int width, int height)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    void Update(RichTextEngine engine)
    {
        if (richTextDirty || (_wrap && richTextWidth != width)) {
            richText?.Dispose();
            richTextDirty = false;
            richTextWidth = width;
            nodes[^1] = new RichTextTextNode()
            {
                FontName = _fontName,
                FontSize = _fontSize,
                Contents = Mask ? new string('*', Text.Length) : Text,
                Color = _fontColor,
                Shadow = _fontShadow,
                Background = _allSelected ? new OptionalColor(Color4.CornflowerBlue) : new OptionalColor()
            };
            richText = engine.BuildText(nodes, _wrap ? richTextWidth : int.MaxValue);
        }
    }

    private const double BLINK_TIME = 0.5;

    public void Draw(RenderContext context, double globalTime)
    {
        if (!context.PushScissor(new Rectangle(x, y, width, height)))
            return;
        Update(context.Renderer2D.CreateRichTextEngine());
        Rectangle pos;
        if (nodes.Length == 1 && ((RichTextTextNode)nodes[0]).Contents == "")
            pos = new Rectangle(0, 0, 1, (int)context.Renderer2D.CreateRichTextEngine().LineHeight(_fontName, _fontSize));
        else
            pos = richText.GetCaretPosition(nodes.Length - 1, CaretPosition - 1);
        int xOffset = 0;
        if (!_wrap && pos.X >= width) {
            xOffset = 5 + pos.X - width;
        }
        context.Renderer2D.CreateRichTextEngine().RenderText(richText, x - xOffset, y);
        bool caretVisible = (globalTime % (2 * BLINK_TIME)) < BLINK_TIME;
        if (Focused && !_allSelected && caretVisible)
        {
            if (_fontShadow.Enabled)
            {
                var shadowRect = new Rectangle(x - xOffset + 2 + pos.X, y + 2 + pos.Y, pos.Width, pos.Height);
                context.Renderer2D.FillRectangle(shadowRect, _fontShadow.Color);
            }
            var caretRect = new Rectangle(x - xOffset + pos.X, y + pos.Y, pos.Width, pos.Height);
            context.Renderer2D.FillRectangle(caretRect, _fontColor);
        }
        context.PopScissor();
    }
}
