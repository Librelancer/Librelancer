using LibreLancer.Ini;

namespace LibreLancer.Data.Universe.Rooms;

public class Spiels
{
    [Entry("EquipmentDealer")] 
    public string EquipmentDealer;
    [Entry("ShipDealer")] 
    public string ShipDealer;
    [Entry("CommodityDealer")] 
    public string CommodityDealer;
}