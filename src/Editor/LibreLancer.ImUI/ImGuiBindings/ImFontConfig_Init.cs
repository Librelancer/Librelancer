using System.Runtime.InteropServices;

namespace ImGuiNET;

public unsafe partial struct ImFontConfig
{
    [DllImport("cimgui")]
    static extern void ImFontConfig_Construct (ImFontConfig* self);
    public ImFontConfig()
    {
        fixed (ImFontConfig* self = &this)
        {
            ImFontConfig_Construct(self);
        }
    }
}
