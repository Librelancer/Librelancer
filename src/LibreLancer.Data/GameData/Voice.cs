using LibreLancer.Data.Schema.Voices;

namespace LibreLancer.Data.GameData;

public class Voice : IdentifiableItem
{
    public FLGender Gender = FLGender.unset;
    public string[] Scripts = [];
}
