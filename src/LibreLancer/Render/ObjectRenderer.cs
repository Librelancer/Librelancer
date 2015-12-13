using System;
using OpenTK;
using LibreLancer.GameData;
namespace LibreLancer
{
	public abstract class ObjectRenderer : IDisposable
	{
		protected Camera camera;
		public Matrix4 World { get; private set; }
		public SystemObject SpaceObject { get; private set; }

		public ObjectRenderer (Camera camera, Matrix4 world, bool useObjectPosAndRotate, SystemObject spaceObject)
		{
			if (useObjectPosAndRotate)
			{
				World = world * Matrix4.CreateTranslation(spaceObject.Position);
				if(spaceObject.Rotation != null)
					World = spaceObject.Rotation.Value * World;
			}
			else World = Matrix4.Identity;
			SpaceObject = spaceObject;
			this.camera = camera;
		}

		public virtual void Update(TimeSpan elapsed) {}
		public abstract void Draw(Lighting lights);
		public abstract void Dispose();

	}
}

