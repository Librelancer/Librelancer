// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using LibreLancer.Ini;
namespace LibreLancer.Data.Universe
{
    public class NebulaFog
    {
        [Entry("fog_enabled")] 
        public int Enabled;
        [Entry("near")] 
        public int Near;
        [Entry("distance")] 
        public int Distance;
        [Entry("color")] 
        public Color4 Color;
    }
}