namespace LibreLancer.Data.GameData;

public struct BasicCargo
{
    public Items.Equipment Item;
    public int Count;
    public string? Hardpoint;

    public BasicCargo(Items.Equipment item, int count, string? hardpoint = null)
    {
        Item = item;
        Count = count;
        Hardpoint = hardpoint;
    }
}
