using System;
using System.Collections.Generic;
using System.Text;
using LibreLancer.Net;

namespace LibreLancer.Server;

public enum ServerEventType
{
    PlayerConnected, // network + auth ie "lobby"
    PlayerDisconnected,
    CharacterConnected, // selected character & entered game
    CharacterDisconnected,// on logout / swap

    PlayerAdminChanged,
    PlayerBanChanged,
}

public record CharacterConnectedEvent( long CharacterId, string CharacterName, string System, string Base);
public record CharacterDisconnectedEvent(long CharacterId, string CharacterName);
public record PlayerConnectedEvent( Guid PlayerId, string[] Characters);
public record PlayerDisconnectedEvent(Guid PlayerId, string[] Characters, DisconnectReason reason);
public record PlayerBanChangedEvent(Guid PlayerId, string[] Characters, bool IsBanned, DateTime? BanToDate);
public record PlayerAdminChangedEvent(long Id, string Character, bool IsAdmin);

public struct ServerEvent
{
    public ServerEventType Type;
    public DateTime TimeUtc;
    public object Payload;

    public T GetPayload<T>()
    {
        return Payload is T t
            ? t
            : throw new InvalidCastException(
                $"ServerEvent {Type} payload is not {typeof(T).Name}"
            );
    }
}
