using LibreLancer.Data;
using LibreLancer.Data.GameData;

namespace LibreLancer.Missions;

public class CostumeEntry
{
    public Bodypart Head;
    public Bodypart Body;
    public Accessory Accessory;

    public CostumeEntry() { }

    public CostumeEntry(string[] source, GameItemDb db)
    {
        if (source != null)
        {
            if (source.Length > 0 && source[0] != "no_head") Head = db.Bodyparts.Get(source[0]);
            if (source.Length > 1) Body = db.Bodyparts.Get(source[1]);
            if (source.Length > 2) Accessory = db.Accessories.Get(source[2]);
        }
    }
}
