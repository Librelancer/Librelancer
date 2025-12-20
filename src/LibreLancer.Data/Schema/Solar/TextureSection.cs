using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Solar;

[ParsedSection]
public partial class TextureSection
{
    [Entry("file", Multiline = true)]
    public List<string> Files = new();
}
