/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
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
