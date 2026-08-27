// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;

namespace LibreLancer.Thn;

public sealed class ThnScriptLayer : IDisposable
{
    private Cutscene? owner;

    internal ThnScriptLayer(
        Cutscene owner,
        ThnScriptInstance instance,
        Dictionary<string, ThnLayerBinding> bindings,
        Dictionary<string, ThnSceneObject> createdObjects)
    {
        this.owner = owner;
        Instance = instance;
        Bindings = bindings;
        CreatedObjects = createdObjects;
    }

    public ThnScriptInstance Instance { get; }
    public bool Running => Instance.Running;

    internal IReadOnlyDictionary<string, ThnLayerBinding> Bindings { get; }
    internal IReadOnlyDictionary<string, ThnSceneObject> CreatedObjects { get; }

    public void Dispose()
    {
        var currentOwner = owner;
        if (currentOwner == null)
            return;

        owner = null;
        currentOwner.RemoveLayer(this);
    }

    internal void Detach() => owner = null;
}

internal readonly record struct ThnLayerBinding(
    ThnSceneObject? Previous,
    ThnSceneObject Bound);
