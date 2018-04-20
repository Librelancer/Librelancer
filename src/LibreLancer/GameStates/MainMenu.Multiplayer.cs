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
using System.Collections.Generic;
using LibreLancer.Infocards;
namespace LibreLancer
{
    public partial class MainMenu
    {
        bool internetServers = false;
        GameClient netClient;
        UIServerList serverList;
        UIServerDescription serverDescription;
        UIMenuButton connectButton;
        LocalServerInfo selectedInfo;
        CharacterSelectInfo csel;

        //Character Selection
        void ConstructCharacterSelect()
        {
            manager.Elements.Clear();
            manager.AnimationComplete -= ConstructCharacterSelect;
            netClient.Disconnected += CharSelect_Disconnected;
            Vector2 buttonScale = new Vector2(1.87f, 2.5f);
            manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.70f, 0.42f), "NEW CHARACTER", CallNewCharacter) { UIScale = buttonScale });
            manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.70f, 0.24f), "LOAD CHARACTER", null) { UIScale = buttonScale });
            manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.70f, 0.06f), "DELETE CHARACTER", null) { UIScale = buttonScale });
            manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.70f, -0.12f), "SELECT ANOTHER SERVER", BackToServerList) { UIScale = buttonScale });
            manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.70f, -0.30f), "MAIN MENU", CharacterToMainMenu) { UIScale = buttonScale });
            manager.Elements.Add(new UICharacterList(manager));
            manager.PlaySound("ui_motion_swish");
            manager.FlyInAll(FLYIN_LENGTH, 0.05);
        }
        void BackToServerList()
        {
            netClient.Disconnected -= CharSelect_Disconnected;
            netClient.Stop();
            netClient.Start();
            manager.PlaySound("ui_motion_swish");
            manager.FlyOutAll(FLYIN_LENGTH, 0.05);
            manager.AnimationComplete += ConstructServerList;
        }
        void CharacterToMainMenu()
        {
            netClient.Disconnected -= CharSelect_Disconnected;
            netClient.Dispose();
            netClient = null;
            manager.PlaySound("ui_motion_swish");
            manager.FlyOutAll(FLYIN_LENGTH, 0.05);
            manager.AnimationComplete += ConstructMainMenu;
        }
        void CallNewCharacter()
        {
            netClient.RequestCharacterCreate();
        }
        void NetClient_OpenNewCharacter(int obj)
        {
            Console.WriteLine("New Character Dialog: {0} credits", obj);
        }


        void CharSelect_Disconnected(string obj)
        {
            netClient.Disconnected -= CharSelect_Disconnected;
            manager.FlyOutAll(FLYIN_LENGTH, 0.05);
            manager.PlaySound("ui_motion_swish");
            manager.AnimationComplete += ConstructServerList;
            netClient.Disconnected += ServerList_Disconnected;
        }

        //Server List
        void ConstructServerList()
        {
            manager.Elements.Clear();
            manager.AnimationComplete -= ConstructServerList;
            serverList = new UIServerList(manager) { Internet = internetServers };
            serverList.Selected += ServerList_Selected;
            manager.Elements.Add(serverList);
            manager.Elements.Add(new UIMenuButton(manager, new Vector2(0.01f, -0.55f), "SET FILTER", null));
            manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.64f, -0.55f), "MAIN MENU", ServersToMainMenu));
            manager.FlyInAll(FLYIN_LENGTH, 0.05);
            //Refresh button - from right
            var rfrsh = new UIMenuButton(manager, new Vector2(0.67f, -0.55f), "REFRESH LIST", RefreshServerList);
            rfrsh.Animation = new FlyInRight(rfrsh.UIPosition, 0, FLYIN_LENGTH);
            rfrsh.Animation.Begin();
            manager.Elements.Add(rfrsh);
            //Connect button - from right
            connectButton = new UIMenuButton(manager, new Vector2(0.67f, -0.82f), "CONNECT >");
            connectButton.Animation = new FlyInRight(connectButton.UIPosition, 0, FLYIN_LENGTH);
            connectButton.Animation.Begin();
            manager.Elements.Add(connectButton);
            //SERVER DESCRIPTION - from right
            serverDescription = new UIServerDescription(manager, -0.32f, -0.81f) { ServerList = serverList };
            serverDescription.Animation = new FlyInRight(serverDescription.UIPosition, 0, FLYIN_LENGTH);
            serverDescription.Animation.Begin();
            manager.Elements.Add(serverDescription);
            manager.PlaySound("ui_motion_swish");
            if (netClient == null)
            {
                netClient = new GameClient(Game);
                netClient.Disconnected += ServerList_Disconnected;
                netClient.ServerFound += NetClient_ServerFound;
                netClient.OpenNewCharacter += NetClient_OpenNewCharacter;
                netClient.Start();
                netClient.UUID = Game.Config.UUID.Value;
                netClient.CharacterSelection += (info) =>
                {
                    csel = info;
                    manager.FlyOutAll(FLYIN_LENGTH, 0.05);
                    manager.PlaySound("ui_motion_swish");
                    manager.AnimationComplete += ConstructCharacterSelect;
                    netClient.Disconnected -= ServerList_Disconnected;
                };
            }
            netClient.DiscoverLocalPeers();
            if (internetServers)
                netClient.DiscoverGlobalPeers();
        }
        void RefreshServerList()
        {
            connectButton.Tag = null;
            serverList.Servers.Clear();
            netClient.DiscoverLocalPeers();
            if (internetServers)
                netClient.DiscoverGlobalPeers();   
        }
        void ServersToMainMenu()
        {
            netClient.Disconnected -= ServerList_Disconnected;
            netClient.Dispose();
            netClient = null;
            manager.PlaySound("ui_motion_swish");
            manager.FlyOutAll(FLYIN_LENGTH, 0.05);
            manager.AnimationComplete += ConstructMainMenu;   
        }
        void ServerListConnect()
        {
            netClient.Connect(selectedInfo.EndPoint);
        }
        void ServerList_Selected(LibreLancer.LocalServerInfo obj)
        {
            selectedInfo = obj;
            serverDescription.Description = obj.Description;
            connectButton.Action = ServerListConnect;
        }
        void ServerList_Disconnected(string reason)
        {
            var dlg = new List<UIElement>();
            dlg.Add(new UIBackgroundElement(manager) { FillColor = new Color4(0, 0, 0, 0.25f) });
            var ifc = new Infocard();
            ifc.Nodes = new List<InfocardNode>();
            ifc.Nodes.Add(new InfocardTextNode() { Bold = true, Contents = "Disconnected" });
            ifc.Nodes.Add(new InfocardParagraphNode());
            ifc.Nodes.Add(new InfocardTextNode() { Contents = reason });
            dlg.Add(new UIMessageBox(manager, ifc));
            var x = new UIXButton(manager, 0.64f, 0.26f, 2, 2.9f);
            x.Clicked += () =>
            {
                manager.Dialog = null;
                connectButton.Action = null;
            };
            dlg.Add(x);

            manager.Dialog = dlg;
        }
        void NetClient_ServerFound(LocalServerInfo obj)
        {
            serverList.Servers.Add(obj);
        }
    }
}
