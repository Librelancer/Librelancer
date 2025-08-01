// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Resources;

namespace LibreLancer
{
    public interface IRigidModelFile : IDrawable
    {
        RigidModel CreateRigidModel(bool drawable, ResourceManager resources);
    }
}
