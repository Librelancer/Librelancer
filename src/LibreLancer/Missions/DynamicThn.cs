using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data;
using LibreLancer.Data.Schema.Save;
using LibreLancer.Net;

namespace LibreLancer.Missions;

public record AmbientInfo(string Script, string Room, string Base);

public class DynamicThn
{
    private Dictionary<string, MissionRtc> rtcs = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, AmbientInfo> ambients = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<MissionRtc> Rtcs => rtcs.Values;
    public IEnumerable<AmbientInfo> Ambients => ambients.Values;

    public void AddRTC(string rtc)
    {
        rtcs[rtc] = (new MissionRtc(rtc, false));
    }

    public void RemoveRTC(string rtc)
    {
        rtcs.Remove(rtc);
    }

    public void AddAmbient(string script, string room, string _base)
    {
        ambients[script] = new AmbientInfo(script, room, _base);
    }

    public void RemoveAmbient(string script)
    {
        ambients.Remove(script);
    }

    public void Reset()
    {
        rtcs = new(StringComparer.OrdinalIgnoreCase);
        ambients = new(StringComparer.OrdinalIgnoreCase);
    }

    public NetThnInfo Pack() => new (){
        Rtcs = rtcs.Values.ToArray(),
        Ambients = ambients.Values.Select(x =>
        {
            var roomHash = FLHash.CreateLocationID(x.Base, x.Room);
            return new NetAmbientInfo(x.Script, roomHash, FLHash.CreateID(x.Base));
        }).ToArray()
    };

    public void Unpack(NetThnInfo info, GameDataManager gameData)
    {
        //Reset dictionaries
        rtcs = new(StringComparer.OrdinalIgnoreCase);
        ambients = new(StringComparer.OrdinalIgnoreCase);
        //Fill
        if (info.Rtcs != null)
        {
            foreach (var rtc in info.Rtcs)
                rtcs[rtc.Script] = rtc;
        }

        if (info.Ambients != null)
        {
            foreach (var ambient in info.Ambients)
            {
                var _base = gameData.Items.Bases.Get(ambient.BaseId);
                var room = _base.Rooms.Get(ambient.RoomId);
                ambients[ambient.Script] = new AmbientInfo(ambient.Script, room.Nickname, _base.Nickname);
            }
        }
    }
}
