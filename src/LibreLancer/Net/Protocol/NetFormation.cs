using System.Numerics;

namespace LibreLancer.Net.Protocol
{
    public struct NetFormation
    {
        public bool Exists;
        public int LeadShip;
        public int[] Followers;
        public Vector3 YourPosition;

        public void Put(PacketWriter message)
        {
            message.Put(Exists);
            if (Exists)
            {
                message.PutVariableInt32(LeadShip);
            }
            message.PutVariableUInt32((uint) Followers.Length);
            foreach (var f in Followers)
                message.PutVariableInt32(f);
            message.Put(YourPosition);
        }

        public static NetFormation Read(PacketReader message)
        {
            var exists = message.GetBool();
            if (exists)
            {
                var leadship = message.GetVariableInt32();
                var followers = new int[message.GetVariableUInt32()];
                for (int i = 0; i < followers.Length; i++) followers[i] = message.GetVariableInt32();
                return new NetFormation()
                {
                    Exists = true, LeadShip = leadship, Followers = followers,
                    YourPosition = message.GetVector3()
                };
            }
            return new NetFormation();
        }
    }
}
