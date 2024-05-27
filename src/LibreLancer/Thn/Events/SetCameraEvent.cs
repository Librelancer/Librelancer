// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Thorn;

namespace LibreLancer.Thn.Events
{
    public class SetCameraEvent : ThnEvent
    {
        public SetCameraEvent() { }

        public SetCameraEvent(ThornTable table) : base(table) { }

        public override void Run(ThnScriptInstance instance)
        {
            if (instance.Objects.TryGetValue(Targets[0], out var monitor))
            {
                if (monitor.Entity.Type != EntityTypes.Monitor)
                {
                    FLLog.Error("Thn", $"Tried to set camera on non-monitor {Targets[0]}");
                    return;
                }
                if (monitor.MonitorIndex != 0)
                {
                    FLLog.Debug("Thn", $"SET_CAMERA: Ignoring on non-main monitor {Targets[0]}");
                    return;
                }
            }
            instance.Cutscene.SetCamera(Targets[1]);
        }
    }
}
