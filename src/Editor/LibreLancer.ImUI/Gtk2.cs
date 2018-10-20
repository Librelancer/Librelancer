// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
namespace LibreLancer.ImUI
{
    static class Gtk2
    {
        public const string LIB = "libgtk-x11-2.0.so.0";
        public const string LIB_GDK = "libgdk-x11-2.0.so.0";
        public const string LIB_XLIB = "libX11.so.6";
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
        static extern void gtk_widget_realize(IntPtr widget);

        [DllImport(LIB)]
        static extern IntPtr gtk_widget_get_window(IntPtr widget);

        [DllImport(LIB_GDK)]
        static extern IntPtr gdk_x11_get_default_xdisplay();

        [DllImport(LIB_GDK)]
        static extern IntPtr gdk_x11_drawable_get_xid(IntPtr drawable);

        [DllImport(LIB_XLIB)]
        static extern int XSetTransientForHint(IntPtr display, IntPtr w, IntPtr prop_window);

        [DllImport(LIB)]
        static extern IntPtr gtk_file_filter_new();

        [DllImport(LIB)]
        static extern void gtk_file_filter_set_name(IntPtr filter, string str);

        [DllImport(LIB)]
        static extern void gtk_file_filter_add_pattern(IntPtr filter, string pattern);

        [DllImport(LIB)]
        static extern void gtk_file_chooser_add_filter(IntPtr chooser, IntPtr filter);

        static void SetFilters(IntPtr dlg, FileDialogFilters filters)
        {
            if (filters == null) return;
            foreach (var managed in filters.Filters) {
                var f = gtk_file_filter_new();
                gtk_file_filter_set_name(f, managed.Name);
                foreach (var ext in managed.Extensions)
                    gtk_file_filter_add_pattern(f, "*." + ext);
                gtk_file_chooser_add_filter(dlg, f);
            }
            //Add wildcards
            var wc = gtk_file_filter_new();
            gtk_file_filter_set_name(wc,"*.*");
            gtk_file_filter_add_pattern(wc,"*");
            gtk_file_chooser_add_filter(dlg,wc);
        }
        //Logic
        public static string GtkOpen(IntPtr parent, FileDialogFilters filters)
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
            gtk_widget_realize(dlg);
            XSetTransientForHint(gdk_x11_get_default_xdisplay(), 
                                 gdk_x11_drawable_get_xid(gtk_widget_get_window(dlg)), parent);
            string result = null;
            if (gtk_dialog_run(dlg) == GTK_RESPONSE_ACCEPT)
                result = UnsafeHelpers.PtrToStringUTF8(gtk_file_chooser_get_filename(dlg));
            WaitCleanup();
            gtk_widget_destroy(dlg);
            WaitCleanup();
            return result;
        }

        public static string GtkFolder(IntPtr parent)
        {
            if(!gtk_init_check(IntPtr.Zero, IntPtr.Zero))
            {
                throw new Exception();
            }
            var dlg = gtk_file_chooser_dialog_new("Open Directory", IntPtr.Zero,
                                                 GTK_FILE_CHOOSER_ACTION_SELECT_FOLDER,
                                                  IntPtr.Zero);
            gtk_dialog_add_button(dlg, "_Cancel", GTK_RESPONSE_CANCEL);
            gtk_dialog_add_button(dlg, "_Accept", GTK_RESPONSE_ACCEPT);
            gtk_widget_realize(dlg);
            XSetTransientForHint(gdk_x11_get_default_xdisplay(),
                                 gdk_x11_drawable_get_xid(gtk_widget_get_window(dlg)), parent);
            string result = null;
            if (gtk_dialog_run(dlg) == GTK_RESPONSE_ACCEPT)
                result = UnsafeHelpers.PtrToStringUTF8(gtk_file_chooser_get_filename(dlg));
            WaitCleanup();
            gtk_widget_destroy(dlg);
            WaitCleanup();
            return result;
        }
        public static string GtkSave(IntPtr parent, FileDialogFilters filters)
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
            gtk_widget_realize(dlg);
            XSetTransientForHint(gdk_x11_get_default_xdisplay(),
                                 gdk_x11_drawable_get_xid(gtk_widget_get_window(dlg)), parent);
            string result = null;
            if (gtk_dialog_run(dlg) == GTK_RESPONSE_ACCEPT)
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
