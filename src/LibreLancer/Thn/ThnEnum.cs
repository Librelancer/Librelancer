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
using System.Linq;
namespace LibreLancer
{
	//Conversions from FL's numbering to LibreLancer's
	public class ThnEnum
	{
		public static T Check<T>(object o)
		{
			if (o is T) return (T)o;
			if (o is string) return (T)ThnScript.ThnEnv[(string)o];

			if (workingTypes.Contains(typeof(T)))
			{
				var i = (int)(float)o;
				if (!Enum.IsDefined(typeof(T), i))
					throw new NotImplementedException(typeof(T).ToString() + ": " + i);
				return (T)(object)i;
			}
			if (typeof(T) == typeof(bool)) return (T)(object)((float)o != 0);
			//TODO: Move non-flags enums to workingTypes when complete
			if (typeof(T) == typeof(TargetTypes)) return (T)DoTargetTypes(o);
			if (typeof(T) == typeof(EntityTypes)) return (T)DoEntityTypes(o);
			if (typeof(T) == typeof(AttachFlags)) return (T)DoAttachFlags(o);
			if (typeof(T) == typeof(EventTypes)) return (T)DoEventTypes(o);
			if (typeof(T) == typeof(FogModes)) return (T)DoFogModes(o);
			throw new InvalidCastException();
		}

		//Types where internal representations match
		static Type[] workingTypes = new Type[] {
			typeof(LightTypes)
		};

		//WIP enums
		static object DoAttachFlags(object o)
		{
			switch ((int)(float)o)
			{
				case (2 | 4):
					return AttachFlags.Position | AttachFlags.Orientation;
				case (2 | 4 | 8):
					return AttachFlags.Position | AttachFlags.Orientation | AttachFlags.LookAt;
				case 2:
					return AttachFlags.Position;
				case 4:
					return AttachFlags.Orientation;
				case (2 | 4 | 32):
					return AttachFlags.Position | AttachFlags.Orientation | AttachFlags.EntityRelative;
				case (2 | 4 | 32 | 128):
					return AttachFlags.Position | AttachFlags.Orientation | AttachFlags.EntityRelative | AttachFlags.OrientationRelative;
				default:
					throw new NotImplementedException(o.ToString());
			}
		}

		public static T FlagsReflected<T>(int input)
		{
			int v = input;
			int objFlags = 0;
			foreach (var fl in Enum.GetValues(typeof(T)))
			{
				var integer = (int)(dynamic)fl;
				if ((v & integer) == integer)
				{
					v &= ~integer;
					objFlags |= integer;
				}
			}
			if (v != 0)
			{
				throw new NotImplementedException("Flags for " + typeof(T).Name + ": " + v);
			}
			return (T)(dynamic)objFlags;
		}

		static object DoFogModes(object o)
		{
			switch ((int)(float)o)
			{
				case 3:
					return FogModes.Linear;
				default:
					throw new NotImplementedException(o.ToString());
			}
		}

		//TODO: Migrate to workingTypes
		static object DoEventTypes(object o)
		{
			switch ((int)(float)o)
			{
				case 3:
					return EventTypes.StartSound;
				case 4:
					return EventTypes.StartLightPropAnim;
				case 6:
					return EventTypes.StartPathAnimation;
				case 7:
					return EventTypes.StartSpatialPropAnim;
				case 8:
					return EventTypes.AttachEntity;
				case 13:
					return EventTypes.StartPSys;
				case 15:
					return EventTypes.StartAudioPropAnim;
				case 16:
					return EventTypes.StartFogPropAnim;
				default:
					throw new NotImplementedException(o.ToString());
			}
		}

		//TODO: Migrate to workingTypes
		static object DoEntityTypes(object o)
		{
			switch ((int)(float)o)
			{
				case 1:
					return EntityTypes.Compound;
				case 3:
					return EntityTypes.Camera;
				case 5:
					return EntityTypes.Light;
				case 6:
					return EntityTypes.Sound;
				case 7:
					return EntityTypes.Marker;
				case 9:
					return EntityTypes.Scene;
				case 11:
					return EntityTypes.MotionPath;
				case 13:
					return EntityTypes.PSys;
				default:
					throw new NotImplementedException(o.ToString());
			}
		}

		//TODO: Migrate to workingTypes
		static object DoTargetTypes(object o)
		{
			if (o is TargetTypes)
				return (TargetTypes)o;
			else
			{
				switch ((int)(float)o)
				{
					case 0:
						return TargetTypes.Root;
					case 1:
						return TargetTypes.Hardpoint;
					default:
						throw new NotImplementedException(o.ToString());
				}
			}
		}

	}
}
