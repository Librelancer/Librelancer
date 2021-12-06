// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data.Missions;
using LibreLancer.Missions;

namespace LibreLancer.Gameplay.Missions
{
    public class Act_MovePlayer : ScriptedAction
    {
        public Vector3 Position;
        public float Unknown; //1 in M01A
        
        public Act_MovePlayer(MissionAction act)
        {
            Position = new Vector3(act.Entry[0].ToSingle(), act.Entry[1].ToSingle(),
                act.Entry[2].ToSingle());
            if(act.Entry.Count > 3)
                Unknown = act.Entry[3].ToSingle(); 
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            runtime.Player.RemoteClient.ForceMove(Position);
        }
    }

    public class Act_RelocateShip : ScriptedAction
    {
        public string Ship;
        public Vector3 Position;
        public Quaternion? Quaternion;
        
        public Act_RelocateShip(MissionAction act)
        {
            Ship = act.Entry[0].ToString();
            Position = new Vector3(act.Entry[1].ToSingle(), act.Entry[2].ToSingle(),
                act.Entry[3].ToSingle());
            if (act.Entry.Count > 4)
            {
                Quaternion = new Quaternion(act.Entry[5].ToSingle(), act.Entry[6].ToSingle(), act.Entry[7].ToSingle(),
                    act.Entry[4].ToSingle());
            }
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            if (Ship.Equals("player", StringComparison.OrdinalIgnoreCase))
            {
                runtime.Player.RemoteClient.ForceMove(Position);
            }
            else if (script.Ships.ContainsKey(Ship))
            {
                runtime.Player.WorldAction(() =>
                {
                    var obj = runtime.Player.World.GameWorld.GetObject(Ship);
                    var quat = Quaternion ?? obj.LocalTransform.ExtractRotation();
                    obj.SetLocalTransform(Matrix4x4.CreateFromQuaternion(quat) * Matrix4x4.CreateTranslation(Position));
                });
            }   
        }
    }
}