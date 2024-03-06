// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using LibreLancer.Data.IO;
using LibreLancer.Ini;

namespace LibreLancer.Data.Cameras
{
    public class CameraIni : IniFile
    {
        [Section("WinCamera")] public CameraProps WinCamera = new CameraProps() {FovX = 54.432f};
        [Section("CockpitCamera")] public CameraProps CockpitCamera = new CameraProps();
        [Section("ThirdPersonCamera")] public CameraProps ThirdPersonCamera = new CameraProps();
        [Section("DeathCamera")] public CameraProps DeathCamera = new CameraProps();
        [Section("TurretCamera")] public CameraProps TurretCamera = new CameraProps();
        [Section("RearViewCamera")] public CameraProps RearViewCamera = new CameraProps();

        public CameraIni(string camerasPath, FileSystem vFS)
        {
            ParseAndFill(camerasPath, vFS);
        }
    }

    public class CameraProps
    {
        [Entry("fovx")] public float FovX = 70;
        [Entry("znear")] public float ZNear;
    }
}
