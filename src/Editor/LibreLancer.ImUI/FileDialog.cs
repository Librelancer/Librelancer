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
			if (Platform.RunningOS == OS.Windows)
			{
				IntPtr ptr;
				if ((ptr = game.GetHwnd()) == IntPtr.Zero) return;
				LoadSwf();
				var t = winforms.GetType("System.Windows.Forms.Control");
				var method = t.GetMethod("FromHandle", BindingFlags.Public | BindingFlags.Static);
				parentForm = method.Invoke(null, new object[] { ptr });
			}
            else
            {
                kdialog = HasKDialog();
                game.GetX11Info(out IntPtr _, out parentWindow);
            }
        }

        public static string Open(FileDialogFilters filters = null)
		{
			if (Platform.RunningOS == OS.Windows)
			{
				string result = null;
				using (var ofd = NewObj("System.Windows.Forms.OpenFileDialog"))
				{
                    if (parentForm != null) ofd.Parent = parentForm;
                    if (filters != null) ofd.Filter = SwfFilter(filters);
					if (ofd.ShowDialog() == SwfOk())
					{
						result = ofd.FileName;
					}
				}
				WinformsDoEvents();
				return result;
			}
			else if (Platform.RunningOS == OS.Linux)
			{
                if (kdialog)
                    return KDialogOpen();
                else if (parentWindow != IntPtr.Zero)
                    return Gtk2.GtkOpen(parentWindow,filters);
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
                string result = null;
                using (var fbd = NewObj("System.Windows.Forms.FolderBrowserDialog")) {
                    if (parentForm != null) fbd.Parent = parentForm;
                    if (fbd.ShowDialog() == SwfOk() && !string.IsNullOrEmpty(fbd.SelectedPath))
                        result = fbd.SelectedPath;
                }
                return result;
            } else if (Platform.RunningOS == OS.Linux) {
                if (kdialog)
                    throw new NotImplementedException();
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
				string result = null;
				using (var sfd = NewObj("System.Windows.Forms.SaveFileDialog"))
				{
                    if (parentForm != null) sfd.Parent = parentForm;
                    if (filters != null) sfd.Filter = SwfFilter(filters);
                    if (sfd.ShowDialog() == SwfOk())
					{
						result = sfd.FileName;
					}
				}
				WinformsDoEvents();
				return result;
			}
			else if (Platform.RunningOS == OS.Linux)
			{
                if (kdialog)
                    return KDialogSave();
                else if (parentWindow != IntPtr.Zero)
                    return Gtk2.GtkSave(parentWindow,filters);
                else
                    return Gtk3.GtkSave(filters);
			}
			else
			{
				//Mac
				throw new NotImplementedException();
			}
		}

        static string SwfFilter(FileDialogFilters filters)
        {
            var builder = new StringBuilder();
            bool first = true;
            foreach(var f in filters.Filters) {
                if (!first)
                    builder.Append("|");
                else
                    first = false;
                builder.Append(f.Name);
                builder.Append(" (");
                var exts = string.Join(";",f.Extensions.Select((x) => "*." + x));
                builder.Append(exts).Append(")|").Append(exts);
            }
            builder.Append("|All files (*.*)|*.*");
            return builder.ToString();
        }
        const string WINFORMS_NAME = "System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
        static Assembly winforms;
        static void LoadSwf()
        {
            if (winforms == null)
                winforms = Assembly.Load(WINFORMS_NAME);
        }
		static dynamic NewObj(string type)
		{
            LoadSwf();
			return Activator.CreateInstance(winforms.GetType(type));
		}

		static dynamic SwfOk()
		{
            LoadSwf();
			var type = winforms.GetType("System.Windows.Forms.DialogResult");
			return Enum.Parse(type, "OK");
		}
		static void WinformsDoEvents()
		{
            LoadSwf();
			var t = winforms.GetType("System.Windows.Forms.Application");
			var method = t.GetMethod("DoEvents", BindingFlags.Public | BindingFlags.Static);
			method.Invoke(null, null);
		}

        static bool HasKDialog()
        {
            var p = Process.Start("bash", "-c 'command -v kdialog'");
            p.WaitForExit();
            return p.ExitCode == 0;
        }

        static string KDialogProcess(string s)
        {
            if (parentWindow != IntPtr.Zero) 
                s = string.Format("--attach {0} {1}", parentWindow, s);
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

        static string lastSave = "";
        static string KDialogSave()
        {
            if (string.IsNullOrEmpty(lastSave))
                lastSave = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var ret = KDialogProcess(string.Format("--getsavefilename \"{0}\"", lastSave));
            lastSave = ret ?? lastSave;
            return ret;
        }
        static string lastOpen = "";
        static string KDialogOpen()
        {
            if (String.IsNullOrEmpty(lastOpen))
                lastOpen = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var ret = KDialogProcess(string.Format("--getopenfilename \"{0}\"", lastOpen));
            lastOpen = ret ?? lastOpen;
            return ret;
        }

	}
}
