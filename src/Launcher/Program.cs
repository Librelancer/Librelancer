using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
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
			
			#if OSX
			//I'll get to making this work on OSX later. For now a nice hardcoded path - Callum
			LaunchPath = "/Volumes/Untitled/Freelancer";
			#else
				Application.EnableVisualStyles ();
				Application.SetCompatibleTextRenderingDefault (true);
				Application.Run (new MainForm ());
			#endif
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
