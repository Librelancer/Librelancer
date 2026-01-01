// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe;

public abstract class ZoneReference
{
    [Entry("file", Required = true)]
    public string IniFile = null!;
    [Entry("zone", Required = true)]
    public string ZoneName = null!;
    [Section("texturepanels")]
    public TexturePanelsRef TexturePanels = new();
    [Section("properties")]
    public ObjectProperties Properties = new();
}
