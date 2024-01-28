using System;
using System.IO;

namespace LibreLancer.Tests;

public static class TestAsset
{
    public static Stream Open(string name)
    {
        return typeof(TestAsset).Assembly.GetManifestResourceStream("LibreLancer.Tests.TestAssets." + name) ?? throw new FileNotFoundException();
    }
}
