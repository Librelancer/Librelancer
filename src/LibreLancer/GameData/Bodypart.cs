using LibreLancer.Utf.Dfm;

namespace LibreLancer.GameData;

public class Bodypart : IdentifiableItem
{
    public string Sex;
    public string Path;
    public DfmFile LoadModel(ResourceManager resources)
    {
        return (DfmFile)resources.GetDrawable(Path).Drawable;
    }
}
