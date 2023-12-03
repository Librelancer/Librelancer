using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace LibreLancer.Platforms.Linux;

static unsafe class GLib
{
    //glib

    [DllImport("libglib-2.0.so.0")]
    static extern void g_free(IntPtr obj);

    static string GLibString(IntPtr v)
    {
        var str = Marshal.PtrToStringUTF8(v);
        g_free(v);
        return str;
    }

    // GList struct
    [StructLayout(LayoutKind.Sequential)]
    struct GList
    {
        public IntPtr data;
        public GList* next;
        public GList* prev;
    }

    [DllImport("libglib-2.0.so.0")]
    static extern void g_list_free_full(GList* list, delegate* unmanaged<IntPtr, void> free_func);

    [DllImport("libgobject-2.0.so.0")]
    static extern IntPtr g_type_check_instance_cast(IntPtr obj, IntPtr type);

    [UnmanagedCallersOnly]
    static void unref_wrap(IntPtr obj) => g_object_unref(obj);

    //GObject
    [DllImport("libgobject-2.0.so.0")]
    static extern void g_object_unref(IntPtr obj);

    [DllImport("libgobject-2.0.so.0")]
    static extern void g_signal_connect_data(IntPtr instance, IntPtr detailed_signal, IntPtr c_handler, void* data, void* destroy_data, int connect_flags);

    static void SignalConnect(IntPtr obj, string name, IntPtr cb, void* data)
    {
        var namestr = Marshal.StringToCoTaskMemUTF8(name);
        g_signal_connect_data(obj, namestr, cb, data, null, 0);
        Marshal.FreeCoTaskMem(namestr);
    }


    // GVolumeMonitor class
    [DllImport("libgio-2.0.so.0")]
    static extern GList* g_volume_monitor_get_mounts(IntPtr volume_monitor);

    [DllImport("libgio-2.0.so.0")]
    static extern IntPtr g_volume_monitor_get();

    //GMount class

    [DllImport("libgio-2.0.so.0")]
    static extern IntPtr g_mount_get_type();

    static IntPtr ToGMount(IntPtr obj) =>
        g_type_check_instance_cast(obj, g_mount_get_type());

    [DllImport("libgio-2.0.so.0")]
    static extern IntPtr g_mount_get_name(IntPtr mount);

    [DllImport("libgio-2.0.so.0")]
    static extern IntPtr g_mount_get_root(IntPtr mount);

    //GFile class
    [DllImport("libgio-2.0.so.0")]
    static extern IntPtr g_file_get_uri(IntPtr file);

    [DllImport("libgtk-3.so.0")]
    public static extern bool gtk_events_pending();

    [DllImport("libgtk-3.so.0")]
    public static extern void gtk_main_iteration();

    public static MountInfo[] GetMounts()
    {
        var paths = new List<MountInfo>();
        paths.Add(new MountInfo("/", "/"));
        //Fill mount information (need to run main loop)
        //Get mount information
        while(gtk_events_pending())
            gtk_main_iteration();
        IntPtr monitor = g_volume_monitor_get();
        var mounts = g_volume_monitor_get_mounts(monitor);
        GList* l;
        for (l = mounts; l != null; l = l->next)
        {
            var mount = ToGMount(l->data);
            var name = GLibString(g_mount_get_name(mount));
            var root = g_mount_get_root(mount);
            var uri = GLibString(g_file_get_uri(root));
            g_object_unref(root);
            if (uri.StartsWith("file://")) {
                paths.Add(new MountInfo(name, uri.Substring(7)));
            }
        }
        g_list_free_full(null, &unref_wrap);
        g_object_unref(monitor);
        return paths.ToArray();
    }

    // Monitor mount events and call GetMounts()
    public class GMountEvents : PlatformEvents
    {
        private Thread eventLoop;
        private IUIThread dispatch;

        public GMountEvents(IUIThread mainThread)
        {
            dispatch = mainThread;
        }

        private IntPtr monitor;

        delegate void MonitorCallback(IntPtr a, IntPtr b);

        private MonitorCallback cb;
        public void Start()
        {
            //Asynchronously update mounts whenever they are changed
            cb = (MonitorCallback) ((_, _) => dispatch.QueueUIThread(() =>
            {
                Platform.OnMountsChanged(GetMounts());
            }));
            var ptr = Marshal.GetFunctionPointerForDelegate(cb);
            monitor = g_volume_monitor_get();
            SignalConnect(monitor, "mount-added", ptr, null);
            SignalConnect(monitor, "mount-removed", ptr, null);
            SignalConnect(monitor, "mount-changed", ptr, null);
        }

        public override void Poll()
        {
            if(gtk_events_pending())
                gtk_main_iteration();
        }

        public override void Dispose()
        {
            g_object_unref(monitor);
        }
    }
}
