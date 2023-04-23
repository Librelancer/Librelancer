using LibreLancer.GameData.Items;

namespace LibreLancer.GameData;

public struct BasicCargo
{
    public Equipment Item;
    public int Count;

    public BasicCargo(Equipment item, int count)
    {
        Item = item;
        Count = count;
    }
}