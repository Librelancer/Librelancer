// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Xml.Serialization;
using MoonSharp.Interpreter;

namespace LibreLancer
{
    [MoonSharpUserData]
    public class GameSettings
    {
        public float MasterVolume = 1.0f;
        public float SfxVolume = 1.0f;
        public float MusicVolume = 1.0f;
        public bool VSync = true;
        public int Anisotropy = 0;
        public int MSAA = 0;
        
        public int[] AnisotropyLevels() => RenderContext.GetAnisotropyLevels();
        public int MaxMSAA() => RenderContext.MaxSamples;
        
        [MoonSharpHidden] 
        [XmlIgnore]
        public RenderContext RenderContext;
        
        [MoonSharpHidden]
        public GameSettings MakeCopy()
        {
            var gs = new GameSettings();
            gs.MasterVolume = MasterVolume;
            gs.SfxVolume = SfxVolume;
            gs.MusicVolume = MusicVolume;
            gs.VSync = VSync;
            gs.Anisotropy = Anisotropy;
            gs.MSAA = MSAA;
            gs.RenderContext = RenderContext;
            return gs;
        }
    }
}