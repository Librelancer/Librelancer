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
using System.Text;
using Xwt;
namespace Launcher
{
	public class CrashWindow : Window
	{
		public CrashWindow(Exception ex) : this(
			"Uh oh!",
			"Librelancer has crashed. See the log for more information.",
			GetExceptionMessage(ex)
		)
		{
            nextWindow = false;
		}
		static string GetExceptionMessage(Exception ex)
		{
			var str = new StringBuilder();
			str.AppendLine(ex.GetType().Name);
			str.AppendLine(ex.Message);
			str.AppendLine(ex.StackTrace);
			return str.ToString();
		}
        bool nextWindow;
		public CrashWindow(string title, string label, string text, bool nextWindow = false)
		{
			Title = title;
            var vbox = new VBox();
            vbox.PackStart(new Label() { Text = label });
            //24SEP18 - TextView doesn't work on Linux/Gtk with Xwt.
            var view = new RichTextView();
            view.ReadOnly = true;
            view.LoadText(text, Xwt.Formats.TextFormat.Plain);
            vbox.PackStart(view, true, true);
            this.Size = new Size(600, 400);
            this.nextWindow = nextWindow;
            var wrap = new HBox() { MinWidth = 600, MinHeight = 400 };
            wrap.PackStart(vbox, true, true);
            Content = wrap;
		}
    }
}
