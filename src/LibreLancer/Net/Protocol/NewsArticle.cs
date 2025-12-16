// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using WattleScript.Interpreter;

namespace LibreLancer.Net.Protocol
{
    [WattleScriptUserData]
    public class NewsArticle
    {
        public string Icon;
        public string Logo;
        public int Headline;
        public int Text;

        public void Put(PacketWriter message)
        {
            message.Put(Icon);
            message.Put(Logo);
            message.PutVariableUInt32((uint)Headline);
            message.PutVariableUInt32((uint)Text);
        }

        public static NewsArticle Read(PacketReader message) => new()
        {
            Icon = message.GetString(),
            Logo = message.GetString(),
            Headline = (int)message.GetVariableUInt32(),
            Text = (int)message.GetVariableUInt32()
        };
    }
}
