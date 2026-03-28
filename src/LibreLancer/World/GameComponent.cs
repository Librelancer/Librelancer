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

        public virtual void Update(double time, GameWorld world)
		{
		}

		public virtual void Register(GameWorld world)
		{
		}

		public virtual void Unregister(GameWorld world)
		{
		}

        public virtual void HardpointDestroyed(Hardpoint hardpoint)
        {
        }

        protected SoundManager? GetSoundManager(GameWorld world)
        {
            return world.Renderer != null ? world.Renderer!.Game.GetService<SoundManager>() : null;
        }

        protected GameDataManager? GetGameData(GameWorld world)
        {
            return world.Server != null ? world.Server.Server.GameData : world?.Renderer?.Game.GetService<GameDataManager>();
        }

        protected ResourceManager? GetResourceManager(GameWorld world)
        {
            return world.Renderer != null ? world.Renderer!.ResourceManager : world?.Server?.Server.Resources;
        }
	}
}
