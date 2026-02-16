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
            GetString(nameof(Dialog), 0,  out Dialog, act.Entry);
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
                    voice = src.NPC.Voice;
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
            GetString(nameof(Source), 0, out Source, act.Entry);
            GetString(nameof(Destination), 1, out Destination, act.Entry);
            GetString(nameof(Line),  2, out Line, act.Entry);
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_SendComm", Source, Destination, Line);
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            var netdlg = new NetDlgLine[1];
            var src = script.Ships[Source];
            var voice = src.NPC.Voice;
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
            GetString(nameof(Voice), 0, out Voice, act.Entry);
            GetInt(nameof(IdsName), 1, out IdsName, act.Entry);
            GetString(nameof(Target), 2, out Target, act.Entry);
            GetString(nameof(Line), 3, out Line, act.Entry);
            GetInt(nameof(Unknown), 4, out Unknown, act.Entry);
            GetString(nameof(Head), 5, out Head, act.Entry);
            if (Head.Equals("no_head", StringComparison.OrdinalIgnoreCase))
            {
                Head = string.Empty;
            }
            GetString(nameof(Body), 6, out Body, act.Entry);
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
