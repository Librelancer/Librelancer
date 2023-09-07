// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using LibreLancer.GameData;
using LibreLancer.GameData.World;
using LibreLancer.Physics;
using LibreLancer.World;

namespace LibreLancer.Client.Components
{
	//Generates rigidbodies the player can hit for asteroid fields
	public class CAsteroidFieldComponent : GameComponent
	{
		public AsteroidField Field;
        ConvexMeshCollider shape;
        private ResourceManager res;
		public CAsteroidFieldComponent(AsteroidField field, ResourceManager res, GameObject parent) : base(parent)
		{
			Field = field;
			float rdist = 0f;
			if (field.Zone.Shape is ZoneSphere)
				rdist = ((ZoneSphere)field.Zone.Shape).Radius;
			else if (field.Zone.Shape is ZoneEllipsoid)
			{
				var s = ((ZoneEllipsoid)field.Zone.Shape).Size;
				rdist = Math.Max(Math.Max(s.X, s.Y), s.Z);
			}
			else if (field.Zone.Shape is ZoneBox)
			{
				var s = ((ZoneEllipsoid)field.Zone.Shape).Size;
				rdist = Math.Max(Math.Max(s.X, s.Y), s.Z);
			}
			rdist += COLLIDE_DISTANCE;
			activateDist = rdist * rdist;
            this.res = res;
        }

		float activateDist;

		ExclusionZone GetExclusionZone(Vector3 pt)
		{
			for (int i = 0; i < Field.ExclusionZones.Count; i++)
			{
				var f = Field.ExclusionZones[i];
				if (f.Zone.Shape.ContainsPoint(pt))
					return f;
			}
			return null;
		}

        PhysicsWorld phys;
        public override void Register(PhysicsWorld physics)
        {
            phys = physics;
            Dictionary<string, int> indexes = new Dictionary<string, int>();
            shape = new ConvexMeshCollider(phys);
            foreach (var asteroid in Field.Cube)
            {
                var path = Path.ChangeExtension(asteroid.Drawable.ModelFile, "sur");
                if (File.Exists(path))
                {
                    var id = physics.UseMeshFile(res.GetSur(path));
                    shape.AddPart(id, 0, asteroid.RotationMatrix * Matrix4x4.CreateTranslation(asteroid.Position * Field.CubeSize), null);
                }
                else
                {
                    FLLog.Error("Sur", "Hitbox not found " + path);
                }
            }
        }
        public override void Unregister(PhysicsWorld physics)
        {
            if(shape != null) shape.Dispose();
            phys = null;
        }
		const float COLLIDE_DISTANCE = 600;

        List<PhysicsObject> bodies = new List<PhysicsObject>();
		public override void Update(double time)
		{
			var world = Parent.GetWorld();
			var player = world.GetObject("player");
			if (player == null) return;
			if (Vector3.DistanceSquared(player.PhysicsComponent.Body.Position, Field.Zone.Position) > activateDist) return;
			var cds = (Field.CubeSize + COLLIDE_DISTANCE);
			cds *= cds;
			for (int i = bodies.Count - 1; i >= 0; i--)
			{
				var distance = Vector3.DistanceSquared(player.PhysicsComponent.Body.Position, bodies[i].Position);
				if (distance > cds)
				{
                    world.Physics.RemoveObject(bodies[i]);
					bodies.RemoveAt(i);
				}
			}

			var close = AsteroidFieldShared.GetCloseCube(player.PhysicsComponent.Body.Position, Field.CubeSize);
			var cubeRad = new Vector3(Field.CubeSize) * 0.5f;
			int amountCubes = (int)Math.Floor((COLLIDE_DISTANCE / Field.CubeSize)) + 1;
			for (int x = -amountCubes; x <= amountCubes; x++)
			{
				for (int y = -amountCubes; y <= amountCubes; y++)
				{
					for (int z = -amountCubes; z <= amountCubes; z++)
					{
						var center = close + new Vector3(x * Field.CubeSize, y * Field.CubeSize, z * Field.CubeSize);
						if (!Field.Zone.Shape.ContainsPoint(center))
							continue;
						if (Vector3.DistanceSquared(player.PhysicsComponent.Body.Position, center) > cds)
							continue;
						if (!AsteroidFieldShared.CubeExists(center, Field.EmptyCubeFrequency, out int tval))
							continue;
						if (GetExclusionZone(center) != null)
							continue;
						bool create = true;
						for (int i = 0; i < bodies.Count; i++)
						{
							if ((bodies[i].Position - center).LengthSquared() < 4)
							{
								create = false;
								break;
							}
						}
						if (create)
						{
                            var transform = Field.CubeRotation.GetRotation(tval) * Matrix4x4.CreateTranslation(center);
                            var body = phys.AddStaticObject(transform, shape);
                            bodies.Add(body);
						}
					}
				}
			}
		}
	}
}
