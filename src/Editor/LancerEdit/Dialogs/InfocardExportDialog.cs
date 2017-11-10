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
using System.IO;
using System.Text;
using Xwt;
using L = LibreLancer.Compatibility;
namespace LancerEdit
{
	public class InfocardExportDialog : Window
	{
		TextEntry inputEntry;
		TextEntry outputEntry;
		public InfocardExportDialog()
		{
			var mainBox = new VBox();

			var inputBox = new HBox();
			inputEntry = new TextEntry();
			var lblInput = new Label() { Text = "Freelancer Directory: " };
			var btnInput = new Button() { Label = "..." };
			btnInput.Clicked += (sender, e) =>
			{
				var dlg = new SelectFolderDialog();
				if (dlg.Run() == true)
				{
					inputEntry.Text = dlg.Folder;
				}
			};
			inputBox.PackStart(lblInput);
			inputBox.PackStart(inputEntry, true, true);
			inputBox.PackStart(btnInput);
			mainBox.PackStart(inputBox);

			var outputBox = new HBox();
			outputEntry = new TextEntry();
			var lblOutput = new Label() { Text = "Output File: " };
			var btnOutput = new Button() { Label = "..." };
			btnOutput.Clicked += (sender, e) =>
			{
				var sfd = new SaveFileDialog();
				sfd.Filters.Add(new FileDialogFilter("JSON Files", "*.json"));
				sfd.Filters.Add(new FileDialogFilter("All Files", "*.*"));
				if (sfd.Run() == true)
				{
					outputEntry.Text = sfd.FileName;
				}
			};
			outputBox.PackStart(lblOutput);
			outputBox.PackStart(outputEntry, true, true);
			outputBox.PackStart(btnOutput);
			mainBox.PackStart(outputBox);

			var doBox = new HBox();
			var btnDoStrings = new Button() { Label = "Export Strings" };
			btnDoStrings.Clicked += BtnDo_Clicked;
			doBox.PackEnd(btnDoStrings);

			var btnDoInfocards = new Button() { Label = "Export Infocards" };
			btnDoInfocards.Clicked += BtnDoInfocards_Clicked;
			doBox.PackEnd(btnDoInfocards);
			mainBox.PackEnd(doBox);

			Content = mainBox;
			Title = "Infocard Export";
		}

		void BtnDo_Clicked(object sender, EventArgs e)
		{
			if (!LibreLancer.GameConfig.CheckFLDirectory(inputEntry.Text))
			{
				MessageDialog.ShowError("Not a valid Freelancer directory!");
				return;
			}
			L.VFS.Init(inputEntry.Text);
			var ini = new L.GameData.FreelancerIni();
			if (ini.Resources == null)
			{
				MessageDialog.ShowError("This install does not use DLL resources");
				return;
			}
			var infocards = new L.GameData.InfocardManager(ini.Resources);
			infocards.ExportStrings(outputEntry.Text);
			MessageDialog.ShowMessage("Done!");
			Close();
		}

		void BtnDoInfocards_Clicked(object sender, EventArgs e)
		{
			if (!LibreLancer.GameConfig.CheckFLDirectory(inputEntry.Text))
			{
				MessageDialog.ShowError("Not a valid Freelancer directory!");
				return;
			}
			L.VFS.Init(inputEntry.Text);
			var ini = new L.GameData.FreelancerIni();
			var infocards = new L.GameData.InfocardManager(ini.Resources);
			infocards.ExportInfocards(outputEntry.Text);
			MessageDialog.ShowMessage("Done!");
			Close();
		}
	}
}
