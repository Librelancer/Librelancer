// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Threading;

namespace LibreLancer.Media
{
    public class SoundInstance
    {
        internal volatile bool Active = true;
        internal SoundData Dispose;
        internal Action OnFinish;
        internal uint ID;
        internal SoundInstance(uint id)
        {
            ID = id;
        }
        public void SetGain(float gain)
        {
            if(Active)
                Al.alSourcef(ID, Al.AL_GAIN, gain);
            Al.CheckErrors();
        }
        public void SetPosition(Vector3 pos)
        {
            if(float.IsNaN(pos.X) || float.IsNaN(pos.Y) || float.IsNaN(pos.Z)) {
                //NaN ??? - Exit
                FLLog.Error("Sound", "Attempted to set NaN pos");
                return;
            }
            if (Active)
                Al.alSource3f(ID, Al.AL_POSITION, pos.X, pos.Y, pos.Z);
            Al.CheckErrors();
        }
        public void SetVelocity(Vector3 pos)
        {
            if (Active)
                Al.alSource3f(ID, Al.AL_VELOCITY, pos.X, pos.Y, pos.Z);
            Al.CheckErrors();
        }
        public void Stop()
        {
            if (Active)
            {
                Al.alSourceStopv(1, ref ID);
                Al.CheckErrors();
                Active = false;
            }
        }
    }
}
