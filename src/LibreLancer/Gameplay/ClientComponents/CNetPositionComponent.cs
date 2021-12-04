// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer
{
    public class CNetPositionComponent : GameComponent
    {
        public int BufferTime = 80;
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
        public override void FixedUpdate(double time)
        {
            UpdatePosition(time);
            UpdateOrientation(time);
            if (setV && setQ)
            {
                Parent.SetLocalTransform(Matrix4x4.CreateFromQuaternion(currentQuat) *
                                         Matrix4x4.CreateTranslation(currentPos));
            }
        }

        private bool setV = false;
        private bool setQ = false;
        private Vector3 currentPos;
        private Quaternion currentQuat;
        private double posTimer;
        private double quatTimer;
        void UpdatePosition(double delta)
        {
            if (receivedPosTime < BufferTime || posBuffer.Count < 2)
            {
                setV = false;
                return;
            }
            setV = true;
            var dataA = posBuffer[0];
            var dataB = posBuffer[1];
            var lerpTime = SeqDiff(dataB.Tick, dataA.Tick) / 1000.0;
            if (lerpTime == 0) {
                currentPos = dataB.Pos;
            }
            else {
                var t = posTimer / lerpTime;
                currentPos = Vector3.Lerp(dataA.Pos, dataB.Pos, (float) t);
                posTimer += delta;
            }

            if (posTimer > lerpTime)
            {
                receivedPosTime -= (int)(lerpTime * 1000);
                posBuffer.Dequeue();
                posTimer -= lerpTime;
            }

            if (float.IsNaN(currentPos.X)) throw new Exception("NaN position");
        }
        
        void UpdateOrientation(double delta)
        {
            if (receivedOrientTime < BufferTime || orientBuffer.Count < 2)
            {
                setQ = false;
                return;
            }
            setQ = true;
            var dataA = orientBuffer[0];
            var dataB = orientBuffer[1];
            var lerpTime = SeqDiff(dataB.Tick, dataA.Tick) / 1000.0;
            if (lerpTime == 0) {
                currentQuat = dataB.Orient;
            }
            else {
                var t = quatTimer / lerpTime;
                currentQuat = Quaternion.Slerp(dataA.Orient, dataB.Orient, (float) t);
            }
            quatTimer += delta;
            if (quatTimer > lerpTime)
            {
                receivedOrientTime -= (int) (lerpTime * 1000);
                orientBuffer.Dequeue();
                quatTimer -= lerpTime;
            }
            if (float.IsNaN(currentQuat.X)) throw new Exception("NaN orientation");

        }
    }
}