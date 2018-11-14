using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace ImGuiNET
{

    public class MemoryEditor : IDisposable
    {
        [DllImport("cimgui")]
        static extern IntPtr igExtMemoryEditInit();

        [DllImport("cimgui")]
        static extern void igExtMemoryEditFree(IntPtr memedit);

        [DllImport("cimgui")]
        static extern void igExtMemoryEditDrawContents(IntPtr memedit, IntPtr mem_data_void_ptr, IntPtr mem_size, IntPtr base_display_addr);
        IntPtr nativePtr;

        public MemoryEditor()
        {
            nativePtr = igExtMemoryEditInit();
        }

        ~MemoryEditor()
        {
            Dispose();
        }

        public unsafe void DrawContents(byte[] mem_data, int mem_size, int base_display_addr = 0) 
        {
            if (disposed) throw new ObjectDisposedException("MemoryEditor");
            fixed(byte *ptr = mem_data)
            {
                igExtMemoryEditDrawContents(nativePtr, (IntPtr)ptr, (IntPtr)mem_size, (IntPtr)base_display_addr);
            }
        }

        bool disposed = false;
        public void Dispose()
        {
            if (disposed) return;
            igExtMemoryEditFree(nativePtr);
            disposed = true;
        }
    }

}