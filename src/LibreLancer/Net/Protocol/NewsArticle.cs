// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LiteNetLib;
using LiteNetLib.Utils;

namespace LibreLancer.Net
{
    public class NewsArticle
    {
        public string Icon;
        public string Logo;
        public int Category;
        public int Headline;
        public int Text;

        public void Put(NetDataWriter message)
        {
            message.PutStringPacked(Icon);
            message.PutStringPacked(Logo);
            message.Put(Category);
            message.Put(Headline);
            message.Put(Text);
        }

        public static NewsArticle Read(NetPacketReader message) => new()
        {
            Icon = message.GetStringPacked(),
            Logo = message.GetStringPacked(),
            Category = message.GetInt(),
            Headline = message.GetInt(),
            Text = message.GetInt()
        };
    }
}