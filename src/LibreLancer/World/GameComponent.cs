// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Physics;
using LibreLancer.Resources;
using LibreLancer.Sounds;

namespace LibreLancer.World
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

		public virtual void Register(PhysicsWorld physics)
		{
		}
		public virtual void Unregister(PhysicsWorld physics)
		{
		}

        public virtual void HardpointDestroyed(Hardpoint hardpoint)
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
            var w = Parent.GetWorld();
            if (w?.Server != null)
                return w.Server.Server.GameData;
            if(w?.Renderer != null)
                return w.Renderer.Game.GetService<GameDataManager>();
            return null;
        }

        protected ResourceManager GetResourceManager()
        {
            var w = Parent.GetWorld();
            if (w.Renderer != null)
                return w.Renderer.ResourceManager;
            else if (w.Server != null)
                return w.Server.Server.Resources;
            return null;
        }
	}
}
