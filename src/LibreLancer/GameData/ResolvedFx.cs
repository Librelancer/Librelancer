// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.Audio;
using LibreLancer.Data.Effects;
using LibreLancer.Fx;
using LibreLancer.Resources;

namespace LibreLancer.GameData
{
    public class ResolvedFx : IdentifiableItem
    {
        public string[] LibraryFiles;
        public uint VisFxCrc;
        public string AlePath;
        public BeamSpear Spear;
        public BeamBolt Bolt;
        public AudioEntry Sound;

        public ParticleEffect GetEffect(ResourceManager resman)
        {
            if (string.IsNullOrWhiteSpace(AlePath))
            {
                return null;
            }

            foreach (var f in LibraryFiles)
            {
                resman.LoadResourceFile(f);
            }

            var lib = resman.GetParticleLibrary(AlePath);
            return lib.FindEffect(VisFxCrc);
        }
    }
}
