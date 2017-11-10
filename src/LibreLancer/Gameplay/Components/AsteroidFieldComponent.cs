/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Collections.Generic;
using System.IO;
using LibreLancer.Utf.Cmp;
using LibreLancer.Sur;
using LibreLancer.Jitter.Dynamics;
using LibreLancer.GameData;
namespace LibreLancer
{
	//Generates rigidbodies the player can hit for asteroid fields
	public class AsteroidFieldComponent : GameComponent
	{
		public AsteroidField Field;
		CompoundSurShape shape;
		public AsteroidFieldComponent(AsteroidField field, GameObject parent) : base(parent)
		{
			Field = field;
			var shapes = new List<CompoundSurShape.TransformedShape>();
			foreach (var asteroid in Field.Cube)
			{
				var mdl = asteroid.Drawable as ModelFile;
				var path = Path.ChangeExtension(mdl.Path, "sur");
				if (File.Exists(path))
				{
					SurFile sur = parent.Resources.GetSur(path);
					foreach (var s in sur.GetShape(0))
					{
						shapes.Add(new CompoundSurShape.TransformedShape(s, new Matrix3(asteroid.RotationMatrix), asteroid.Position * Field.CubeSize));
					}
				}
				else
				{
					FLLog.Error("Sur", "Hitbox not found " + path);
				}
			}
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
			shape = new CompoundSurShape(shapes);
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

		const float COLLIDE_DISTANCE = 600;

		List<RigidBody> bodies = new List<RigidBody>();
		public override void FixedUpdate(TimeSpan time)
		{
			var world = Parent.GetWorld();
			var player = world.GetObject("player");
			if (player == null) return;
			if (VectorMath.DistanceSquared(player.PhysicsComponent.Position, Field.Zone.Position) > activateDist) return;
			var cds = (Field.CubeSize + COLLIDE_DISTANCE);
			cds *= cds;
			for (int i = bodies.Count - 1; i >= 0; i--)
			{
				var distance = VectorMath.DistanceSquared(player.PhysicsComponent.Position, bodies[i].Position);
				if (distance > cds)
				{
					world.Physics.RemoveBody(bodies[i]);
					bodies.RemoveAt(i);
				}
			}

			var close = AsteroidFieldShared.GetCloseCube(player.PhysicsComponent.Position, Field.CubeSize);
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
						if (VectorMath.DistanceSquared(player.PhysicsComponent.Position, center) > cds)
							continue;
						float tval;
						if (!AsteroidFieldShared.CubeExists(center, Field.EmptyCubeFrequency, out tval))
							continue;
						if (GetExclusionZone(center) != null)
							continue;
						bool create = true;
						for (int i = 0; i < bodies.Count; i++)
						{
							if ((bodies[i].Position - center).LengthFast < 3)
							{
								create = false;
								break;
							}
						}
						if (create)
						{
							var body = new RigidBody(shape);
							body.Orientation = new Matrix3(Field.CubeRotation.GetRotation(tval));
							body.Position = center;
							body.IsStatic = true;
							bodies.Add(body);
							world.Physics.AddBody(body);
						}
					}
				}
			}
		}


	}
}
