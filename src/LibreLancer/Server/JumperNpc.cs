using System.Text.Json;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Pilots;
using LibreLancer.Net.Protocol;
using LibreLancer.Server.Components;
using LibreLancer.World;
using LibreLancer.World.Components;
using Pilot = LibreLancer.Data.GameData.Pilot;

namespace LibreLancer.Server;

public class JumperNpc
{
    public string Nickname;
    public ObjectName Name;
    public Faction Faction;

    public uint[] DestroyedParts;
    public SpawnedEffect[] Effects;
    public Bodypart CommHead;
    public Bodypart CommBody;
    public Accessory CommHelmet;
    public float Health;
    public ObjectLoadout Loadout;
    public Pilot Pilot;
    public StateGraph StateGraph;

    public static JumperNpc FromGameObject(GameObject go)
    {
        var npc = new JumperNpc();
        npc.Nickname = go.Nickname;
        npc.Name = go.Name;

        var ld = new ObjectLoadout();
        ld.Archetype = go.GetComponent<ShipComponent>().Ship.Nickname;

        if (go.TryGetComponent<SRepComponent>(out var srep))
        {
            npc.Faction = srep.Faction;
        }
        if (go.TryGetComponent<SNPCComponent>(out var snpc))
        {
            npc.Pilot = snpc.Pilot;
            npc.StateGraph = snpc.StateGraph;
            npc.CommHead = snpc.CommHead;
            npc.CommBody = snpc.CommBody;
            npc.CommHelmet = snpc.CommHelmet;
        }
        if (go.TryGetComponent<SHealthComponent>(out var health))
        {
            npc.Health = health.CurrentHealth;
        }

        foreach (var item in go.GetComponents<EquipmentComponent>())
        {
            ld.Items.Add(item.GetLoadoutItem());
        }

        foreach (var item in go.GetChildComponents<EquipmentComponent>())
        {
            ld.Items.Add(item.GetLoadoutItem());
        }

        if (go.TryGetComponent<SNPCCargoComponent>(out var cargo))
        {
            foreach (var cg in cargo.Cargo)
            {
                ld.Cargo.Add(new BasicCargo(cg.Item, cg.Count));
            }
        }

        npc.Loadout = ld;
        return npc;
    }
}
