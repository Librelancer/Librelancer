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
using System.Text;
using System.Linq;
using System.IO;
using System.Threading;
using LibreLancer.Infocards;
using LibreLancer.Compatibility;
using LibreLancer.Compatibility.GameData;
using Xwt;
namespace LancerEdit
{
	public class DataExplorerPage : LTabPage
	{
		FreelancerData sourceData;
		TextEntry directoryEntry;
		IMainWindow window;
		string freelancerDir;
		VBox picker;
		VBox loadScreen;
		public DataExplorerPage(IMainWindow window) : base("Data Explorer")
		{
			picker = new VBox();
			var hbox = new HBox();
			directoryEntry = new TextEntry();
			hbox.PackStart(directoryEntry, true, true);
			var dirPickButton = new Button() { Label = "..." };
			dirPickButton.Clicked += DirPickButton_Clicked;
			hbox.PackStart(dirPickButton);
			picker.PackStart(hbox);
			var hbox2 = new HBox();
			var loadButton = new Button() { Label = "Load" };
			loadButton.Clicked += LoadButton_Clicked;
			hbox2.PackEnd(loadButton);
			picker.PackStart(hbox2);
			PackStart(picker, true, true);
			this.window = window;
		}

		void DirPickButton_Clicked(object sender, EventArgs e)
		{
			var dlg = new SelectFolderDialog();
			if (dlg.Run() == true)
			{
				directoryEntry.Text = dlg.Folder;
			}
		}

		void LoadButton_Clicked(object sender, EventArgs e)
		{
			var dir = directoryEntry.Text;
			if (!Directory.Exists(dir))
			{
				MessageDialog.ShowError("Directory does not exist: " + dir);
				return;
			}
			if (!LibreLancer.GameConfig.CheckFLDirectory(dir))
			{
				MessageDialog.ShowError("Not a valid Freelancer directory");
				return;
			}
			//Set loading screen
			loadScreen = new VBox();
			Remove(picker);
			var hbox = new HBox();
			var sp = new Spinner();
			sp.Animate = true;
			hbox.PackStart(sp, true, true);
			hbox.PackStart(new Label() { Text = "Loading..." });
			loadScreen.PackStart(hbox);
			hbox.VerticalPlacement = WidgetPlacement.Center;
			hbox.HorizontalPlacement = WidgetPlacement.Center;
			this.PackStart(loadScreen, true, true);

			//Load data
			freelancerDir = dir;
			new Thread(() =>
			{
				try
				{
					VFS.Init(freelancerDir);
					var flini = new FreelancerIni();
					sourceData = new FreelancerData(flini);
					sourceData.LoadDacom = false;
					sourceData.LoadData();
					window.QueueUIThread(OnLoadSuccess);
				}
				catch (Exception ex)
				{
					loadException = ex;
					window.QueueUIThread(OnLoadError);
				}

			}).Start();
		}

		Exception loadException;
		void OnLoadError()
		{
			Remove(loadScreen);
			var builder = new StringBuilder();
			builder.AppendLine(loadException.Message);
			builder.AppendLine(loadException.StackTrace);
			if (loadException.InnerException != null)
			{
				builder.AppendLine("----");
				builder.AppendLine(loadException.InnerException.Message);
				builder.AppendLine(loadException.InnerException.StackTrace);
			}
			var rtv = new RichTextView();
			rtv.ReadOnly = true;
			rtv.LoadText(builder.ToString(), Xwt.Formats.TextFormat.Plain);
			PackStart(rtv, true, true);
		}

		VBox viewBox;

		ComboBox selector;
		ISortableList sortable;
		Widget currentTable;
		HPaned paned;
		RichTextView infocardView;
		void OnLoadSuccess()
		{
			Remove(loadScreen);
			viewBox = new VBox();
			selector = new ComboBox();
			selector.Items.Add("Ships");
			selector.Items.Add("Thrusters");
			selector.Items.Add("Bases");
			selector.SelectedIndex = 0;
			selector.SelectionChanged += Selector_SelectionChanged;
			viewBox.PackStart(selector);
			infocardView = new RichTextView();
			infocardView.BackgroundColor = Xwt.Drawing.Colors.DarkSlateBlue;
			paned = new HPaned();
			paned.Panel2.Content = infocardView;
			paned.Panel1.Content = null;
            paned.Panel1.Shrink = true;
            paned.Panel2.Shrink = true;
            paned.Panel1.Resize = true;
            paned.Panel2.Resize = true;
			LoadShips();
			PackStart(viewBox, true, true);
		}

		void Selector_SelectionChanged(object sender, EventArgs e)
		{
			Action[] selections = {
				LoadShips,
				LoadThrusters,
				LoadBases
			};
			selections[selector.SelectedIndex]();
		}

		ListViewColumn ConstructColumn(string name, IDataField field)
		{
			var column = new ListViewColumn(name, CellView.GetDefaultCellView(field));
			column.CanResize = true;
			return column;
		}

		int infocardIndex = -1;

		void Unpack()
		{
			if (currentTable == null) return;
			infocardView.LoadText("", Xwt.Formats.TextFormat.Plain);
			if (infocardIndex == -1)
			{
				viewBox.Remove(currentTable);
			}
			else
			{
				paned.Panel1.Content = null;
				viewBox.Remove(paned);
			}
		}

		void PackWithInfocard()
		{
			paned.Panel1.Content = currentTable;
			viewBox.PackStart(paned, true, true);
		}

		void PackNoInfocard()
		{
			infocardIndex = -1;
			viewBox.PackStart(currentTable, true, true);
		}

		void LoadShips()
		{
			Unpack();
			var tab = window.ConstructList();

			tab.AddColumn("Display Name");
			tab.AddColumn("Nickname");
			tab.AddColumn("Hitpoints");
			tab.AddColumn("Mass");
			tab.AddColumn("IdsName");
			tab.AddColumn("IdsInfo");

			foreach (var ship in sourceData.Ships.Ships)
			{
				var infos = new List<string>();
				if (ship.IdsInfo1 != null) infos.Add(ship.IdsInfo1.ToString());
				infos.Add(ship.IdsInfo.ToString());

				tab.AddRow(
					sourceData.Infocards.GetStringResource(ship.IdsName),
					ship.Nickname,
					ship.Hitpoints,
					ship.Mass,
					ship.IdsName,
					string.Join(";",infos)
				);
			}
			sortable = tab;
			tab.SelectionChanged += Tab_SelectionChanged;
			currentTable = tab.GetWidget();
			infocardIndex = 5;
			PackWithInfocard();
		}

		void LoadBases()
		{
			Unpack();
			var tab = window.ConstructList();

			tab.AddColumn("Display Name");
			tab.AddColumn("Nickname");
			tab.AddColumn("System");
			tab.AddColumn("System Nickname");
			tab.AddColumn("IdsName");
			tab.AddColumn("IdsInfo");

			foreach (var b in sourceData.Universe.Bases)
			{
				var s = sourceData.Universe.FindSystem(b.System);
				tab.AddRow(
					b.StridName,
					b.Nickname,
					s.StridName,
					s.Nickname,
					0,
					0
				);
			}
			sortable = tab;
			currentTable = tab.GetWidget();
			PackNoInfocard();
		}

		void Tab_SelectionChanged()
		{
			var selection = sortable.GetSelectedRow();
			if (selection == null) return;
			var inf = selection[infocardIndex];
			if (inf is int)
			{
				infocardView.LoadText(InfocardTools.InfocardToMarkup(sourceData.Infocards.GetXmlResource((int)inf)), Xwt.Formats.TextFormat.Markup);
			}
			else
			{
				var infocards = ((string)inf).Split(';').Select((x) => int.Parse(x));
				var str = "";
				foreach (var i in infocards)
					str += InfocardTools.InfocardToMarkup(sourceData.Infocards.GetXmlResource(i));
				infocardView.LoadText(str, Xwt.Formats.TextFormat.Markup);
			}
		}

		void LoadThrusters()
		{
			Unpack();
			var tab = window.ConstructList();
			tab.AddColumn("Display Name");
			tab.AddColumn("Nickname");
			tab.AddColumn("Max Force");
			tab.AddColumn("Power Usage");
			tab.AddColumn("Particles");
			tab.AddColumn("HpParticles");
			tab.AddColumn("Hitpoints");
			tab.AddColumn("DaArchetype");
			tab.AddColumn("Material Library");
			tab.AddColumn("IdsName");
			tab.AddColumn("IdsInfo");
			foreach (var eq in sourceData.Equipment.Equip)
			{
				var thruster = (eq as LibreLancer.Compatibility.GameData.Equipment.Thruster);
				if (thruster == null) continue;
				tab.AddRow(
					sourceData.Infocards.GetStringResource(thruster.IdsName),
					thruster.Nickname,
					thruster.MaxForce,
					thruster.PowerUsage,
					thruster.Particles,
					thruster.HpParticles,
					thruster.Hitpoints,
					thruster.DaArchetype,
					thruster.MaterialLibrary,
					thruster.IdsName,
					thruster.IdsInfo
				);
			}
			sortable = tab;
			tab.SelectionChanged += Tab_SelectionChanged;
			currentTable = tab.GetWidget();
			infocardIndex = 10;
			PackWithInfocard();
		}
	}
}
