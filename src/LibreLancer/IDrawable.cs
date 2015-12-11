using System.Collections.Generic;

using OpenTK;
//using FLCommon;

//using FLApi.Universe;

namespace LibreLancer
{
    public interface IDrawable
    {
		void Initialize(ResourceCache cache);
        void Resized();
        void Update(Camera camera);
		void Draw(Matrix4 world, Lighting light);
    }
}