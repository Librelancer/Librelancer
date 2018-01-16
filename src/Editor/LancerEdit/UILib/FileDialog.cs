using System;
using System.Reflection;
using LibreLancer;
using System.Runtime.InteropServices;
namespace LancerEdit
{
	static class FileDialog
	{
		public static string Open()
		{
			if (Platform.RunningOS == OS.Windows)
			{
				string result = null;
				using (var ofd = NewObj("System.Windows.Forms.OpenFileDialog"))
				{
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

		static Assembly winforms;
		static dynamic NewObj(string type)
		{
			if (winforms == null)
				winforms = Assembly.LoadFrom("System.Windows.Forms");
			return Activator.CreateInstance(winforms.GetType(type));
		}

		static dynamic SwfOk()
		{
			if (winforms == null)
				winforms = Assembly.Load("System.Windows.Forms");
			var type = winforms.GetType("System.Windows.Forms.DialogResult");
			return Enum.Parse(type, "OK");
		}
		static void WinformsDoEvents()
		{
			if (winforms == null)
				winforms = Assembly.Load("System.Windows.Forms");
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
