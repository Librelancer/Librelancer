// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Cameras;

[ParsedIni]
public partial class CameraIni
{
    [Section("WinCamera")] public CameraProps WinCamera = new() {FovX = 54.432f};
    [Section("CockpitCamera")] public CameraProps CockpitCamera = new();
    [Section("ThirdPersonCamera")] public CameraProps ThirdPersonCamera = new();
    [Section("DeathCamera")] public CameraProps DeathCamera = new();
    [Section("TurretCamera")] public CameraProps TurretCamera = new();
    [Section("RearViewCamera")] public CameraProps RearViewCamera = new();

    public CameraIni(string camerasPath, FileSystem vfs, IniStringPool? stringPool = null)
    {
        ParseIni(camerasPath, vfs, stringPool);
    }
}

[ParsedSection]
public partial class CameraProps
{
    [Entry("fovx")] public float FovX = 70;
    [Entry("znear")] public float ZNear;
}
