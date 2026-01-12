using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace LibreLancer.ImUI;

public static class Icons
{
    //Unicode char
    public const char BulletEmpty = '\u25CB'; //NOT included in GetChars(), made from empty-bullet.ttf
    //Icons straight from font
    public const char ArrowUp = '\uf062';
    public const char ArrowDown = '\uf063';
    public const char ArrowLeft = '\uf060';
    public const char ArrowRight = '\uf061';
    public const char TurnUp = '\uf3bf';
    public const char BezierCurve = '\uf55b';
    public const char Book = '\uf02d';
    public const char BookOpen = '\uf518';
    public const char BoxOpen = '\uf49e';
    public const char Check = '\uf00c';
    public const char Info = '\uf129';
    public const char File = '\uf15b';
    public const char Fire = '\uf06d';
    public const char Grip = '\uf58d';
    public const char Export = '\uf56e';
    public const char Import = '\uf56f';
    public const char Keyboard = '\uf11c';
    public const char List = '\uf03a';
    public const char Open = '\uf07c';
    public const char Save = '\uf0c7';
    public const char Quit = '\uf52b';
    public const char X = '\uf00d';
    public const char Log = '\uf022';
    public const char Cog = '\uf013';
    public const char FileImport = '\uf56f';
    public const char FileExport = '\uf56e';
    public const char Palette = '\uf53f';
    public const char Play = '\uf04b';
    public const char Backward = '\uf04a';
    public const char SprayCan = '\uf5bd';
    public const char Exchange = '\uf362';
    public const char Edit = '\uf044';
    public const char Eye = '\uf06e';
    public const char TrashAlt = '\uf2ed';
    public const char Clone = '\uf24d';
    public const char PlusCircle = '\uf055';
    public const char Cut = '\uf0c4';
    public const char Copy = '\uf0c5';
    public const char Paste = '\uf0ea';
    public const char Eraser = '\uf12d';
    public const char MagnifyingGlass = '\uf002';
    public const char StreetView = '\uf21d';
    public const char Paintbrush = '\uf1fc';
    public const char Star = '\uf005';
    public const char Table = '\uf0ce';
    public const char UpRightFromSquare = '\uf35d';
    public const char VectorSquare = '\uf5cb';
    public const char Gift = '\uf06b';
    public const char Tree = '\uf1bb';
    public const char PersonRunning = '\uf70c';
    public const char Bone = '\uf5d7';
    public const char Calculator = '\uf1ec';
    public const char Map = '\uf279';
    public const char Newspaper = '\uf1ea';
    public const char SquarePlus = '\uf0fe';
    public const char SquareMinus = '\uf146';
    public const char SquareCorners = '\uf065';
    public const char SquareCornersInverted = '\uf066';
    //View Mode
    public const char Image = '\uf03e';
    public const char Lightbulb = '\uf0eb';
    public const char ArrowsAltH = '\uf337';
    public const char EyeSlash = '\uf070';
    public const char PenSquare = '\uf14b';
    public const char Video = '\uf03d';
    //ALE icon
    public const char Bolt = '\uf0e7';
    public const char Cloud = '\uf0c2';
    public const char Wind = '\uf72e';
    public const char Bullseye = '\uf140';
    public const char Cube = '\uf6d1'; //Orig: DiceD6
    public const char Globe = '\uf0ac';
    public const char AngleDoubleDown = '\uf103';
    public const char Leaf = '\uf06c';
    public const char Splotch = '\uf5bc';
    public const char Fan = '\uf863';
    public const char CarCrash = '\uf5e1';
    public const char Images = '\uf302';
    public const char Stop = '\uf04d';
    public const char IceCream = '\uf810';
    //Icons to be tinted
    public const char SyncAlt = '\uf2f1';
    private const char ExpandArrowsAlt = '\uf31e';
    private const char SignInAlt = '\uf2f6';
    private const char ExclamationTriangle = '\uf071';

    //Tinted icons
    public static readonly char Con_Pris;
    public static readonly char Con_Sph;
    public static readonly char Tree_DarkGreen;
    public static readonly char Tree_Red;

    public static readonly char Hardpoints;

    public static readonly char Cube_LightPink;
    public static readonly char Cube_Purple;
    public static readonly char Cube_LightYellow;
    public static readonly char Cube_LightGreen;
    public static readonly char Cube_LightSkyBlue;
    public static readonly char Cube_Coral;

    public static readonly char Rev_LightSeaGreen;
    public static readonly char Rev_LightGreen;
    public static readonly char Rev_LightCoral;
    public static readonly char Rev_Coral;

    public static readonly char Warning;

    public static void Init() => GC.KeepAlive(Warning); //Run static constructor

    static Icons()
    {
        Tint(out Con_Pris, ExpandArrowsAlt, Color4.LightPink);
        Tint(out Con_Sph, Globe, Color4.LightGreen);
        Tint(out Tree_DarkGreen, Tree, Color4.DarkGreen);
        Tint(out Tree_Red, Tree, Color4.IndianRed);
        Tint(out Hardpoints, SignInAlt, Color4.CornflowerBlue);

        Tint(out Cube_Purple, Cube, Color4.Purple);
        Tint(out Cube_LightPink, Cube, Color4.LightPink);
        Tint(out Cube_LightGreen, Cube, Color4.LightGreen);
        Tint(out Cube_LightSkyBlue, Cube, Color4.LightSkyBlue);
        Tint(out Cube_Coral,  Cube, Color4.Coral);
        Tint(out Cube_LightYellow, Cube, Color4.LightYellow);

        Tint(out Rev_Coral, SyncAlt, Color4.Coral);
        Tint(out Rev_LightCoral, SyncAlt, Color4.LightCoral);
        Tint(out Rev_LightSeaGreen, SyncAlt, Color4.LightSeaGreen);
        Tint(out Rev_LightGreen, SyncAlt, Color4.LightGreen);
        Tint(out Warning, ExclamationTriangle, Color4.Orange);
    }

    private static ushort pmap = 0xE100;
    static void Tint(out char dst, char src, Color4 color)
    {
        dst = (char)(pmap++);
        ImGuiFreeType_AddTintIcon(dst, src, (VertexDiffuse)color);
    }

    [DllImport("cimgui")]
    static extern void ImGuiFreeType_AddTintIcon(ushort glyph, ushort icon, uint color);
}
