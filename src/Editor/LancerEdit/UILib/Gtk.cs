using System;
using System.Runtime.InteropServices;
namespace LancerEdit
{
	static class Gtk
	{
		[DllImport("libgtk-3.so")]
		public static extern bool gtk_init_check(IntPtr argc, IntPtr argv);

		public const int GTK_FILE_CHOOSER_ACTION_OPEN = 0;
		public const int GTK_FILE_CHOOSER_ACTION_SAVE = 1;
		public const int GTK_FILE_CHOOSER_ACTION_SELECT_FOLDER = 2;
		public const int GTK_FILE_CHOOSER_ACTION_CREATE_FOLDER = 3;
		public const int GTK_RESPONSE_CANCEL = -6;
		public const int GTK_RESPONSE_ACCEPT = -3;

		[DllImport("libgtk-3.so")]
		public static extern IntPtr gtk_file_chooser_dialog_new(
			[MarshalAs(UnmanagedType.LPStr)]string title,
			IntPtr parent,
			int action,
			IntPtr ignore);

		[DllImport("libgtk-3.so")]
		public static extern int gtk_dialog_run(IntPtr dialog);

		[DllImport("libgtk-3.so")]
		public static extern bool gtk_events_pending();

		[DllImport("libgtk-3.so")]
		public static extern void gtk_main_iteration();

		[DllImport("libgtk-3.so")]
		public static extern void gtk_widget_destroy(IntPtr widget);

		[DllImport("libgtk-3.so")]
		public static extern void gtk_dialog_add_button(IntPtr dialog, [MarshalAs(UnmanagedType.LPStr)]string button_text, int response_id);

		[DllImport("libgtk-3.so")]
		public static extern IntPtr gtk_file_chooser_get_filename(IntPtr chooser);
	}
}
