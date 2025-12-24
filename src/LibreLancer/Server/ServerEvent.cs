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

public record CharacterConnectedEventPayload(Player ConnectedCharacter);
public record CharacterDisconnectedEventPayload(Player DisconnectedCharacter);
public record PlayerConnectedEventPayload( Player ConnectedPlayer);
public record PlayerDisconnectedEventPayload(Player DisconnectedPlayer, DisconnectReason Reason);
public record PlayerBanChangedEventPayload( Guid PlayerId, bool IsBanned, DateTime? BanToDate);
public record PlayerAdminChangedEventPayload(long Id, bool IsAdmin);

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
