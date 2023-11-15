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

    //GMainLoop
    [DllImport("libglib-2.0.so.0")]
    static extern IntPtr g_main_loop_new(void* context, int is_running);

    [DllImport("libglib-2.0.so.0")]
    static extern IntPtr g_main_loop_quit(IntPtr main_loop);

    [DllImport("libglib-2.0.so.0")]
    static extern IntPtr g_main_loop_run(IntPtr main_loop);

    [DllImport("libglib-2.0.so.0")]
    static extern IntPtr g_main_loop_unref(IntPtr main_loop);

    [DllImport("libglib-2.0.so.0")]
    static extern IntPtr g_timeout_add(uint interval, delegate* unmanaged<void*, int> function, void* data);

    [UnmanagedCallersOnly]
    static int iterate_gmain_timeout_function(void* data)
    {
        g_main_loop_quit((IntPtr) data);
        return 0;
    }

    public static MountInfo[] GetMounts()
    {
        var paths = new List<MountInfo>();
        paths.Add(new MountInfo("/", "/"));
        //Fill mount information (need to run main loop)
        var mainloop = g_main_loop_new(null, 0);
        g_timeout_add(500, &iterate_gmain_timeout_function, (void*) mainloop);
        g_main_loop_run(mainloop);
        //Get mount information
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
        g_main_loop_unref(mainloop);
        return paths.ToArray();
    }

    // Monitor mount events and call GetMounts()
    public class GMountEvents : PlatformEvents
    {
        public IUIThread Dispatch;
        private Thread eventLoop;
        private MountContext* context;

        public GMountEvents(IUIThread mainThread) => Dispatch = mainThread;

        struct MountContext
        {
            public bool running;
            public IntPtr mainloop;
        }

        [UnmanagedCallersOnly]
        static int iterate_gmain_timeout_function(void* data)
        {
            var ctx = (MountContext*) data;
            if (!ctx->running)
            {
                g_main_loop_quit(ctx->mainloop);
                return 0;
            }
            return 1;
        }

        delegate void MonitorCallback(IntPtr a, IntPtr b);
        public void Start()
        {
            if (eventLoop != null)
                throw new InvalidOperationException();
            context = (MountContext*) Marshal.AllocHGlobal(sizeof(MountContext));
            context->running = true;
            var mainloop = g_main_loop_new(null, 0);
            context->mainloop = mainloop;
            g_timeout_add(250, &iterate_gmain_timeout_function, (void*) context);
            eventLoop = new Thread(() =>
            {
                //Asynchronously update mounts whenever they are changed
                var del = (MonitorCallback) ((_, _) =>
                {
                    Task.Run(() => Dispatch.QueueUIThread(() => Platform.OnMountsChanged(GetMounts())));
                });
                var cb = Marshal.GetFunctionPointerForDelegate(del);
                var monitor = g_volume_monitor_get();
                SignalConnect(monitor, "mount-added", cb, null);
                SignalConnect(monitor, "mount-removed", cb, null);
                SignalConnect(monitor, "mount-changed", cb, null);
                g_main_loop_run(mainloop);
                g_object_unref(monitor);
                g_main_loop_unref(mainloop);
            }) {IsBackground = true, Name = "GLib monitoring loop"};
            eventLoop.Start();
        }

        public override void Dispose()
        {
            if (context != null)
                context->running = false;
            eventLoop?.Join();
            Marshal.FreeHGlobal((IntPtr)context);
            context = null;
            eventLoop = null;
        }
    }
}
