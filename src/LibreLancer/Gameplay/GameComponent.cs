// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Physics;
namespace LibreLancer
{
	public class GameComponent
	{
		public GameObject Parent;
		public GameComponent(GameObject parent)
		{
			Parent = parent;
		}
		public virtual void Update(double time)
		{
		}
		public virtual void FixedUpdate(double time)
		{
		}
		public virtual void Register(PhysicsWorld physics)
		{
		}
		public virtual void Unregister(PhysicsWorld physics)
		{
		}

        protected SoundManager GetSoundManager()
        {
            if(Parent.GetWorld().Renderer != null)
                return Parent.GetWorld().Renderer.Game.GetService<SoundManager>();
            return null;
        }

        protected GameDataManager GetGameData()
        {
            if(Parent.GetWorld().Renderer != null)
                return Parent.GetWorld().Renderer.Game.GetService<GameDataManager>();
            return null;
        }

        protected ResourceManager GetResourceManager()
        {
            if(Parent.GetWorld().Renderer != null)
                return Parent.GetWorld().Renderer.Game.GetService<ResourceManager>();
            return null;
        }
	}
}
