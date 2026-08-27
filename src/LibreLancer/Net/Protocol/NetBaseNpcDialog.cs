using WattleScript.Interpreter;
using LibreLancer.Data.GameData.World;

namespace LibreLancer.Net.Protocol;

public static class BaseNpcOptionKind
{
    public const int Rumor = 1;
    public const int Mission = 2;
    public const int Bribe = 3;
    public const int Knowledge = 4;
}

[WattleScriptUserData]
public class NetBaseNpcOption
{
    public int Id;
    public int Kind;
    public int Text;
    public int Contents;
    public int Price;
    public int FactionIdsName;
    public string[] ObjectNames = [];

    public static NetBaseNpcOption ForBribe(int index, BaseNpcBribe bribe) => new()
    {
        Id = 2000 + index,
        Kind = BaseNpcOptionKind.Bribe,
        Text = bribe.Ids,
        Contents = bribe.Ids,
        Price = bribe.Price,
        FactionIdsName = bribe.Faction!.IdsName
    };

    public void Put(PacketWriter message)
    {
        message.PutVariableInt32(Id);
        message.PutVariableInt32(Kind);
        message.PutVariableInt32(Text);
        message.PutVariableInt32(Contents);
        message.PutVariableInt32(Price);
        message.PutVariableInt32(FactionIdsName);
        message.PutVariableUInt32((uint)ObjectNames.Length);
        foreach (var objectName in ObjectNames)
            message.Put(objectName);
    }

    public static NetBaseNpcOption Read(PacketReader message)
    {
        var option = new NetBaseNpcOption
        {
            Id = message.GetVariableInt32(),
            Kind = message.GetVariableInt32(),
            Text = message.GetVariableInt32(),
            Contents = message.GetVariableInt32(),
            Price = message.GetVariableInt32(),
            FactionIdsName = message.GetVariableInt32()
        };

        var objectCount = message.GetVariableUInt32();
        option.ObjectNames = new string[(int)objectCount];
        for (var i = 0; i < option.ObjectNames.Length; i++)
            option.ObjectNames[i] = message.GetString() ?? "";
        return option;
    }
}

[WattleScriptUserData]
public class NetBaseNpcDialog
{
    public string Npc = "";
    public int IndividualName;
    public int Contents;
    public NetBaseNpcOption[] Options = [];
    public uint FocusSystemHash;
    public uint FocusObjectHash;

    public void Put(PacketWriter message)
    {
        message.Put(Npc);
        message.PutVariableInt32(IndividualName);
        message.PutVariableInt32(Contents);
        message.PutVariableUInt32((uint)(Options.Length + 1));
        foreach (var option in Options)
            option.Put(message);
        message.PutVariableUInt32(FocusSystemHash);
        message.PutVariableUInt32(FocusObjectHash);
    }

    public static NetBaseNpcDialog Read(PacketReader message)
    {
        var result = new NetBaseNpcDialog
        {
            Npc = message.GetString() ?? "",
            IndividualName = message.GetVariableInt32(),
            Contents = message.GetVariableInt32()
        };

        var length = message.GetVariableUInt32();
        if (length > 0)
        {
            result.Options = new NetBaseNpcOption[(int)length - 1];
            for (var i = 0; i < result.Options.Length; i++)
                result.Options[i] = NetBaseNpcOption.Read(message);
        }

        result.FocusSystemHash = message.GetVariableUInt32();
        result.FocusObjectHash = message.GetVariableUInt32();

        return result;
    }
}
