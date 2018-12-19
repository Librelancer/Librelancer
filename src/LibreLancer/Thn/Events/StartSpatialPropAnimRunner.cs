// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Thorn;
namespace LibreLancer
{
    [ThnEventRunner(EventTypes.StartSpatialPropAnim)]
    public class StartSpatialPropAnimRunner : IThnEventRunner
    {
        public void Process(ThnEvent ev, Cutscene cs)
        {
            ThnObject obj; 
            if(!cs.Objects.TryGetValue((string)ev.Targets[0], out obj))
            {
                FLLog.Error("Thn", "Object does not exist " + (string)ev.Targets[0]);
                return;
            }

            var props = (LuaTable)ev.Properties["spatialprops"];
            Matrix4? orient = null;
            object tmp;
            if (ev.Properties.TryGetValue("orient", out tmp))
            {
                orient = ThnScript.GetMatrix((LuaTable)tmp);
            }
            if (obj.Camera != null)
            {
                if (orient != null) obj.Camera.Orientation = orient.Value;
                if (ev.Duration > 0)
                {
                    FLLog.Error("Thn", "spatialpropanim.duration > 0 - unimplemented");
                    //return;
                }
            }
            if (obj.Camera == null)
            {
                FLLog.Error("Thn", "StartSpatialPropAnim unimplemented");
            }
        }

    }
}
