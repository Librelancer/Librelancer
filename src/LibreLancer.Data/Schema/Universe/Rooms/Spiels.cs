using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe.Rooms;

[ParsedSection]
public partial class Spiels
{
    [Entry("EquipmentDealer")]
    public string? EquipmentDealer;
    [Entry("ShipDealer")]
    public string? ShipDealer;
    [Entry("CommodityDealer")]
    public string? CommodityDealer;
}
