using LibreLancer;
using LibreLancer.Graphics;

namespace LancerEdit.Shaders;

static class EditorShaders
{
    public static ShaderBundle EnvMapTest;
    public static ShaderBundle Grid;
    public static ShaderBundle Normals;

    private static bool iscompiled;

    public static void Compile(RenderContext context)
    {
        if (iscompiled)
        {
            return;
        }

        iscompiled = true;
        FLLog.Debug("Shaders", "Compiling LancerEdit shaders");
        EnvMapTest = Compile(context, "EnvMapTest");
        Grid = Compile(context, "Grid");
        Normals = Compile(context, "Normals");
        FLLog.Debug("Shaders", "Compile complete");
    }

    static ShaderBundle Compile(RenderContext context, string name)
    {
        FLLog.Debug("Shaders", $"Compiling {name}");
        return ShaderBundle.FromResource<MainWindow>(context, $"{name}.bin");
    }
}
