using System;
using System.Reflection;
using LibreLancer;
using System.Runtime.InteropServices;
namespace LancerEdit
{
	static class FileDialog
	{
        static dynamic parentForm;
        public static void RegisterParent(Game game)
        {
            if (Platform.RunningOS != OS.Windows) return;
            IntPtr ptr;
            if ((ptr = game.GetHwnd()) == IntPtr.Zero) return;
            LoadSwf();
            var t = winforms.GetType("System.Windows.Forms.Control");
            var method = t.GetMethod("FromHandle", BindingFlags.Public | BindingFlags.Static);
            parentForm = method.Invoke(null, new object[] { ptr });
        }

        public static string Open()
		{
			if (Platform.RunningOS == OS.Windows)
			{
				string result = null;
				using (var ofd = NewObj("System.Windows.Forms.OpenFileDialog"))
				{
                    if (parentForm != null) ofd.Parent = parentForm;
					if (ofd.ShowDialog() == SwfOk())
					{
						result = ofd.FileName;
					}
				}
				WinformsDoEvents();
				return result;
			}
			else if (true || Platform.RunningOS == OS.Linux)
			{
				return GtkOpen();
			}
			else
			{
				//Mac
				throw new NotImplementedException();
			}
		}

		public static string Save()
		{
			if (Platform.RunningOS == OS.Windows)
			{
				string result = null;
				using (var sfd = NewObj("System.Windows.Forms.SaveFileDialog"))
				{
                    if (parentForm != null) sfd.Parent = parentForm;
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
				return GtkSave();
			}
			else
			{
				//Mac
				throw new NotImplementedException();
			}
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

		static string GtkOpen()
		{
			if (!Gtk.gtk_init_check(IntPtr.Zero, IntPtr.Zero))
			{
				throw new Exception();
			}
			var dlg = Gtk.gtk_file_chooser_dialog_new("Open File", IntPtr.Zero,
													  Gtk.GTK_FILE_CHOOSER_ACTION_OPEN,
													  IntPtr.Zero);
			Gtk.gtk_dialog_add_button(dlg, "_Cancel", Gtk.GTK_RESPONSE_CANCEL);
			Gtk.gtk_dialog_add_button(dlg, "_Accept", Gtk.GTK_RESPONSE_ACCEPT);
			string result = null;
			if (Gtk.gtk_dialog_run(dlg) == Gtk.GTK_RESPONSE_ACCEPT)
			{
				var file = Gtk.gtk_file_chooser_get_filename(dlg);
				result = Marshal.PtrToStringAnsi(file);
			}
			WaitCleanup();
			Gtk.gtk_widget_destroy(dlg);
			WaitCleanup();
			return result;
		}

		static string GtkSave()
		{
			if (!Gtk.gtk_init_check(IntPtr.Zero, IntPtr.Zero))
			{
				throw new Exception();
			}
			var dlg = Gtk.gtk_file_chooser_dialog_new("Save File", IntPtr.Zero,
													  Gtk.GTK_FILE_CHOOSER_ACTION_SAVE,
													  IntPtr.Zero);
			Gtk.gtk_dialog_add_button(dlg, "_Cancel", Gtk.GTK_RESPONSE_CANCEL);
			Gtk.gtk_dialog_add_button(dlg, "_Accept", Gtk.GTK_RESPONSE_ACCEPT);
			string result = null;
			if (Gtk.gtk_dialog_run(dlg) == Gtk.GTK_RESPONSE_ACCEPT)
			{
				var file = Gtk.gtk_file_chooser_get_filename(dlg);
				result = Marshal.PtrToStringAnsi(file);
			}
			WaitCleanup();
			Gtk.gtk_widget_destroy(dlg);
			WaitCleanup();
			return result;
		}

		static void WaitCleanup()
		{
			while (Gtk.gtk_events_pending())
				Gtk.gtk_main_iteration();
		}
	}
}
