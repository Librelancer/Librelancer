// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Linq;
namespace LibreLancer
{
    public class ScriptedHud : IDisposable
    {
        public XmlUIManager UI;
        XmlChatBox chatbox;

        public ScriptedHud(object api, bool space, FreelancerGame game)
        {
            UI = new XmlUIManager(game, "game", api, game.GameData.GetInterfaceXml(space ? "hud" : "baseside"));
        }

        public void Init()
        {
            UI.OnConstruct();
            chatbox = UI.Elements.OfType<XmlChatBox>().First();
        }
        public void SetManeuver(string action)
        {
            if (UI.Events["onnavchange"] != null)
                UI.Events.onnavchange(action);
        }
        public void Update(TimeSpan delta) => UI.Update(delta);
        public void Draw(TimeSpan delta) => UI.Draw(delta);
        public void Dispose() => UI.Dispose();

        public bool TextEntry
        {
            get { return chatbox.Visible; }
            set { chatbox.Visible = value; }
        }
        public void OnTextEntry(string e)
        {
            chatbox.AppendText(e);
        }

        public event Action<string> OnEntered;

        public void TextEntryKeyPress(Keys k)
        {
            if (k == Keys.Enter)
            {
                TextEntry = false;
                if (OnEntered != null)
                    OnEntered(chatbox.CurrentText);
                chatbox.CurrentText = "";
            }
            if (k == Keys.Backspace)
            {
                if (chatbox.CurrentText.Length > 0)
                    chatbox.CurrentText = chatbox.CurrentText.Substring(0, chatbox.CurrentText.Length - 1);
            }
            if (k == Keys.Escape)
            {
                TextEntry = false;
                chatbox.CurrentText = "";
            }
        }
    }
}
