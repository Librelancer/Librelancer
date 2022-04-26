using System.Collections.Generic;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [WattleScriptUserData]
    public class ChatSource
    {
        internal List<DisplayMessage> Messages = new List<DisplayMessage>(15);
        public class DisplayMessage
        {
            public CachedRenderString Cache;
            public InterfaceColor Color;
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
                    {Text = text, Font = font, Size = size, Color = new InterfaceColor() {Color = color}});
            }
        }
    }
}