using System;
using System.IO;
using Eto.Forms;
using Eto.Drawing;
namespace Launcher
{
	public class MainWindow : Form
	{
		public bool Run = false;
		TextBox textInput;
		NumericMaskedTextBox<int> resWidthBox;
		NumericMaskedTextBox<int> resHeightBox;
		CheckBox skipMovies;
		public MainWindow (bool forceNoMovies)
		{
			Title = "Librelancer Launcher";
			ClientSize = new Size (400, 250);
			Resizable = false;
			var layout = new TableLayout ();
			layout.Spacing = new Size (2, 2);
			layout.Padding = new Padding (5, 5, 5, 5);

			layout.Rows.Add (new TableRow (
				new Label { Text = "Freelancer Directory:" }
			));

			textInput = new TextBox ();
			textInput.Text = Program.Config.FreelancerPath ?? "";
			var findButton = new Button () { Text = "..." };
			findButton.Click += FindButton_Click;

			layout.Rows.Add (new TableLayout(new TableRow (
				new TableCell(textInput, true),
				findButton
			)));
			skipMovies = new CheckBox () { Text = "Skip Intro Movies", Checked = forceNoMovies || !Program.Config.IntroMovies, Enabled = !forceNoMovies };
			layout.Rows.Add (new TableRow (skipMovies));
            if(Environment.OSVersion.Platform != PlatformID.Unix)
            {
                var angleCheck = new CheckBox() { Text = "Force DX9 (Not Recommended)", Checked = Program.Config.ForceAngle };
                angleCheck.CheckedChanged += AngleCheck_CheckedChanged;
                layout.Rows.Add(new TableRow(angleCheck));
            }
			resWidthBox = new NumericMaskedTextBox<int>();
			resWidthBox.Value = Program.Config.BufferWidth;
			resHeightBox = new NumericMaskedTextBox<int>();
			resHeightBox.Value = Program.Config.BufferHeight;
			layout.Rows.Add(new TableLayout(new TableRow(
				new TableCell(new Label() { Text = "Resolution: ", VerticalAlignment = VerticalAlignment.Center }, true),
				resWidthBox,
				resHeightBox
			)));
			var launchButton = new Button () { Text = "Launch Librelancer" };
			launchButton.Click += LaunchButton_Click;
			layout.Rows.Add(new TableRow { ScaleHeight = true });

			layout.Rows.Add (
				new TableRow (
				TableLayout.AutoSized (launchButton, null, true)
				)
			);
			Content = layout;
		}

        private void AngleCheck_CheckedChanged(object sender, EventArgs e)
        {
            Program.Config.ForceAngle = ((CheckBox)sender).Checked ?? false;
        }

        void FindButton_Click (object sender, EventArgs e)
		{
			var dlg = new SelectFolderDialog ();
			if (dlg.ShowDialog (this) == DialogResult.Ok) {
				textInput.Text = dlg.Directory;
			}
		}

		void LaunchButton_Click (object sender, EventArgs e)
		{
			if (Directory.Exists(textInput.Text))
			{
				if(!Directory.Exists(Path.Combine(textInput.Text, "DATA"))
					|| !Directory.Exists(Path.Combine(textInput.Text, "EXE")))
				{
					MessageBox.Show (this, "Not a valid freelancer directory", "Librelancer", MessageBoxType.Error);
					return;
				}
				Program.Config.FreelancerPath = textInput.Text;
				Program.Config.IntroMovies = !(skipMovies.Checked ?? true);
				Program.Config.BufferWidth = resWidthBox.Value;
				Program.Config.BufferHeight = resHeightBox.Value;
				Run = true;
				Close ();			
			}
			else
			{
				MessageBox.Show(this, "Path does not exist", "Librelancer", MessageBoxType.Error);
			}
		}
	}
}

