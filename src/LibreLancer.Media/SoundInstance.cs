// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Numerics;
using System.Threading;
using SharpDX.MediaFoundation;

namespace LibreLancer.Media
{
    public class SoundInstance : IDisposable
    {
        struct SourceProperties
        {
            public Vector3 Position;
            public Vector3 Velocity;
            public Vector3 Direction;
            public bool Is3D;
            public float Gain;
            public float Pitch;
            public float ReferenceDistance;
            public float MaxDistance;
            public float ConeInnerAngle; //degrees
            public float ConeOuterAngle; //degrees
            public float ConeOuterGain;

            public void DefaultValues()
            {
                Position = Velocity = Direction = Vector3.Zero;
                Gain = 1f;
                Pitch = 1f;
                ReferenceDistance = 0;
                MaxDistance = float.MaxValue;
                ConeInnerAngle = 360;
                ConeOuterAngle = 360;
                ConeOuterGain = 0;
            }
        }

        
        private const int FLAG_POS = 1 << 0;
        private const int FLAG_VEL = 1 << 1;
        private const int FLAG_GAIN = 1 << 2;
        private const int FLAG_PITCH = 1 << 3;
        private const int FLAG_DIST = 1 << 4;
        private const int FLAG_CONE = 1 << 5;
        private const int FLAG_DIRECTION = 1 << 6;

        void SetSourceProperties(SourceProperties prop, int flags)
        {
            var src = man.AM_GetInstanceSource(ID, false);
            if (src != uint.MaxValue)
                SetPropertiesAl(src, ref prop, flags);
        }
        static void SetPropertiesAl(uint src, ref SourceProperties prop, int flags)
        {
            if ((flags & FLAG_POS) == FLAG_POS)
            {
                Al.alSourcei(src, Al.AL_SOURCE_RELATIVE, prop.Is3D ? 0 : 1);
                Al.alSource3f(src, Al.AL_POSITION, prop.Position.X, prop.Position.Y, prop.Position.Z);
            }
            if ((flags & FLAG_VEL) == FLAG_VEL)
            {
                Al.alSource3f(src, Al.AL_VELOCITY, prop.Velocity.X, prop.Velocity.Y, prop.Velocity.Z);
            }
            if ((flags & FLAG_DIRECTION) == FLAG_DIRECTION)
            {
                Al.alSource3f(src, Al.AL_DIRECTION, prop.Direction.X, prop.Direction.Y, prop.Direction.Z);
            }
            if ((flags & FLAG_GAIN) == FLAG_GAIN)
            {
                Al.alSourcef(src, Al.AL_GAIN, prop.Gain);
            }
            if ((flags & FLAG_PITCH) == FLAG_PITCH)
            {
                Al.alSourcef(src, Al.AL_PITCH, prop.Pitch);
            }
            if ((flags & FLAG_DIST) == FLAG_DIST)
            {
                Al.alSourcef(src, Al.AL_REFERENCE_DISTANCE, prop.ReferenceDistance);
                Al.alSourcef(src, Al.AL_MAX_DISTANCE, prop.MaxDistance);
            }
            if ((flags & FLAG_CONE) == FLAG_CONE)
            {
                Al.alSourcef(src, Al.AL_CONE_INNER_ANGLE, prop.ConeInnerAngle);
                Al.alSourcef(src, Al.AL_CONE_OUTER_ANGLE, prop.ConeOuterAngle);
                Al.alSourcef(src, Al.AL_CONE_OUTER_GAIN, prop.ConeOuterGain);
            }
        }
        
        public bool DisposeOnStop = false;

        public volatile bool Playing = false;
        
        private SourceProperties properties;
        private int dirtyFlags = 0;
        private SoundData data;
        internal uint ID;
        private AudioManager man;

        public int Priority = 0;
        internal SoundInstance(uint id, AudioManager manager, SoundData data)
        {
            man = manager;
            ID = id;
            this.data = data;
            properties.DefaultValues();
        }

        private bool gainSet = false;
        private float attenuation = 0;
        public void SetAttenuation(float attenuation)
        {
            this.attenuation = attenuation;
            UpdateAttenuation();
        }

        public void Set3D()
        {
            dirtyFlags |= FLAG_POS;
            properties.Is3D = true;
        }
        
        

        internal void UpdateAttenuation()
        {
            var gain = ALUtils.ClampVolume(ALUtils.DbToAlGain(attenuation) * man.GetVolume(SoundType.Sfx));
            gainSet = true;
            dirtyFlags |= FLAG_GAIN;
            properties.Gain = gain;
        }

        public void SetPropertyDefaults()
        {
            properties.DefaultValues();
            SetAttenuation(0);
            dirtyFlags = int.MaxValue;
            gainSet = false;
        }
        
        public void Play(bool loop = false, float timeOffset = 0)
        {
            if(ID == uint.MaxValue) throw new ObjectDisposedException("SoundInstance");
            var props = properties;
            if (!gainSet)
                UpdateAttenuation();
            dirtyFlags = 0;
            Playing = true;
            man.Do(() =>
            {
                var src = man.AM_GetInstanceSource(ID, true);
                if (src == uint.MaxValue)
                {
                    dirtyFlags = int.MaxValue;
                    Playing = false;
                    return;
                }
                SetPropertiesAl(src, ref props, int.MaxValue);
                if(timeOffset > 0)
                    Al.alSourcef(src, Al.AL_SEC_OFFSET, timeOffset);
                Al.alSourcei(src, Al.AL_BUFFER, (int)data.ID);
                Al.alSourcei(src, Al.AL_LOOPING, loop ? 1 : 0);
                Al.alSourcePlay(src);
                man.AM_AddInstance(ID);
            });
        }
        
        public void SetPosition(Vector3 pos)
        {
            if(float.IsNaN(pos.X) || float.IsNaN(pos.Y) || float.IsNaN(pos.Z)) {
                //NaN ??? - Exit
                FLLog.Error("Sound", "Attempted to set NaN pos");
            } else
            {
                dirtyFlags |= FLAG_POS;
                properties.Position = pos;
            }
        }
        public void SetVelocity(Vector3 pos)
        {
            dirtyFlags |= FLAG_VEL;
            properties.Velocity = pos;
        }

        public void SetPitch(float pitch)
        {
            dirtyFlags |= FLAG_PITCH;
            properties.Pitch = pitch;
        }

        public void SetDistance(float minDist, float maxDist)
        {
            dirtyFlags |= FLAG_DIST;
            properties.ReferenceDistance = minDist;
            properties.MaxDistance = maxDist;
        }

        public void SetCone(float innerAngle, float outerAngle, float outsideGain)
        {
            dirtyFlags |= FLAG_CONE;
            properties.ConeInnerAngle = innerAngle;
            properties.ConeOuterAngle = outerAngle;
            properties.ConeOuterGain = outsideGain;
        }

        public Action OnStop;
        public void UpdateProperties()
        {
            if (ID == uint.MaxValue) return;
            var props = properties;
            var flags = dirtyFlags;
            dirtyFlags = 0;
            man.Do(() =>
            {
                SetSourceProperties(props, flags);
            });
        }

        internal void Stopped()
        {
            if (DisposeOnStop)
                Dispose();
            Playing = false;
            if (OnStop != null) {
                man.UIThread.QueueUIThread(OnStop);
            }
        }
        public void Stop()
        {
            uint _id = ID;
            Playing = false;
            man.Do(() =>
            {
                var src = man.AM_GetInstanceSource(_id, false);
                if (src != uint.MaxValue) {
                    Al.alSourceStopv(1, ref src);
                }
            });
        }

        public bool Disposed => ID == uint.MaxValue;

        internal void Dispose(bool audioManager)
        {
            
            if(ID == uint.MaxValue && !audioManager) throw new ObjectDisposedException("SoundInstance");
            if (ID == uint.MaxValue) return;
            var _id = ID;
            ID = uint.MaxValue;
            if (audioManager)
            {
                man.AM_ReleaseInstance(_id);
            }
            else
            {
                man.Do(() => { man.AM_ReleaseInstance(_id); });
            }
        }
        public void Dispose()
        {
            Dispose(false);
        }
    }
}
