// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer
{
    public class CNetPositionComponent : GameComponent
    {
        private const int BUFFER_MS = 80;
        struct PosState
        {
            public uint Tick;
            public Vector3 Pos;
        }
        struct OrientState
        {
            public uint Tick;
            public Quaternion Orient;
        }

        private CircularBuffer<PosState> posBuffer = new CircularBuffer<PosState>(30);
        private CircularBuffer<OrientState> orientBuffer = new CircularBuffer<OrientState>(30);
        
        private double receivedPosTime = 0;
        private double receivedOrientTime = 0;
        
        //t1 - t2 but taking wrapping into account
        static int SeqDiff(uint t1, uint t2)
        {
            return Diff((int)t1, (int)t2, (int)(LNetConst.MAX_TICK_MS / 2));
        }
        static int Diff(int a, int b, int halfMax)
        {
            return (a - b + halfMax*3) % (halfMax*2) - halfMax;
        }
        
        public CNetPositionComponent(GameObject parent) : base(parent)
        {
        }

        public void QueuePosition(uint ms, Vector3 pos)
        {
            uint lastMs = 0;
            if (posBuffer.Count > 0)
                lastMs = posBuffer.Peek().Tick;
            receivedPosTime += SeqDiff(ms, lastMs);
            if (posBuffer.Count == posBuffer.Capacity)
            {
                FLLog.Warning("Net", "Something bad happened lerp pos");
                receivedPosTime = 0;
                posBuffer.Clear();
            }

            posBuffer.Enqueue(new PosState()
            {
                Tick = ms, Pos =  pos
            });
        }
        
        public void QueueOrientation(uint ms, Quaternion orient)
        {
            uint lastMs = 0;
            if (orientBuffer.Count > 0)
                lastMs = orientBuffer.Peek().Tick;
            receivedOrientTime += SeqDiff(ms, lastMs);
            if (orientBuffer.Count == orientBuffer.Capacity)
            {
                FLLog.Warning("Net", "Something bad happened lerp orient");
                receivedPosTime = 0;
                posBuffer.Clear();
            }
            orientBuffer.Enqueue(new OrientState()
            {
                Tick = ms, Orient = orient
            });
        }
        public override void FixedUpdate(TimeSpan time)
        {
            UpdatePosition(time);
            UpdateOrientation(time);
            if (setV && setQ)
            {
                Parent.Transform = Matrix4x4.CreateFromQuaternion(currentQuat) *
                                   Matrix4x4.CreateTranslation(currentPos);
            }
        }

        private bool setV = false;
        private bool setQ = false;
        private Vector3 currentPos;
        private Quaternion currentQuat;
        private TimeSpan posTimer;
        private TimeSpan quatTimer;
        void UpdatePosition(TimeSpan delta)
        {
            if (receivedPosTime < BUFFER_MS || posBuffer.Count < 2)
            {
                setV = false;
                return;
            }
            setV = true;
            var dataA = posBuffer[0];
            var dataB = posBuffer[1];
            var lerpTime = TimeSpan.FromMilliseconds(SeqDiff(dataB.Tick, dataA.Tick));
            var t = posTimer / lerpTime;
            currentPos = Vector3.Lerp(dataA.Pos, dataB.Pos, (float)t);
            posTimer += delta;
            if (posTimer > lerpTime)
            {
                receivedPosTime -= lerpTime.TotalMilliseconds;
                posBuffer.Dequeue();
                posTimer -= lerpTime;
            }
        }
        
        void UpdateOrientation(TimeSpan delta)
        {
            if (receivedOrientTime < BUFFER_MS || orientBuffer.Count < 2)
            {
                setQ = false;
                return;
            }
            setQ = true;
            var dataA = orientBuffer[0];
            var dataB = orientBuffer[1];
            var lerpTime = TimeSpan.FromMilliseconds(SeqDiff(dataB.Tick, dataA.Tick));
            var t = quatTimer / lerpTime;
            currentQuat = Quaternion.Slerp(dataA.Orient, dataB.Orient, (float) t);
            quatTimer += delta;
            if (quatTimer > lerpTime)
            {
                receivedOrientTime -= (int) lerpTime.TotalMilliseconds;
                orientBuffer.Dequeue();
                quatTimer -= lerpTime;
            }
        }
    }
}