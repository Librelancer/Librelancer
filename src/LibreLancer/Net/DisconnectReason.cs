using LibreLancer.Net.Protocol;
using LiteNetLib.Utils;

namespace LibreLancer.Net;

public enum DisconnectReason : byte
{
    Unknown = 0,
    TokenRequired = 1,
    ConnectionError = 2,
    LoginError = 3,
    Banned = 4,
    Kicked = 5,
    InvalidEndpoint = 6,
    MaxValue
}