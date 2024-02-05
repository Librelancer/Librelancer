// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Data.Missions;
using LibreLancer.Net.Protocol;

namespace LibreLancer.Missions
{
    public class Act_StartDialog : ScriptedAction
    {
        public string Dialog;

        public Act_StartDialog(MissionAction act) : base(act)
        {
            Dialog = act.Entry[0].ToString();
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            var dlg = script.Dialogs[Dialog];
            var netdlg = new NetDlgLine[dlg.Lines.Count];
            for (int i = 0; i < dlg.Lines.Count; i++)
            {
                var d = dlg.Lines[i];
                string voice = "trent_voice";
                int sourceId = 0;
                if (!d.Source.Equals("Player", StringComparison.OrdinalIgnoreCase))
                {
                    var src = script.Ships[d.Source];
                    var npc = script.NPCs[src.NPC];
                    voice = npc.Voice;
                    var o = runtime.Player.Space.World.GameWorld.GetObject(d.Source);
                    sourceId = o?.NetID ?? 0;
                }

                var hash = FLHash.CreateID(d.Line);
                runtime.EnqueueLine(hash, d.Line);
                netdlg[i] = new NetDlgLine() {
                    Source = sourceId,
                    TargetIsPlayer = d.Target.Equals("Player", StringComparison.OrdinalIgnoreCase),
                    Voice = voice,
                    Hash = hash
                };
            }
            runtime.Player.RpcClient.RunMissionDialog(netdlg);
        }
    }

    public class Act_SendComm : ScriptedAction
    {
        public string Source;
        public string Destination;
        public string Line;

        public Act_SendComm(MissionAction act) : base(act)
        {
            Source = act.Entry[0].ToString();
            Destination = act.Entry[1].ToString();
            Line = act.Entry[2].ToString();
        }
        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            var netdlg = new NetDlgLine[1];
            var src = script.Ships[Source];
            var npc = script.NPCs[src.NPC];
            var voice = npc.Voice;
            var hash = FLHash.CreateID(Line);
            runtime.EnqueueLine(hash, Line);
            int sourceId = 0;
            var o = runtime.Player.Space.World.GameWorld.GetObject(Source);
            sourceId = o?.NetID ?? 0;
            netdlg[0] = new NetDlgLine()
            {
                Source = sourceId,
                TargetIsPlayer = Destination.Equals("Player", StringComparison.OrdinalIgnoreCase),
                Voice = voice,
                Hash = hash
            };
            runtime.Player.RpcClient.RunMissionDialog(netdlg);
        }
    }

    public class Act_EtherComm : ScriptedAction
    {
        public string Voice;
        public string Line;
        public Act_EtherComm(MissionAction act) : base(act)
        {
            Voice = act.Entry[0].ToString();
            Line = act.Entry[3].ToString();
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            var hash = FLHash.CreateID(Line);
            runtime.EnqueueLine(hash, Line);
            runtime.Player.RpcClient.RunMissionDialog(new NetDlgLine[] { new NetDlgLine()
            {
                Source = 0,
                TargetIsPlayer = true,
                Voice = Voice,
                Hash = hash
            }});
        }
    }


}
