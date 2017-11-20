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
using Xwt;
namespace LibreLancer.FxDebugger
{
	public class LaunchWindow : Window
	{
		public TextEntry textInput;
		public LaunchWindow()
		{
			var vbox = new VBox();
			vbox.PackStart(new Label() { Text = "Freelancer Directory:" });
			var pathHBox = new HBox();
			textInput = new TextEntry();
			textInput.Text = Program.Config.FreelancerPath;
			pathHBox.PackStart(textInput, true, true);
			var selectDir = new Button() { Label = "..." };
			selectDir.Clicked += SelectDir_Clicked;
			pathHBox.PackStart(selectDir);
			vbox.PackStart(pathHBox);
			var launchBox = new HBox();
			var btnLaunch = new Button("Launch");
			btnLaunch.Clicked += BtnLaunch_Clicked;
			launchBox.PackStart(btnLaunch);
			vbox.PackEnd(launchBox);
			Content = vbox;
		}

		protected override void OnClosed()
		{
			Application.Exit();
		}

		void SelectDir_Clicked(object sender, EventArgs e)
		{
			var dlg = new SelectFolderDialog();
			if (dlg.Run() == true)
			{
				textInput.Text = dlg.Folder;
			}
		}

		void BtnLaunch_Clicked(object sender, EventArgs e)
		{
			if (Directory.Exists(textInput.Text))
			{
				if (!LibreLancer.GameConfig.CheckFLDirectory(textInput.Text))
				{
					MessageDialog.ShowError(this, "Not a valid freelancer directory");
					return;
				}
				Program.Config.FreelancerPath = textInput.Text;
				Program.Start = true;
				Close();
			}
			else
			{
				MessageDialog.ShowError(this, "Path does not exist");
			}
		}
	}
}
