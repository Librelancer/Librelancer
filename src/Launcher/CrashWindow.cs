using System;
using System.Text;
using Eto.Forms;
namespace Launcher
{
	public class CrashWindow : Form
	{
		public CrashWindow(Exception ex)
		{
			Title = "Uh oh!";
			var str = new StringBuilder();
			str.AppendLine(ex.GetType().Name);
			str.AppendLine(ex.Message);
			str.AppendLine(ex.StackTrace);
			Content = new TableLayout(
				new Label() { Text = "Librelancer has crashed. See the log for more information." },
				new TextArea() { Text = str.ToString() }
			);
			Width = 600;
			Height = 400;
		}
	}
}
