// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
namespace LibreLancer.ImUI
{
	static class Gtk3
	{
		public const string LIB="libgtk-3.so.0";
        public const string LIB_GLIB = "libgobject-2.0.so.0";
		[DllImport(LIB)]
		public static extern bool gtk_init_check(IntPtr argc, IntPtr argv);

		public const int GTK_FILE_CHOOSER_ACTION_OPEN = 0;
		public const int GTK_FILE_CHOOSER_ACTION_SAVE = 1;
		public const int GTK_FILE_CHOOSER_ACTION_SELECT_FOLDER = 2;
		public const int GTK_FILE_CHOOSER_ACTION_CREATE_FOLDER = 3;
		public const int GTK_RESPONSE_CANCEL = -6;
		public const int GTK_RESPONSE_ACCEPT = -3;

		[DllImport(LIB)]
		public static extern IntPtr gtk_file_chooser_dialog_new(
			[MarshalAs(UnmanagedType.LPStr)]string title,
			IntPtr parent,
			int action,
			IntPtr ignore);

		[DllImport(LIB)]
		public static extern int gtk_dialog_run(IntPtr dialog);

		[DllImport(LIB)]
		public static extern bool gtk_events_pending();

		[DllImport(LIB)]
		public static extern void gtk_main_iteration();

		[DllImport(LIB)]
		public static extern void gtk_widget_destroy(IntPtr widget);

        [DllImport(LIB)]
        public static extern void gtk_widget_show(IntPtr widget);


		[DllImport(LIB)]
		public static extern void gtk_dialog_add_button(IntPtr dialog, [MarshalAs(UnmanagedType.LPStr)]string button_text, int response_id);

		[DllImport(LIB)]
		public static extern IntPtr gtk_file_chooser_get_filename(IntPtr chooser);

		[DllImport(LIB)]
		public static extern void gtk_window_set_keep_above(IntPtr window, bool value);

        [DllImport(LIB)]
        public static extern void gtk_window_present(IntPtr window);

        [DllImport(LIB_GLIB)]
        public static extern void g_signal_connect_data(IntPtr instance, string detailed_signal,
                                                        IntPtr c_handler, IntPtr data, IntPtr destroy_data,
                                                        int connect_flags);

        [DllImport(LIB)]
        static extern IntPtr gtk_file_filter_new();

        [DllImport(LIB)]
        static extern void gtk_file_filter_set_name(IntPtr filter, string str);

        [DllImport(LIB)]
        static extern void gtk_file_filter_add_pattern(IntPtr filter, string pattern);

        [DllImport(LIB)]
        static extern void gtk_file_chooser_add_filter(IntPtr chooser, IntPtr filter);

        static IntPtr ptr;
        static Delegate del;
        static int response;
        static bool running;
        public static int gtk_dialog_run_HACK(IntPtr dlg)
        {
            if(del == null) {
                del = (Action<IntPtr,int,IntPtr>)run_response_handler;
                ptr = Marshal.GetFunctionPointerForDelegate(del);
            }
            gtk_widget_show(dlg);
            g_signal_connect_data(dlg, "response", ptr, IntPtr.Zero, IntPtr.Zero, 0);
            const int DO_ITERATIONS = 4;
            int iterations = 0;
            running = true;
            response = 0;
            while (running)
            {
                //BIG HACK to stop Gtk filechooser from appearing below
                if (iterations == (DO_ITERATIONS -1)) {
                    gtk_window_set_keep_above(dlg, true);
                    iterations++;
                } else if (iterations == DO_ITERATIONS) {
                    gtk_window_set_keep_above(dlg, false);
                    iterations++;
                }
                else if (iterations < DO_ITERATIONS)
                    iterations++;
                while (gtk_events_pending())
                    gtk_main_iteration();
                System.Threading.Thread.Sleep(0);
            }
            return response;
        }

        static void run_response_handler(IntPtr dialog, int response_id, IntPtr data)
        {
            response = response_id;
            running = false;
        }

        static void SetFilters(IntPtr dlg, FileDialogFilters filters)
        {
            if (filters == null) return;
            foreach (var managed in filters.Filters)
            {
                var f = gtk_file_filter_new();
                gtk_file_filter_set_name(f, managed.Name);
                foreach (var ext in managed.Extensions)
                    gtk_file_filter_add_pattern(f, "*." + ext);
                gtk_file_chooser_add_filter(dlg, f);
            }
            //Add wildcards
            var wc = gtk_file_filter_new();
            gtk_file_filter_set_name(wc, "*.*");
            gtk_file_filter_add_pattern(wc, "*");
            gtk_file_chooser_add_filter(dlg, wc);
        }
        //Logic
        public static string GtkOpen(FileDialogFilters filters)
        {
            if (!gtk_init_check(IntPtr.Zero, IntPtr.Zero))
            {
                throw new Exception();
            }
            var dlg = gtk_file_chooser_dialog_new("Open File", IntPtr.Zero,
                                                      GTK_FILE_CHOOSER_ACTION_OPEN,
                                                      IntPtr.Zero);
            gtk_dialog_add_button(dlg, "_Cancel", GTK_RESPONSE_CANCEL);
            gtk_dialog_add_button(dlg, "_Accept", GTK_RESPONSE_ACCEPT);
            SetFilters(dlg, filters);
            string result = null;
            if (gtk_dialog_run_HACK(dlg) == GTK_RESPONSE_ACCEPT)
                result = UnsafeHelpers.PtrToStringUTF8(gtk_file_chooser_get_filename(dlg));
            WaitCleanup();
            gtk_widget_destroy(dlg);
            WaitCleanup();
            return result;
        }
        public static string GtkFolder()
        {
            if (!gtk_init_check(IntPtr.Zero, IntPtr.Zero))
                throw new Exception();
            var dlg = gtk_file_chooser_dialog_new("Open Directory", IntPtr.Zero,
                                                      GTK_FILE_CHOOSER_ACTION_SELECT_FOLDER,
                                                      IntPtr.Zero);
            gtk_dialog_add_button(dlg, "_Cancel", GTK_RESPONSE_CANCEL);
            gtk_dialog_add_button(dlg, "_Accept", GTK_RESPONSE_ACCEPT);
            string result = null;
            if (gtk_dialog_run_HACK(dlg) == GTK_RESPONSE_ACCEPT)
                result = UnsafeHelpers.PtrToStringUTF8(gtk_file_chooser_get_filename(dlg));
            WaitCleanup();
            gtk_widget_destroy(dlg);
            WaitCleanup();
            return result;
        }

        public static string GtkSave(FileDialogFilters filters)
        {
            if (!gtk_init_check(IntPtr.Zero, IntPtr.Zero))
            {
                throw new Exception();
            }
            var dlg = gtk_file_chooser_dialog_new("Save File", IntPtr.Zero,
                                                      GTK_FILE_CHOOSER_ACTION_SAVE,
                                                      IntPtr.Zero);
            gtk_dialog_add_button(dlg, "_Cancel", GTK_RESPONSE_CANCEL);
            gtk_dialog_add_button(dlg, "_Accept", GTK_RESPONSE_ACCEPT);
            SetFilters(dlg, filters);
            string result = null;
            if (gtk_dialog_run_HACK(dlg) == GTK_RESPONSE_ACCEPT)
                result = UnsafeHelpers.PtrToStringUTF8(gtk_file_chooser_get_filename(dlg));
            WaitCleanup();
            gtk_widget_destroy(dlg);
            WaitCleanup();
            return result;
        }


        static void WaitCleanup()
        {
            while (gtk_events_pending())
                gtk_main_iteration();
        }
	}
}
