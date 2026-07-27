using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using LibreLancer;
using LibreLancer.Data.GameData.Archetypes;

namespace LancerEdit.GameContent.Stars;

public sealed class StarPreviewState
{
    private static readonly ConditionalWeakTable<GameDataContext, Dictionary<string, StarPreviewState>> States = new();

    public float Zoom = 1f;
    public Vector2 ViewOffset = Vector2.Zero;

    public static StarPreviewState Get(GameDataContext context, Sun sun)
    {
        var states = States.GetValue(context, _ => new Dictionary<string, StarPreviewState>(StringComparer.OrdinalIgnoreCase));
        if (!states.TryGetValue(sun.Nickname, out var state))
        {
            state = new StarPreviewState();
            states[sun.Nickname] = state;
        }
        return state;
    }

    public void Reset()
    {
        Zoom = 1f;
        ViewOffset = Vector2.Zero;
    }
}
