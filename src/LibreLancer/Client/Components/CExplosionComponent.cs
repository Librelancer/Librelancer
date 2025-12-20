// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.GameData;
using LibreLancer.World;

namespace LibreLancer.Client.Components
{
    public class CExplosionComponent : GameComponent
    {
        public Explosion Explosion;
        public CExplosionComponent(GameObject parent, Explosion explosion) : base(parent)
        {
            Explosion = explosion;
        }
    }
}
