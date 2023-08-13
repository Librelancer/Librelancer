using LibreLancer.GameData.World;

namespace LancerEdit;

public class EditZone
{
    public bool Visible = false;
    public bool Dirty = false;
    
    public Zone Original;
    public Zone Current;

    public EditZone() {}

    public EditZone(Zone og)
    {
        Original = og;
        Current = og.Clone();
    }

    public void Reset()
    {
        Original.CopyTo(Current);
        Dirty = false;
    }
}