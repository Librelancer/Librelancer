// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Fx;

namespace LibreLancer.GameData
{
    public class ResolvedFx
    {
        public string[] LibraryFiles;
        public uint VisFxCrc;
        public string AlePath;

        public ParticleEffect GetEffect(ResourceManager resman)
        {
            foreach(var f in LibraryFiles)
                resman.LoadResourceFile(f);
            var lib = resman.GetParticleLibrary(AlePath);
            return lib.FindEffect(VisFxCrc);
        }
    }
}