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
    void ClosedPopup(string id);
    void StoryNPCSelect(string name, string room, string _base);
    void RTCMissionAccepted();
    void RTCMissionRejected();
    void Respawn();
    [Channel(1)]
    void ChatMessage(ChatCategory category, BinaryChatMessage message);

    void UpdateWeaponGroup(NetWeaponGroup wg);
}
