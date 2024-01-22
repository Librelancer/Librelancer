using System.Collections.Generic;
using LibreLancer.Graphics.Text;
using LibreLancer.Infocards;
using LibreLancer.Net.Protocol;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [WattleScriptUserData]
    public class ChatSource
    {
        internal CircularBuffer<DisplayMessage> Messages = new CircularBuffer<DisplayMessage>(1000);
        public class DisplayMessage
        {
            public List<RichTextNode> Nodes;
            public float TimeAlive = 20;
        }

        [WattleScriptHidden]
        public int Version = 0;

        RichTextNode Convert(BinaryChatSegment msg, string fontName, Color4 defaultColor)
        {
            var size = msg.Size switch
            {
                ChatMessageSize.XLarge => 42,
                ChatMessageSize.Large => 32,
                ChatMessageSize.Regular => 26,
                ChatMessageSize.Small => 18,
                _ => 26
            };
            return new RichTextTextNode()
            {
                Bold = msg.Bold ?? false,
                Italic = msg.Italic ?? false,
                Underline = msg.Underline ?? false,
                Color = msg.Color.Enabled ? msg.Color.Color : defaultColor,
                Shadow = new OptionalColor(Color4.Black),
                Contents = msg.Contents,
                FontSize = size,
                FontName = fontName
            };
        }
        public void Append(BinaryChatMessage source, BinaryChatMessage msg, Color4 color, string font)
        {
            var nodes = new List<RichTextNode>();
            if (source != null)
            {
                foreach (var n in source.Segments)
                    nodes.Add(Convert(n, font, color));
            }
            foreach(var n in msg.Segments)
                nodes.Add(Convert(n,font,color));
            Messages.Enqueue(new DisplayMessage() { Nodes = nodes });
            Version++;
        }
    }
}
