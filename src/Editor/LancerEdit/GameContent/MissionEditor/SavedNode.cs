using System;
using System.Numerics;
using LancerEdit.GameContent.MissionEditor.NodeTypes;
using LibreLancer.Data;
using LibreLancer.Data.Ini;

namespace LancerEdit.GameContent.MissionEditor;

public sealed class SavedNode
{
    public bool IsTrigger;
    public string Name;
    public Vector2 Position;
    public Vector2 Size;

    public static SavedNode FromComment(Vector2 pos, CommentNode comment) => new()
    {
        IsTrigger = false,
        Name = comment.BlockName,
        Position = pos,
        Size = comment.Size
    };

    public static SavedNode FromTrigger(Vector2 pos, NodeMissionTrigger trigger) => new()
    {
        IsTrigger = true,
        Name = trigger.Data.Nickname,
        Position = pos
    };

    public static SavedNode FromEntry(Entry e)
    {
        var isTrigger = e[0].ToString().Equals("Trigger", StringComparison.OrdinalIgnoreCase);
        return new SavedNode()
        {
            IsTrigger = isTrigger,
            Name = isTrigger ? e[1].ToString() : CommentEscaping.Unescape(e[1].ToString()),
            Position = new(e[2].ToSingle(), e[3].ToSingle()),
            Size = isTrigger || e.Count <= 4 ? new(100) : new(e[4].ToSingle(), e[5].ToSingle())
        };
    }

    public void Write(IniBuilder.IniSectionBuilder section)
    {
        if (IsTrigger)
        {
            section.Entry("node", "Trigger", Name, Position.X, Position.Y);
        }
        else
        {
            section.Entry("node", "Comment", CommentEscaping.Escape(Name), Position.X, Position.Y, Size.X, Size.Y);
        }
    }
}
