using System.Collections.Generic;
using LibreLancer.Ini;

namespace LibreLancer.Data.Universe.Rooms;

public class FlashlightLine
{
    [Entry("icolor")] 
    public Color3f IColor;
    [Entry("scale")] 
    public float Scale;
    [Entry("gap")] 
    public float Gap;
    [Entry("blink")] 
    public float Blink;
    [Entry("endpause")] 
    public float EndPause;
    [Entry("numlights")] 
    public int NumLights;
    [Entry("hardpoint", Multiline = true)] 
    public List<string> Hardpoints = new List<string>();
}