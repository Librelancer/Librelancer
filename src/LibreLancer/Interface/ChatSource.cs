using System.Collections.Generic;
using LibreLancer.Infocards;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [WattleScriptUserData]
    public class ChatSource
    {
        internal List<DisplayMessage> Messages = new List<DisplayMessage>(15);
        private static int MsgId = 0;
        public class DisplayMessage
        {
            public int ID;
            public Color4 Color;
            public string Text;
            public string Font;
            public float Size;
            public float TimeAlive = 20;
        }
        
        public void Append(string text, string font, float size, Color4 color)
        {
            
            lock (Messages)
            {
                Messages.Add(new DisplayMessage()
                    {Text = text, Font = font, Size = size, Color = color, ID = MsgId++});
            }
        }
    }
}