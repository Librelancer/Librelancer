// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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
        
        public Act_RelocateShip(MissionAction act)
        {
            Ship = act.Entry[0].ToString();
            Position = new Vector3(act.Entry[1].ToSingle(), act.Entry[2].ToSingle(),
                act.Entry[3].ToSingle());
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            if (script.Ships.ContainsKey(Ship))
            {
                runtime.Player.WorldAction(() =>
                {
                    var obj = runtime.Player.World.GameWorld.GetObject(Ship);
                    obj.SetLocalTransform(Matrix4x4.CreateTranslation(Position));
                });
            }   
        }
    }
}