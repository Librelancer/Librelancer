// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using LibreLancer.Utf.Mat;

namespace LibreLancer
{
    public interface IDrawable
    {
		void Initialize(ResourceManager cache);
        void Resized();
		void Update(ICamera camera, TimeSpan delta, TimeSpan totalTime);
		void Draw(RenderState rstate, Matrix4 world, Lighting light);
		void DrawBuffer(CommandBuffer buffer, Matrix4 world, ref Lighting light, Material overrideMat = null);
		float GetRadius();
    }
}