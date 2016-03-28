using System;
using Eto.Forms;
namespace Launcher
{
    static class Program
    {
        public static string LaunchPath = null;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
			new Application ().Run (new MainWindow ());
			//Actually run the game
            if(LaunchPath != null)
            {
				var conf = new LibreLancer.GameConfig ();
				conf.FreelancerPath = LaunchPath;
				conf.Launch ();
            }
        }

    }
}
