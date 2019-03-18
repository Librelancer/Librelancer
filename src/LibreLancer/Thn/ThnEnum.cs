// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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
                if (Enum.GetUnderlyingType(typeof(T)) == typeof(byte)) {
                    var i = (byte)(int)(float)o;
                    if (!Enum.IsDefined(typeof(T), i))
                        throw new NotImplementedException(typeof(T).ToString() + ": " + i);
                    return (T)(object)i;
                }
                else {
                    var i = (int)(float)o;
                    if (!Enum.IsDefined(typeof(T), i))
                        throw new NotImplementedException(typeof(T).ToString() + ": " + i);
                    return (T)(object)i;
                }
			}
			if (typeof(T) == typeof(bool)) return (T)(object)((float)o != 0);
            //TODO: Move non-flags enums to workingTypes when complete
            if (typeof(T) == typeof(SoundFlags)) return (T)DoSoundFlags(o);
			if (typeof(T) == typeof(TargetTypes)) return (T)DoTargetTypes(o);
			if (typeof(T) == typeof(EntityTypes)) return (T)DoEntityTypes(o);
			if (typeof(T) == typeof(AttachFlags)) return FlagsReflected<T>((int)(float)o);
			if (typeof(T) == typeof(EventTypes)) return (T)DoEventTypes(o);
			throw new InvalidCastException();
		}

		//Types where internal representations match
		static Type[] workingTypes = new Type[] {
			typeof(LightTypes), typeof(FogModes)
		};

		//WIP enums
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
				FLLog.Error("Thn","Flags for " + typeof(T).Name + ": " + v);
			}
			return (T)(dynamic)objFlags;
		}


        static object DoSoundFlags(object o)
        {
            if (o is float && (((int)(float)o) == 8)) return SoundFlags.Loop;
            throw new NotImplementedException(o.ToString()); 
        }

        //TODO: Migrate to workingTypes
        static object DoEventTypes(object o)
		{
			switch ((int)(float)o)
			{
                case 2:
                    return EventTypes.SetCamera;
				case 3:
					return EventTypes.StartSound;
				case 4:
					return EventTypes.StartLightPropAnim;
                case 5:
                    return EventTypes.StartCameraPropAnim;
				case 6:
					return EventTypes.StartPathAnimation;
				case 7:
					return EventTypes.StartSpatialPropAnim;
				case 8:
					return EventTypes.AttachEntity;
				case 10:
					return EventTypes.StartMotion;
				case 13:
					return EventTypes.StartPSys;
                case 14:
                    return EventTypes.StartPSysPropAnim;
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
                case 2:
                    return EntityTypes.Deformable;
				case 3:
					return EntityTypes.Camera;
                case 4:
                    return EntityTypes.Monitor;
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
                    case 2:
                        return TargetTypes.Part;
					default:
						throw new NotImplementedException(o.ToString());
				}
			}
		}

	}
}
