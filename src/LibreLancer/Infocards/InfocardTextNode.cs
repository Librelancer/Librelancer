using LibreLancer.Graphics.Text;

namespace LibreLancer.Infocards;

public class InfocardTextNode : InfocardNode
{
    public bool? Bold;
    public bool? Italic;
    public bool? Underline;
    public int? FontIndex;
    public Color4? Color;
    public TextAlignment? Alignment;
    public string? Contents;
    // Not available in RDL, used for internal UI
    public FontDescription? ManualFont;
}
