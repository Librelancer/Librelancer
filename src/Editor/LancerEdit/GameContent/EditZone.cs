using LibreLancer.Data.GameData.World;

namespace LancerEdit.GameContent;

public class EditZone
{
    public bool Visible = false;

    public Zone Original;
    public Zone Current;

    public EditZone() {}

    public EditZone(Zone og)
    {
        Original = og;
        Current = og.Clone();
    }

    public bool CheckDirty() => !Original.ZonesEqual(Current);

}
