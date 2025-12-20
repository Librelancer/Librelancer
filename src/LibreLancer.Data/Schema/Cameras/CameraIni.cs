// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Cameras
{
    [ParsedIni]
    public partial class CameraIni
    {
        [Section("WinCamera")] public CameraProps WinCamera = new CameraProps() {FovX = 54.432f};
        [Section("CockpitCamera")] public CameraProps CockpitCamera = new CameraProps();
        [Section("ThirdPersonCamera")] public CameraProps ThirdPersonCamera = new CameraProps();
        [Section("DeathCamera")] public CameraProps DeathCamera = new CameraProps();
        [Section("TurretCamera")] public CameraProps TurretCamera = new CameraProps();
        [Section("RearViewCamera")] public CameraProps RearViewCamera = new CameraProps();

        public CameraIni(string camerasPath, FileSystem vfs, IniStringPool stringPool = null)
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
}
