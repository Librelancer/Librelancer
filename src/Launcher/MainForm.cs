using System;
using System.IO;
using System.Windows.Forms;

namespace Launcher
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void btnLaunch_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(txtPath.Text))
            {
                if(!Directory.Exists(Path.Combine(txtPath.Text, "DATA")))
                {
                    MessageBox.Show("Not a valid freelancer directory");
                    return;
                }
                Program.LaunchPath = txtPath.Text;
                Properties.Settings.Default.FLPath = txtPath.Text;
                Properties.Settings.Default.Save();
                Application.Exit();
            }
            else
            {
                MessageBox.Show("Path does not exist");
            }
        }

        private void btnChoosePath_Click(object sender, EventArgs e)
        {
            using (var vbrowse = new FolderBrowserDialog())
            {
                if(vbrowse.ShowDialog() == DialogResult.OK)
                {
                    txtPath.Text = vbrowse.SelectedPath;
                }
            }
        }
    }
}
