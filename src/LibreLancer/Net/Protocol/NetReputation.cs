namespace LibreLancer.Net.Protocol
{
    public struct NetReputation
    {
        public uint FactionHash;
        public float Reputation;

        public void Put(PacketWriter message)
        {
            message.Put(FactionHash);
            message.Put((short) (Reputation * 32767));
        }

        public static NetReputation Read(PacketReader message) => new()
        {
            FactionHash = message.GetUInt(),
            Reputation = message.GetShort() / 32767.0f,
        };
    }
}