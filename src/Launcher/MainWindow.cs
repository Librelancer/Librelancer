using System;
using System.IO;
using Xwt;
namespace Launcher
{
	public class MainWindow : Window
	{
		public bool Run = false;
		TextEntry textInput;
        //NumericMaskedTextBox<int> resWidthBox;
        //NumericMaskedTextBox<int> resHeightBox;
        TextEntry resWidthBox;
        TextEntry resHeightBox;
		CheckBox skipMovies;
        CheckBox muteMusic;
        CheckBox vsync;
        public MainWindow(bool forceNoMovies)
        {
            Title = "Librelancer Launcher";
            Resizable = false;
            var mainBox = new VBox() { Spacing = 6 };
            //Directory
            var dirbox = new HBox() { Spacing = 2 };
            mainBox.PackStart(new Label() { Text = "Freelancer Directory: " });
            dirbox.PackStart((textInput = new TextEntry()), true, true);
            textInput.Text = Program.Config.FreelancerPath;
            var btnChooseFolder = new Button() { Label = " ... " };
            btnChooseFolder.Clicked += BtnChooseFolder_Clicked;
            dirbox.PackStart(btnChooseFolder);
            mainBox.PackStart(dirbox);
            //Options
            skipMovies = new CheckBox() { Label = "Skip Intro Movies" };
            if (forceNoMovies)
            {
                skipMovies.Active = true;
                skipMovies.Sensitive = false;
            }
            else
                skipMovies.Active = !Program.Config.IntroMovies;
            var smbox = new HBox();
            smbox.PackStart(skipMovies);
            mainBox.PackStart(smbox);
            muteMusic = new CheckBox() { Label = "Mute Music" };
            muteMusic.Active = Program.Config.MuteMusic;
            mainBox.PackStart(muteMusic);
            vsync = new CheckBox() { Label = "VSync" };
            vsync.Active = Program.Config.VSync;
            mainBox.PackStart(vsync);
            //Resolution
            resWidthBox = new TextEntry();
            resWidthBox.Text = Program.Config.BufferWidth.ToString();
            resWidthBox.TextInput += Masking;
            resHeightBox = new TextEntry();
            resHeightBox.Text = Program.Config.BufferHeight.ToString();
            resHeightBox.TextInput += Masking;
            var hboxResolution = new HBox();
            hboxResolution.PackEnd(resHeightBox);
            hboxResolution.PackEnd(new Label() { Text = "x" });
            hboxResolution.PackEnd(resWidthBox);
            hboxResolution.PackStart(new Label() { Text = "Resolution:" });
            mainBox.PackStart(hboxResolution);
            //Launch
            var launchbox = new HBox() { Spacing = 2 };
			var btnLaunch = new Button("Launch");
            btnLaunch.Clicked += BtnLaunch_Clicked;
            launchbox.PackEnd(btnLaunch);
            mainBox.PackEnd(launchbox);
            //Finish
            CloseRequested += MainWindow_CloseRequested;
            Content = mainBox;
		}

        private void Masking(object sender, TextInputEventArgs e)
        {
            foreach (var ch in e.Text)
            {
                if(!char.IsDigit(ch))
                    e.Handled = true;
            }
        }

        private void MainWindow_CloseRequested(object sender, CloseRequestedEventArgs args)
        {
            Application.Exit();
        }

        private void BtnChooseFolder_Clicked(object sender, EventArgs e)
        {
            var dlg = new SelectFolderDialog();
            if(dlg.Run() == true)
            {
                textInput.Text = dlg.Folder;
            }
        }

		private void BtnLaunch_Clicked(object sender, EventArgs e)
        {
            if (Directory.Exists(textInput.Text))
            {
                if (!LibreLancer.GameConfig.CheckFLDirectory(textInput.Text))
                {
                    MessageDialog.ShowError(this, "Not a valid freelancer directory");
                    return;
                }
                Program.Config.FreelancerPath = textInput.Text;
                Program.Config.IntroMovies = !skipMovies.Active;
                Program.Config.MuteMusic = muteMusic.Active;
                Program.Config.VSync = vsync.Active;
                Program.Config.BufferWidth = int.Parse(resWidthBox.Text);
                Program.Config.BufferHeight = int.Parse(resHeightBox.Text);
                Run = true;
                Visible = false;
                ShowInTaskbar = false;
                Program.Run();
            }
            else
            {
                MessageDialog.ShowError(this, "Path does not exist");
            }
        }
    }
}

