using System.IO;

namespace LibreLancer.Tests;

public static class TestAsset
{
    public static Stream Open<T>(string name)
    {
        var assetPath = typeof(T).Namespace + ".TestAssets." + name;
        return typeof(TestAsset).Assembly.GetManifestResourceStream(assetPath)
            ?? throw new FileNotFoundException("Unable to find embedded resource: " + assetPath, assetPath);
    }
}
