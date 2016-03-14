using System;
using System.Collections.Generic;

using OpenTK;
//using FLCommon;

//using FLApi.Universe;

namespace LibreLancer
{
    public interface IDrawable
    {
		void Initialize(ResourceManager cache);
        void Resized();
		void Update(ICamera camera, TimeSpan delta);
		void Draw(RenderState rstate, Matrix4 world, Lighting light);
    }
}