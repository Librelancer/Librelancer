// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
using ImGuiNET;
namespace LibreLancer.ImUI
{
    public unsafe class ListClipper : IDisposable
    {
        ImGuiListClipper *clipper;
        public ListClipper()
        {
            clipper = ImGuiNative.ImGuiListClipper_ImGuiListClipper();
        }

        public void Begin(int itemsCount, float itemsHeight = -1)
        {
            ImGuiNative.ImGuiListClipper_Begin(clipper, itemsCount, itemsHeight);
        }
        public bool Step()
        {
            return ImGuiNative.ImGuiListClipper_Step(clipper) != 0;
        }
        public void End()
        {
            ImGuiNative.ImGuiListClipper_End(clipper);
        }

        public int DisplayStart => clipper->DisplayStart;
        public int DisplayEnd => clipper->DisplayEnd;

        public void Dispose()
        {
            ImGuiNative.ImGuiListClipper_destroy(clipper);
        }
    }
}
