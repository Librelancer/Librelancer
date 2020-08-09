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
            int integer = 0;
            if (o is float f) integer = (int) f;
            else if (o is int i) integer = i;
            else throw new Exception($"Unable to change type {o.GetType()} to enum");
            if (typeof(T) == typeof(bool)) return (T)(object)(integer != 0);
            return (T) (object) integer;
        }
        
    }
}