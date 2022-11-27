// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Server.Components;
using LibreLancer.World;

namespace LibreLancer.Server.Ai
{
    public abstract class AiState
    {
        public abstract void OnStart(GameObject obj, SNPCComponent ai);
        public abstract void Update(GameObject obj, SNPCComponent ai, double dt);
    }
}