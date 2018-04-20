using LibreLancer;
using LibreLancer.Exceptions;
using System;
using System.IO;
using Xwt;
namespace Launcher
{
	public class MainWindow : Window
	{
		TextEntry textInput;
        //NumericMaskedTextBox<int> resWidthBox;
        //NumericMaskedTextBox<int> resHeightBox;
        TextEntry resWidthBox;
        TextEntry resHeightBox;
		CheckBox skipMovies;
        CheckBox muteMusic;
        CheckBox vsync;
        private GameConfig config;
        public MainWindow(GameConfig config, bool forceNoMovies)
        {
            this.config = config;

            Title = "Librelancer Launcher";
            Resizable = false;
            var mainBox = new VBox() { Spacing = 6 };
            //Directory
            var dirbox = new HBox() { Spacing = 2 };
            mainBox.PackStart(new Label() { Text = "Freelancer Directory: " });
            dirbox.PackStart((textInput = new TextEntry()), true, true);
            textInput.Text = config.FreelancerPath;
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
                skipMovies.Active = !config.IntroMovies;
            var smbox = new HBox();
            smbox.PackStart(skipMovies);
            mainBox.PackStart(smbox);
            muteMusic = new CheckBox() { Label = "Mute Music" };
            muteMusic.Active = config.MuteMusic;
            mainBox.PackStart(muteMusic);
            vsync = new CheckBox() { Label = "VSync" };
            vsync.Active = config.VSync;
            mainBox.PackStart(vsync);
            //Resolution
            resWidthBox = new TextEntry();
            resWidthBox.Text = config.BufferWidth.ToString();
            resWidthBox.TextInput += Masking;
            resHeightBox = new TextEntry();
            resHeightBox.Text = config.BufferHeight.ToString();
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
            try
            {
                config.FreelancerPath = textInput.Text;
                config.IntroMovies = !skipMovies.Active;
                config.MuteMusic = muteMusic.Active;
                config.VSync = vsync.Active;
                config.BufferWidth = int.Parse(resWidthBox.Text);
                config.BufferHeight = int.Parse(resHeightBox.Text);
                config.Validate();
                Program.Launch = true;
                Visible = false;
                ShowInTaskbar = false;
            }
            catch (InvalidFreelancerDirectory)
            {
                MessageDialog.ShowError(this, "Not a valid freelancer directory");
                return;
            }
            catch (Exception ex)
            {
                MessageDialog.ShowError(this, "Invalid configuration");
                return;
            }
        }
    }
}

