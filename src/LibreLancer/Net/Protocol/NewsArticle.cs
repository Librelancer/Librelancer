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
        public int Category;
        public int Headline;
        public int Text;

        public void Put(PacketWriter message)
        {
            message.Put(Icon);
            message.Put(Logo);
            message.Put(Category);
            message.Put(Headline);
            message.Put(Text);
        }

        public static NewsArticle Read(PacketReader message) => new()
        {
            Icon = message.GetString(),
            Logo = message.GetString(),
            Category = message.GetInt(),
            Headline = message.GetInt(),
            Text = message.GetInt()
        };
    }
}