using System;
using System.Text;
using Eto.Forms;
namespace Launcher
{
	public class CrashWindow : Form
	{
		public CrashWindow(Exception ex) : this(
			"Uh oh!",
			"Librelancer has crashed. See the log for more information.",
			GetExceptionMessage(ex)
		)
		{
		}
		static string GetExceptionMessage(Exception ex)
		{
			var str = new StringBuilder();
			str.AppendLine(ex.GetType().Name);
			str.AppendLine(ex.Message);
			str.AppendLine(ex.StackTrace);
			return str.ToString();
		}
		public CrashWindow(string title, string label, string text)
		{
			Title = title;
			Content = new TableLayout(
				new Label() { Text = label },
				new TextArea() { Text = text, ReadOnly = true }
			);
			Width = 600;
			Height = 400;
		}
	}
}
