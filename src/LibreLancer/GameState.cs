// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
	public abstract class GameState
	{
		protected FreelancerGame Game;
		public GameState (FreelancerGame game)
		{
			Game = game;
		}
		public abstract void Update(TimeSpan delta);
		public abstract void Draw(TimeSpan delta);
        public virtual void OnResize()
        {
        }
		public virtual void Unregister()
		{
		}
	}
}

