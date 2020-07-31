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
            if (typeof(T) == typeof(bool)) return (T)(object)((float)o != 0);
            if (Enum.GetUnderlyingType(typeof(T)) == typeof(byte)) {
                var i = (byte)(int)(float)o;
                return (T)(object)i;
            }
            else
            {
                var i = (int) (float) o;
                return (T)(object)i;
            }
        }
    }
}