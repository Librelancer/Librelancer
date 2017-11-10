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
            var view = new TextEntry();
            view.MultiLine = true;
            view.ReadOnly = true;
            view.Text = text;
            vbox.PackStart(view, true, true);
            this.Size = new Size(600, 400);
            this.nextWindow = nextWindow;
            var wrap = new HBox() { MinWidth = 600, MinHeight = 400 };
            wrap.PackStart(vbox, true, true);
            Content = wrap;
           
            CloseRequested += CrashWindow_CloseRequested;
		}

        private void CrashWindow_CloseRequested(object sender, CloseRequestedEventArgs args)
        {
            if (nextWindow)
            {
                var win = new MainWindow(nextWindow);
                win.Show();
            }
            else
            {
                Application.Exit();
            }
        }
    }
}
