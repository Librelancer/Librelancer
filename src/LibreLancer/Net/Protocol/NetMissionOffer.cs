// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using WattleScript.Interpreter;

namespace LibreLancer.Net.Protocol
{
    [WattleScriptUserData]
    public struct NetMissionOffer
    {
        public int Id;
        public int NpcIdsName;
        public int FactionIdsName;
        public int SystemIdsName;
        public int Reward;
        public string MissionType;

        public void Put(PacketWriter message)
        {
            message.PutVariableInt32(Id);
            message.PutVariableInt32(NpcIdsName);
            message.PutVariableInt32(FactionIdsName);
            message.PutVariableInt32(SystemIdsName);
            message.PutVariableInt32(Reward);
            message.Put(MissionType);
        }

        public static NetMissionOffer Read(PacketReader message) => new()
        {
            Id = message.GetVariableInt32(),
            NpcIdsName = message.GetVariableInt32(),
            FactionIdsName = message.GetVariableInt32(),
            SystemIdsName = message.GetVariableInt32(),
            Reward = message.GetVariableInt32(),
            MissionType = message.GetString()!
        };
    }
}
