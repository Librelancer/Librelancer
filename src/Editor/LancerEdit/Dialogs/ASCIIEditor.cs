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
using XwtPlus.TextEditor;
namespace LancerEdit
{
	public class ASCIIEditor : Dialog
	{
		public static Command AcceptCommand;
		public static Command CancelCommand;
		TextEditor entry;
		public ASCIIEditor(string data)
		{
			entry = new TextEditor();
			entry.Document.Text = data;
			entry.ASCIIOnly = true;
			Title = "ASCII editor";
			var vbox = new VBox();
			vbox.PackStart(entry, true, true);
			Content = vbox;

			AcceptCommand = new Command(Txt._("Accept"));
			CancelCommand = new Command(Txt._("Cancel"));

			Buttons.Add(AcceptCommand);
			Buttons.Add(CancelCommand);

			Width = 250;
			Height = 250;
		}
		public string Data
		{
			get
			{
				return entry.Document.Text;
			}
		}
	
	}
}
