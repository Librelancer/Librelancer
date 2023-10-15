namespace LibreLancer.Net;

public interface IPacketClient : IPacketSender
{
    void Disconnect(DisconnectReason reason);
}
