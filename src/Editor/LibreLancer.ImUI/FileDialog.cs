// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
namespace LibreLancer.ImUI
{
    public class FileDialogFilters
    {
        public FileFilter[] Filters;
        public FileDialogFilters(params FileFilter[] filters)
        {
            Filters = filters;
        }
    }
    public class FileFilter
    {
        public string Name;
        public string[] Extensions;
        public FileFilter(string name, params string[] exts)
        {
            Name = name;
            Extensions = exts;
        }
    }
	public  class FileDialog
	{
        static dynamic parentForm;
        static bool kdialog;
        static IntPtr parentWindow;
        public static void RegisterParent(Game game)
        {
			if(Platform.RunningOS != OS.Windows)
            {
                kdialog = HasKDialog();
                game.GetX11Info(out IntPtr _, out parentWindow);
            }
        }

        public static string Open(FileDialogFilters filters = null)
		{
			if (Platform.RunningOS == OS.Windows)
			{
                if (Win32.Win32OpenDialog(Win32.ConvertFilters(filters), null, out string result))
                    return result;
                else
                    return null;
			}
			else if (Platform.RunningOS == OS.Linux)
			{
                if (kdialog)
                    return KDialogOpen(filters);
                else if (parentWindow != IntPtr.Zero)
                    return Gtk2.GtkOpen(parentWindow, filters);
                else
                    return Gtk3.GtkOpen(filters);
			}
			else
			{
				//Mac
				throw new NotImplementedException();
			}
		}

        public static string ChooseFolder()
        {
            if(Platform.RunningOS == OS.Windows) {
                if (Win32.Win32PickFolder(null, out string result))
                    return result;
                else
                    return null;
            } else if (Platform.RunningOS == OS.Linux) {
                if (kdialog)
                    return KDialogChooseFolder();
                else if (parentWindow != IntPtr.Zero)
                    return Gtk2.GtkFolder(parentWindow);
                else
                    return Gtk3.GtkFolder();
            } else {
                //Mac
                throw new NotImplementedException();
            }
        }
        public static string Save(FileDialogFilters filters = null)
		{
			if (Platform.RunningOS == OS.Windows)
			{
                if (Win32.Win32SaveDialog(Win32.ConvertFilters(filters), null, out string result))
                    return result;
                else
                    return null;
			}
			else if (Platform.RunningOS == OS.Linux)
			{
                if (kdialog)
                    return KDialogSave(filters);
                else if (parentWindow != IntPtr.Zero)
                    return Gtk2.GtkSave(parentWindow, filters);
                else
                    return Gtk3.GtkSave(filters);
			}
			else
			{
				//Mac
				throw new NotImplementedException();
			}
		}

        static bool HasKDialog()
        {
            var startInfo = new ProcessStartInfo("/usr/bin/bash")
            {
                UseShellExecute = false,
                Arguments = " -c \"command -v kdialog >/dev/null 2>&1\""
            };
            var p = Process.Start(startInfo);
            p.WaitForExit();
            return p.ExitCode == 0;
        }

        static string KDialogProcess(string s)
        {
            if (parentWindow != IntPtr.Zero) 
                s = string.Format(" --attach {0} {1}", parentWindow, s);
            var pinf = new ProcessStartInfo("kdialog", s);
            pinf.RedirectStandardOutput = true;
            pinf.UseShellExecute = false;
            var p = Process.Start(pinf);
            string output = "";
            p.OutputDataReceived += (sender, e) => {
                output += e.Data + "\n";
            };
            p.BeginOutputReadLine();
            p.WaitForExit();
            if (p.ExitCode == 0)
                return output.Trim();
            else
                return null;
        }

        static string KDialogFilter(FileDialogFilters filters)
        {
            if (filters == null) return "";
            var builder = new StringBuilder();
            bool first = true;
            foreach (var f in filters.Filters)
            {
                if (!first)
                    builder.Append("|");
                else
                    first = false;
                builder.Append(f.Name);
                builder.Append(" (");
                var exts = string.Join(" ", f.Extensions.Select((x) =>
                {
                    if (x.Contains(".")) return x;
                    else return "*." + x;
                }));
                builder.Append(exts).Append(")");
            }
            builder.Append("|All Files (*.*)");
            return builder.ToString();
        }

        static string lastSave = "";
        static string KDialogSave(FileDialogFilters filters)
        {
            if (string.IsNullOrEmpty(lastSave))
                lastSave = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var ret = KDialogProcess(string.Format("--getsavefilename \"{0}\" \"{1}\"", lastSave, KDialogFilter(filters)));
            lastSave = ret ?? lastSave;
            return ret;
        }

        static string KDialogChooseFolder()
        {
            return KDialogProcess("--getexistingdirectory");
        }

        static string lastOpen = "";
        static string KDialogOpen(FileDialogFilters filters)
        {
            if (String.IsNullOrEmpty(lastOpen))
                lastOpen = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var ret = KDialogProcess(string.Format("--getopenfilename \"{0}\" \"{1}\"", lastOpen, KDialogFilter(filters)));
            lastOpen = ret ?? lastOpen;
            return ret;
        }

	}
}
