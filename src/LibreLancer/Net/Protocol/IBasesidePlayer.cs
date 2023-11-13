using System.Threading.Tasks;

namespace LibreLancer.Net.Protocol;

[RPCInterface]
public interface IBasesidePlayer
{
    Task<bool> PurchaseGood(string item, int count);
    Task<bool> SellGood(int id, int count);
    Task<ShipPackageInfo> GetShipPackage(int package);
    Task<ShipPurchaseStatus> PurchaseShip(int package, MountId[] mountedPlayer, MountId[] mountedPackage, SellCount[] sellPlayer, SellCount[] sellPackage);
    Task<bool> Unmount(string hardpoint);
    Task<bool> Mount(int id);
}
