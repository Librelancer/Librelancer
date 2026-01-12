// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Numerics;

namespace LibreLancer.Media;

public class SoundInstance
{
    internal struct SourceProperties
    {
        public bool Is3D;
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 Direction;
        public float Attenuation;
        public float Pitch;
        public float ReferenceDistance;
        public float MaxDistance;
        public float ConeInnerAngle; //degrees
        public float ConeOuterAngle; //degrees
        public float ConeOuterGain;

        public void DefaultValues()
        {
            Position = Velocity = Direction = Vector3.Zero;
            Attenuation = 0;
            Pitch = 1f;
            ReferenceDistance = 0;
            MaxDistance = 1_000_000_000;
            ConeInnerAngle = 360;
            ConeOuterAngle = 360;
            ConeOuterGain = 0;
        }
    }


    internal SourceProperties SetProperties;
    internal bool Active;
    internal bool Looping;
    internal double StartTime;
    internal int Source = -1;
    internal uint Buffer;
    internal readonly SoundData Data;
    private readonly AudioManager man;

    public volatile bool Playing = false;

    public int Priority = 0;

    public SoundCategory Category { get; private set; }

    internal SoundInstance(AudioManager manager, SoundData data, SoundCategory category)
    {
        man = manager;
        Data = data;
        Category = category;
        SetProperties.DefaultValues();
    }

    public void SetAttenuation(float attenuation)
    {
        man.QueueMessage(new AudioEventMessage()
        {
            Type = AudioEvent.SetAttenuation,
            Instance = this,
            Data = new Vector3(attenuation, 0, 0)
        });
    }

    public void Set3D()
    {
        man.QueueMessage(new AudioEventMessage()
        {
            Type = AudioEvent.Set3D,
            Instance = this,
            Data = new Vector3(1, 0, 0)
        });
    }

    public void Play(bool loop = false, float timeOffset = 0)
    {
        if(Data.Disposed) throw new ObjectDisposedException("SoundInstance.Data");
        Playing = true;
        man.QueueMessage(new AudioEventMessage()
        {
            Type = AudioEvent.Play,
            Instance = this,
            Data = new(timeOffset, loop ? 1f : 0f, 0f)
        });
    }

    public void SetPosition(Vector3 pos)
    {
        if(float.IsNaN(pos.X) || float.IsNaN(pos.Y) || float.IsNaN(pos.Z))
        {
            //NaN ??? - Exit
            FLLog.Error("Sound", "Attempted to set NaN pos");
        }
        else
        {
            man.QueueMessage(new AudioEventMessage()
            {
                Type = AudioEvent.SetPosition,
                Instance = this,
                Data = pos
            });
        }
    }
    public void SetVelocity(Vector3 pos) =>
        man.QueueMessage(new AudioEventMessage()
        {
            Type = AudioEvent.SetVelocity,
            Instance = this,
            Data = pos
        });

    public void SetPitch(float pitch) =>
        man.QueueMessage(new AudioEventMessage()
        {
            Type = AudioEvent.SetPitch,
            Instance = this,
            Data = new Vector3(pitch, 0 ,0)
        });

    public void SetDistance(float minDist, float maxDist) =>
        man.QueueMessage(new AudioEventMessage()
        {
            Type = AudioEvent.SetDistance,
            Instance = this,
            Data = new Vector3(minDist, maxDist, 0)
        });

    public void SetCone(float innerAngle, float outerAngle, float outsideDb)
    {
        man.QueueMessage(new AudioEventMessage()
        {
            Type = AudioEvent.SetCone,
            Instance = this,
            Data = new Vector3(innerAngle, outerAngle,
                ALUtils.ClampVolume(ALUtils.DbToAlGain(outsideDb)))
        });
    }

    public Action? OnStop;
    public void Stop()
    {
        man.QueueMessage(new AudioEventMessage()
        {
            Type = AudioEvent.Stop,
            Instance = this
        });
    }

}
