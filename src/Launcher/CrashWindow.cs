// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and confiditons defined in
// LICENSE, which is part of this source code package

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
