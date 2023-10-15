using System.Threading.Tasks;

namespace LibreLancer.Net.Protocol;

[RPCInterface]
public interface IServerPlayer
{
    void Launch();
    void RTCComplete(string rtc);
    void LineSpoken(uint hash);
    void OnLocationEnter(string _base, string room);
    void RequestCharacterDB();
    Task<bool> SelectCharacter(int index);
    Task<bool> DeleteCharacter(int index);
    Task<bool> CreateNewCharacter(string name, int index);
    Task<bool> PurchaseGood(string item, int count);
    Task<bool> SellGood(int id, int count);
    Task<ShipPackageInfo> GetShipPackage(int package);
    Task<ShipPurchaseStatus> PurchaseShip(int package, MountId[] mountedPlayer, MountId[] mountedPackage, SellCount[] sellPlayer, SellCount[] sellPackage);
    void RequestDock(ObjNetId id);
    void FireProjectiles(ProjectileSpawn[] projectiles);
    void FireMissiles(MissileFireCmd[] missiles);
    Task<bool> Unmount(string hardpoint);
    Task<bool> Mount(int id);
    void ClosedPopup(string id);
    void StoryNPCSelect(string name, string room, string _base);
    void RTCMissionAccepted();
    void RTCMissionRejected();
    void Respawn();
    void ChatMessage(ChatCategory category, string message);
    void EnterFormation(int ship);
    void LeaveFormation();
}
