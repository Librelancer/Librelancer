// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Physics;
using LibreLancer.Resources;
using LibreLancer.Sounds;

namespace LibreLancer.World
{
	public class GameComponent(GameObject parent)
    {
		public GameObject Parent = parent;

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

        protected SoundManager? GetSoundManager()
        {
            return Parent.GetWorld()?.Renderer != null ? Parent.GetWorld()!.Renderer!.Game.GetService<SoundManager>() : null;
        }

        protected GameDataManager? GetGameData()
        {
            var w = Parent.GetWorld();
            return w?.Server != null ? w.Server.Server.GameData : w?.Renderer?.Game.GetService<GameDataManager>();

        }

        protected ResourceManager? GetResourceManager()
        {
            var w = Parent.GetWorld();
            return w.Renderer != null ? w.Renderer.ResourceManager : w.Server?.Server.Resources;
        }
	}
}
