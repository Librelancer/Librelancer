namespace LibreLancer.Data.GameData;

public struct BasicCargo
{
    public Items.Equipment Item;
    public int Count;

    public BasicCargo(Items.Equipment item, int count)
    {
        Item = item;
        Count = count;
    }
}