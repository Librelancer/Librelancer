using System;
using System.IO;
using Eto.Forms;
using Eto.Drawing;
namespace Launcher
{
	public class MainWindow : Form
	{
		TextBox textInput;
		NumericMaskedTextBox<int> resWidthBox;
		NumericMaskedTextBox<int> resHeightBox;
        bool forceANGLE = false;
		public MainWindow ()
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
			var findButton = new Button () { Text = "..." };
			findButton.Click += FindButton_Click;

			layout.Rows.Add (new TableLayout(new TableRow (
				new TableCell(textInput, true),
				findButton
			)));
			var skipMovies = new CheckBox () { Text = "Skip Intro Movies", Checked = true };
			skipMovies.CheckedChanged += SkipMovies_CheckedChanged;
			layout.Rows.Add (new TableRow (skipMovies));
            if(Environment.OSVersion.Platform != PlatformID.Unix)
            {
                var angleCheck = new CheckBox() { Text = "Force DX9 (Not Recommended)" };
                angleCheck.CheckedChanged += AngleCheck_CheckedChanged;
                layout.Rows.Add(new TableRow(angleCheck));
            }
			resWidthBox = new NumericMaskedTextBox<int>();
			resWidthBox.Value = 1024;
			resHeightBox = new NumericMaskedTextBox<int>();
			resHeightBox.Value = 768;
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

		void SkipMovies_CheckedChanged (object sender, EventArgs e)
		{
			Program.SkipIntroMovies = ((CheckBox)sender).Checked ?? false;
		}

        private void AngleCheck_CheckedChanged(object sender, EventArgs e)
        {
            forceANGLE = ((CheckBox)sender).Checked ?? false;
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
				Program.LaunchPath = textInput.Text;
                Program.ForceAngle = forceANGLE;
				Program.ResWidth = resWidthBox.Value;
				Program.ResHeight = resHeightBox.Value;
				Close ();			
			}
			else
			{
				MessageBox.Show(this, "Path does not exist", "Librelancer", MessageBoxType.Error);
			}
		}
	}
}

