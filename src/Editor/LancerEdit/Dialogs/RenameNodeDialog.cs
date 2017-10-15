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
using Xwt;
namespace LancerEdit
{
	public class RenameNodeDialog : Dialog
	{
		TextEntry nameEntry;
		public static Command AcceptCommand;
		public static Command CancelCommand;
		public RenameNodeDialog(string name)
		{
			AcceptCommand = new Command(Txt._("Accept"));
			CancelCommand = new Command(Txt._("Cancel"));

			Title = Txt._("Rename");
			Buttons.Add(AcceptCommand);
			Buttons.Add(CancelCommand);
			DefaultCommand = CancelCommand;
			nameEntry = new TextEntry();
			nameEntry.Text = name;
			Content = nameEntry;

			CommandActivated += RenameNodeDialog_CommandActivated;
		}

		public string NodeName
		{
			get
			{
				return nameEntry.Text;
			}
		}

		void RenameNodeDialog_CommandActivated(object sender, DialogCommandActivatedEventArgs e)
		{
			if (e.Command != AcceptCommand)
				return;
			if (nameEntry.Text.Trim().Length <= 0)
			{
				MessageDialog.ShowError(this, Txt._("New name cannot be empty"));
				e.Handled = true;
				return;
			}
		}
	}
}
