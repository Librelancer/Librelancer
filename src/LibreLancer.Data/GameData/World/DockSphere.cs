// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.Schema.Solar;

namespace LibreLancer.Data.GameData.World;

public class DockSphere
{
    public required DockSphereType Type;
    public required string Hardpoint;
    public required int Radius;
    public string? Script;
}
