using LibreLancer;
using LibreLancer.Graphics;

namespace LibreLancer.ImUI.Shaders;

static class ImGuiShader
{
    public static ShaderBundle Shader;
    public static ShaderBundle Gradient;

    private static bool iscompiled;

    public static void Compile(RenderContext context)
    {
        if (iscompiled)
        {
            return;
        }

        iscompiled = true;
        FLLog.Debug("Shaders", "Compiling ImGui shaders");
        Shader = Compile(context, "ImGuiShader");
        Gradient = Compile(context, "AdvGradient");
        FLLog.Debug("Shaders", "Compile complete");
    }

    static ShaderBundle Compile(RenderContext context, string name)
    {
        FLLog.Debug("Shaders", $"Compiling {name}");
        return ShaderBundle.FromResource<ImGuiHelper>(context, $"{name}.bin");
    }
}
