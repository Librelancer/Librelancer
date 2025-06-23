namespace LibreLancer.Net.Protocol;

[RPCInterface]

public interface ISpacePlayer
{
    void RequestDock(ObjNetId id);
    void FireMissiles(MissileFireCmd[] missiles);
    void EnterFormation(int ship);
    void LeaveFormation();
    void UseRepairKits();
    void UseShieldBatteries();
    void Tractor(ObjNetId target);
    void RunDirectiveIndex(int index);
}
