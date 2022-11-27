// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.World.Components
{
    //HACK for Thn Scenes needing a main object
    public class DirtyTransformComponent : GameComponent
    {
        public DirtyTransformComponent(GameObject parent) : base(parent)
        {
        }

        public override void Update(double time)
        {
            Parent.ForceTransformDirty();
        }
    }
}