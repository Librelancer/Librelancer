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
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Collections.Generic;
namespace LibreLancer
{
	public class UIServerList : HudModelElement, IUIContainer
	{
		const int NUM_ROWS = 8;

		//TODO: Make these configurable?
		const int FREELANCER_SERVERS_INFOCARD = 393330;
		const int SERVER_LIST_INFOCARD = 393341;
		const int LAN_GAMES_ONLY_INFOCARD = 393711;
		const int INTERNET_AND_LAN_GAMES_INFOCARD = 393710;

		const int IP_ADDRESS_STR = 1861;
		const int VISITED_STR = 1862;
		const int NAME_STR = 1863;
		const int PING_STR = 1864;
		const int PLAYERS_STR = 1865;
		const int VERSION_STR = 1866;
		const int LAN_STR = 1867;
		const int OPTIONS_STR = 1868;

		string title_text;
		string server_list_text;
		string lan_games_only_text;
		string internet_and_lan_games_text;

		float[] dividerPositions = {
			0.252f,
			0.46f,
			0.55f,
			0.63f,
			0.73f,
			0.82f,
			0.88f
		};
		public bool Internet = false;
		public List<LocalServerInfo> Servers = new List<LocalServerInfo>();
		public Action<LocalServerInfo> Selected;

		class ServerListContent : IGridContent
		{
			UIServerList list;
			public ServerListContent(UIServerList lst)
			{
				list = lst;
			}

			public int Count
			{
				get
				{
					return list.Servers.Count;
				}
			}

			int selected = -1;
			public int Selected
			{
				get
				{
					return selected;
				}

				set
				{
					selected = value;
					list.Selected(list.Servers[value]);
				}
			}

			public string GetContentString(int row, int column)
			{
				var srv = list.Servers[row];
				switch (column)
				{
					case 0:
						return srv.Name;
					case 1:
						return srv.EndPoint.Address.ToString();
					case 2:
						return  "NO";
					case 4:
						return string.Format("{0}/{1}", srv.CurrentPlayers, srv.MaxPlayers);
					case 5:
						return "0.1";
					case 6:
						return "YES";
				}
				return null;
			}
		}

		GridControl grid;

		public UIServerList(UIManager manager) : base(manager, "../INTRO/OBJECTS/front_serverselect.cmp", 0.04f, 0.1f, 1.91f, 2.49f)
		{
			//load in all the resources!
			title_text = manager.Game.GameData.GetInfocard(FREELANCER_SERVERS_INFOCARD).ExtractText(); //HACK: These should be rendered using InfocardDisplay
			server_list_text = manager.Game.GameData.GetInfocard(SERVER_LIST_INFOCARD).ExtractText();
			lan_games_only_text = manager.Game.GameData.GetInfocard(LAN_GAMES_ONLY_INFOCARD).ExtractText();
			internet_and_lan_games_text = manager.Game.GameData.GetInfocard(INTERNET_AND_LAN_GAMES_INFOCARD).ExtractText();

			var ip_label = manager.Game.GameData.GetString(IP_ADDRESS_STR);
			var visited_label = manager.Game.GameData.GetString(VISITED_STR);
			var name_label = manager.Game.GameData.GetString(NAME_STR);
			var ping_label = manager.Game.GameData.GetString(PING_STR);
			var players_label = manager.Game.GameData.GetString(PLAYERS_STR);
			var version_label = manager.Game.GameData.GetString(VERSION_STR);
			var lan_label = manager.Game.GameData.GetString(LAN_STR);
			var options_label = manager.Game.GameData.GetString(OPTIONS_STR);

			var columnTitles = new string[]{
				name_label, ip_label, visited_label, ping_label, players_label, version_label, lan_label, options_label
			};
			grid = new GridControl(manager, dividerPositions, columnTitles, GetServerListRectangle, GetFonts, new ServerListContent(this), NUM_ROWS);
		}

		GridFonts fonts = new GridFonts();
		float lastRowSize = -1;
		GridFonts GetFonts(float rowSize)
		{
			if (lastRowSize != rowSize)
			{
				lastRowSize = rowSize;
				if (fonts.HeaderFont != null) fonts.HeaderFont.Dispose();
				if (fonts.ContentFont != null) fonts.ContentFont.Dispose();
				var pts = (rowSize * 0.8f) * (72.0f / 96.0f);
				fonts.HeaderFont = Font.FromSystemFont(Manager.Game.Renderer2D, "Agency FB", pts);
				fonts.ContentFont = Font.FromSystemFont(Manager.Game.Renderer2D, "Arial Unicode MS", pts * 0.7f);
			}
			return fonts;
		}

		public GridFonts GetFontsCached()
		{
			return fonts;
		}
		public void Dispose()
		{
			if (fonts.HeaderFont != null) fonts.HeaderFont.Dispose();
			if (fonts.ContentFont != null) fonts.ContentFont.Dispose();
		}

		protected override void UpdateInternal(TimeSpan time)
		{
			grid.Update();
			base.UpdateInternal(time);
		}

		public override void DrawText()
		{
			var rect = GetServerListRectangle();
			//Title
			var fntTitle = Manager.GetButtonFontCached(); //TODO: HUGE HACK
			var measured = Manager.Game.Renderer2D.MeasureString(fntTitle, title_text);
			var ofpX = IdentityCamera.Instance.ScreenToPixel(-1f, 0.9f);
			DrawShadowedText(fntTitle, title_text, rect.X + (rect.Width / 2) - (measured.X / 2), rect.Y - ofpX.Y * 3.4f, Manager.TextColor);
			//"SELECT A SERVER"
			DrawShadowedText(fntTitle, server_list_text, rect.X, rect.Y - ofpX.Y * 2.3f, Manager.TextColor);
			//Grid
			grid.Draw();
		}

		Rectangle GetServerListRectangle()
		{
			var tl = IdentityCamera.Instance.ScreenToPixel(Position.X - 0.47f * Scale.X, Position.Y + 0.1f * Scale.Y);
			var br = IdentityCamera.Instance.ScreenToPixel(Position.X + 0.47f * Scale.X, Position.Y - 0.16f * Scale.Y);
			return new Rectangle(
				(int)tl.X,
				(int)tl.Y,
				(int)(br.X - tl.X),
				(int)(br.Y - tl.Y)
			);
		}

		public IEnumerable<UIElement> GetChildren()
		{
			return grid.GetChildren();
		}
	}
}
