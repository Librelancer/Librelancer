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
                if (CaretPosition > value.Length)
                    CaretPosition = value.Length;
                _allSelected = false;
                _text = value;
                richTextDirty = true;
            }
        }
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
            _text = chars;
            _allSelected = false;
        }
        else
        {
            if (CaretPosition == Text.Length)
            {
                _text += chars;
            }
            else
            {
                _text = Text.Insert(CaretPosition, chars);
            }
            CaretPosition += chars.Length;
        }
        richTextDirty = true;
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
                CaretPosition--;
        }
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
                CaretPosition++;
        }
    }

    public void Backspace()
    {
        if(_allSelected) {
            _text = "";
            CaretPosition = 0;
            richTextDirty = true;
            _allSelected = false;
        }
        else if (Text.Length > 0 && CaretPosition == Text.Length) {
            _text = Text.Substring(0, Text.Length - 1);
            CaretLeft();
            richTextDirty = true;
        } else if (Text.Length > 0 && CaretPosition > 0) {
            _text = Text.Remove(CaretPosition - 1, 1);
            CaretLeft();
            richTextDirty = true;
        }
    }

    public void Delete()
    {
        if(_allSelected) {
            _text = "";
            CaretPosition = 0;
            richTextDirty = true;
            _allSelected = false;
        }
        else if (Text.Length > 0 && CaretPosition < Text.Length) {
            _text = Text.Remove(CaretPosition, 1);
            if (CaretPosition > _text.Length)
                CaretPosition = _text.Length;
            richTextDirty = true;
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
        context.ScissorEnabled = true;
        context.ScissorRectangle = new Rectangle(x,y,width,height);
        Update(context.Renderer2D.CreateRichTextEngine());
        var pos = richText.GetCaretPosition(nodes.Length - 1, CaretPosition);
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
        context.ScissorEnabled = false;
    }
}
