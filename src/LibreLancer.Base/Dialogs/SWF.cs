// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Reflection;
namespace LibreLancer.Dialogs
{
    static class SWF
    {
        const string WINFORMS_NAME = "System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
        static Assembly winforms;
        static SWF()
        {
            winforms = Assembly.Load(WINFORMS_NAME);
        }
        public static dynamic New(string type)
        {
            return Activator.CreateInstance(winforms.GetType(type));
        }
        public static dynamic Enum(string e, string val)
        {
            var type = winforms.GetType("System.Windows.Forms." + e);
            return System.Enum.Parse(type, val);
        }
        public static void ApplicationDoEvents()
        {
            var t = winforms.GetType("System.Windows.Forms.Application");
            var method = t.GetMethod("DoEvents", BindingFlags.Public | BindingFlags.Static);
            method.Invoke(null, null);
        }
        public static void ApplicationRun(dynamic form)
        {
            var t = winforms.GetType("System.Windows.Forms.Application");
            var method = t.GetMethod("Run", BindingFlags.Public | BindingFlags.Static, null,
            new Type[] { winforms.GetType("System.Windows.Forms.Form") }, null);
            method.Invoke(null, new object[] { form });
        }
    }
}
