// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Diagnostics;
using System.IO;
using Eto.Forms;
using Eto.Drawing;
using LibreLancer.Exceptions;

namespace Launcher
{
    public class MainWindow : Form
    {
        LibreLancer.GameConfig config;
        TextBox freelancerPath;
        NumericMaskedTextBox<int> bufferWidth;
        NumericMaskedTextBox<int> bufferHeight;
        CheckBox skipIntroMovies;
        Slider masterVolume;
        Slider musicVolume;
        Slider sfxVolume;
        CheckBox vsync;
        public MainWindow()
        {
            config = LibreLancer.GameConfig.Create();
            Title = "Librelancer";
            var wrap = new TableLayout();
            wrap.Rows.Add(new TableRow(new HeaderBar()));
            var layout = new TableLayout();
            //Freelancer Path
            freelancerPath = new TextBox() { Text = config.FreelancerPath };
            var flpath = new TableLayout() { Padding = 2, Spacing = new Size(2, 0) };
            flpath.Rows.Add(new TableRow(
                new Label() { Text = "Freelancer Directory:", VerticalAlignment = VerticalAlignment.Center },
                new TableCell(freelancerPath,true), 
                new Button(FolderBrowse) {  Text = ".." }
                ));
            layout.Rows.Add(flpath);
            //Width & Height
            bufferWidth = new NumericMaskedTextBox<int>() { Value = config.BufferWidth };
            bufferHeight = new NumericMaskedTextBox<int>() { Value = config.BufferHeight };
            var res = new TableLayout() { Padding = 2, Spacing = new Size(2, 0) };
            res.Rows.Add(new TableRow(
            new Label() { Text = "Resolution:", VerticalAlignment = VerticalAlignment.Center },
                new TableCell(bufferWidth) { ScaleWidth = true },
            new Label() { Text = "x", VerticalAlignment = VerticalAlignment.Center },
                new TableCell(bufferHeight) { ScaleWidth = true }));
            layout.Rows.Add(res);
            //Options
            skipIntroMovies = new CheckBox() { Text = "Skip Intro Movies" };
            if (Program.introForceDisable)
            {
                skipIntroMovies.Enabled = false;
                skipIntroMovies.Checked = true;
            }
            else
                skipIntroMovies.Checked = !config.IntroMovies;
            layout.Rows.Add(skipIntroMovies);
            masterVolume = new Slider()
            {
                MinValue =  0, MaxValue = 1000, Value = (int)(config.MasterVolume * 1000),
                Orientation = Orientation.Horizontal, TickFrequency = 0, SnapToTick = false, 
                Style = "volslider"
            };
            var layoutMaster = new TableLayout(
                new TableRow(new Label() {Text = "Master Volume: ", VerticalAlignment = VerticalAlignment.Center}, masterVolume)
            );
            layout.Rows.Add(layoutMaster);
            sfxVolume = new Slider()
            {
                MinValue =  0, MaxValue = 1000, Value = (int)(config.SfxVolume * 1000),
                Orientation = Orientation.Horizontal, TickFrequency = 0, SnapToTick = false, 
                Style = "volslider"
            };
            var layoutSfx = new TableLayout(
                new TableRow(new Label() {Text = "Sfx Volume: ", VerticalAlignment = VerticalAlignment.Center}, sfxVolume)
            );
            layout.Rows.Add(layoutSfx);
            musicVolume = new Slider()
            {
                MinValue =  0, MaxValue = 1000, Value = (int)(config.MusicVolume * 1000),
                Orientation = Orientation.Horizontal, TickFrequency = 0,  SnapToTick = false,
                Style = "volslider"
            };
            var layoutMusic = new TableLayout(
                new TableRow(new Label() {Text = "Music Volume: ",  VerticalAlignment = VerticalAlignment.Center}, musicVolume)
            );
            layout.Rows.Add(layoutMusic);
            vsync = new CheckBox() { Text = "VSync", Checked = config.VSync };
            layout.Rows.Add(vsync);
            //Spacer
            layout.Rows.Add(new TableRow() { ScaleHeight = true });
            //Launch
            var end = new TableLayout() { Padding = 2, Spacing = new Size(2, 0) };
            end.Rows.Add(new TableRow(new TableCell() { ScaleWidth = true }, new Button(LaunchClicked) { Text = "Launch" }));
            layout.Rows.Add(end);
            wrap.Rows.Add(new TableRow(layout) { ScaleHeight = true });
            Content = wrap;
        }
        void FolderBrowse(object sender, EventArgs e)
        {
            using(var dlg = new SelectFolderDialog())
            {
                if (Directory.Exists(freelancerPath.Text))
                    dlg.Directory = freelancerPath.Text;
                if(dlg.ShowDialog(this) == DialogResult.Ok)
                {
                    freelancerPath.Text = dlg.Directory;
                }
            }
        }
        void LaunchClicked(object sender, EventArgs e)
        {
            try
            {
                config.FreelancerPath = freelancerPath.Text;
                config.IntroMovies = !skipIntroMovies.Checked.Value;
                config.MasterVolume = masterVolume.Value / 1000f;
                config.MusicVolume = musicVolume.Value / 1000f;
                config.SfxVolume = sfxVolume.Value / 1000f;
                config.VSync = vsync.Checked.Value;
                config.BufferWidth = bufferWidth.Value;
                config.BufferHeight = bufferHeight.Value;
                config.Validate();
            }
            catch (InvalidFreelancerDirectory)
            {
                MessageBox.Show(this, "Not a valid Freelancer directory", MessageBoxType.Error);
                return;
            }
            catch (Exception)
            {
                MessageBox.Show(this, "Invalid configuration", MessageBoxType.Error);
                return;
            }
            config.Save();
            Environment.CurrentDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            if (LibreLancer.Platform.RunningOS == LibreLancer.OS.Windows)
                Process.Start("lancer.exe");
            else
                Process.Start("mono", "lancer.exe");
            Close();
        }
    }

    class HeaderBar : Drawable
    {
        public HeaderBar()
        {
            var font = new Font(SystemFont.Bold, 14);
            var h = (int)font.MeasureString("Librelancer").Height;
            Height = h + 10;
            MinimumSize = new Size(0, h + 10);
            
            font.Dispose();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var brsh = new LinearGradientBrush(Color.Parse("#0072ff"), Color.Parse("#00c6ff"), new PointF(0, 0), new PointF(ClientSize.Width, 0));
            e.Graphics.FillRectangle(brsh, new RectangleF(0, 0, this.ClientSize.Width, this.ClientSize.Height));
            var fnt = new Font(SystemFont.Bold, 14);
            e.Graphics.DrawText(fnt, Colors.Black, new PointF(7, 7), "Librelancer");
            e.Graphics.DrawText(fnt, Colors.White, new PointF(5, 5), "Librelancer");
            fnt.Dispose();
            brsh.Dispose();
        }
    }
}
