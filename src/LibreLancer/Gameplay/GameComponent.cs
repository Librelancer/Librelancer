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
		public virtual void Update(TimeSpan time)
		{
		}
		public virtual void FixedUpdate(TimeSpan time)
		{
		}
		public virtual void Register(PhysicsWorld physics)
		{
		}
		public virtual void Unregister(PhysicsWorld physics)
		{
		}
	}
}
