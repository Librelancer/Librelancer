using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;

namespace ImGuiNET;

public unsafe partial class ImDrawListPtr
{
    private static int _id;
    private static readonly ConcurrentDictionary<int, CallBackInfo> _callbacks = new();

    class CallBackInfo
    {
        public ImDrawCallback Callback;
        public IntPtr UserData;
        public int UserDataSize;
    }

    [UnmanagedCallersOnly]
    static void CbCall(ImDrawList* list, ImDrawCmd* cmd)
    {
        if (_callbacks.TryRemove((int)cmd->UserCallbackData, out var cb))
        {
            cmd->UserCallbackData = cb.UserData;
            cmd->UserCallbackDataSize = cb.UserDataSize;
            cb.Callback(list, cmd);
        }
    }

    public void AddCallback(ImDrawCallback callback, IntPtr userData, int userDataSize = 0)
    {
        var cb = new CallBackInfo() { Callback = callback, UserData = userData, UserDataSize = userDataSize };
        int newId = Interlocked.Increment(ref _id);
        if (!_callbacks.TryAdd(newId, cb))
            throw new InvalidOperationException("ConcurrentDictionary broke (should never happen)");
        ImGuiNative.ImDrawList_AddCallback(Handle, &CbCall, (IntPtr)newId, 0);
    }
}
