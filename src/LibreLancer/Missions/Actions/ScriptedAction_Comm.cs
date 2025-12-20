// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Net.Protocol;

namespace LibreLancer.Missions.Actions
{
    public class Act_StartDialog : ScriptedAction
    {
        public string Dialog = string.Empty;

        public Act_StartDialog()
        {
        }
        public Act_StartDialog(MissionAction act) : base(act)
        {
            Dialog = act.Entry[0].ToString();
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_StartDialog", Dialog);
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
        public string Source = string.Empty;
        public string Destination = string.Empty;
        public string Line = string.Empty;

        public Act_SendComm()
        {
        }
        public Act_SendComm(MissionAction act) : base(act)
        {
            Source = act.Entry[0].ToString();
            Destination = act.Entry[1].ToString();
            Line = act.Entry[2].ToString();
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_SendComm", Source, Destination, Line);
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
        public string Voice = string.Empty;
        public int IdsName;
        public string Target = string.Empty;
        public string Line = string.Empty;
        // Priority or maybe duration.
        public int Unknown = -1;
        public string Head = string.Empty;
        public string Body = string.Empty;
        public string Accessory = string.Empty;

        public Act_EtherComm()
        {
        }

        public Act_EtherComm(MissionAction act) : base(act)
        {
            Voice = act.Entry[0].ToString();
            IdsName = act.Entry[1].ToInt32();
            Target = act.Entry[2].ToString();
            Line = act.Entry[3].ToString();
            Unknown = act.Entry[4].ToInt32();
            Head = act.Entry[5].ToString()!;
            if (Head.Equals("no_head", StringComparison.OrdinalIgnoreCase))
            {
                Head = string.Empty;
            }
            Body = act.Entry[6].ToString();
            if (act.Entry.Count > 7)
            {
                Accessory =  act.Entry[7].ToString();
            }
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            List<ValueBase> values =
                [Voice, IdsName, Target, Line, Unknown, string.IsNullOrWhiteSpace(Head) ? "no_head" : Head, Body];
            if(!string.IsNullOrWhiteSpace(Accessory))
                values.Add(Accessory);
            section.Entry("Act_EtherComm", values.ToArray());
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            var hash = FLHash.CreateID(Line);
            runtime.EnqueueLine(hash, Line);
            runtime.Player.RpcClient.RunMissionDialog([
                new NetDlgLine()
            {
                Source = 0,
                TargetIsPlayer = true,
                Voice = Voice,
                Hash = hash
            }
            ]);
        }
    }
}
