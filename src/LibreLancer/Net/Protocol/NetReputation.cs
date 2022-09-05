using LiteNetLib;
using LiteNetLib.Utils;

namespace LibreLancer.Net
{
    public struct NetReputation
    {
        public uint FactionHash;
        public float Reputation;

        public void Put(NetDataWriter message)
        {
            message.Put(FactionHash);
            message.Put((short) (Reputation * 32767));
        }

        public static NetReputation Read(NetPacketReader message) => new()
        {
            FactionHash = message.GetUInt(),
            Reputation = message.GetShort() / 32767.0f,
        };
    }
}