// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using LibreLancer.Data;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Solar;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Text;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Render;
using LibreLancer.Shaders;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class Navmap : UiWidget
    {
        class DrawObject
        {
            public UiRenderable Renderable;
            public string Name;
            public Vector2 XZ;
            public float SolarRadius;
            public bool IconZoomedOut;
            public uint Hash;
            public bool IsDockable;
        }
        List<DrawObject> objects = new List<DrawObject>();

        class DrawZone
        {
            public Zone Zone;
            public Color4 Tint;
            public string Texture;
            public float Sort;
        }
        List<DrawZone> zones = new List<DrawZone>();

        struct Tradelanes
        {
            public Vector2 StartXZ;
            public Vector2 EndXZ;
        }

        List<Tradelanes> tradelanes = new List<Tradelanes>();

        // Universe Map data classes
        class UniverseSystem
        {
            public StarSystem System;
            public Vector2 Position;      // UniversePosition
            public string Name;
            public bool IsCurrentSystem;
            public bool IsVisited;
        }

        class UniverseConnection
        {
            public Vector2 StartXZ;
            public Vector2 EndXZ;
            public bool IsLegal;  // true = jump_gate, false = jump_hole
        }

        // Universe Map fields
        private List<UniverseSystem> universeSystems = new();
        private List<UniverseConnection> universeConnections = new();
        private Vector2 universeBoundsMin;
        private Vector2 universeBoundsMax;
        private float navmapscale;
        private const float GridSizeDefault = 240000;
        private string systemName = "";

        public bool LetterMargin { get; set; } = false;

        public bool MapBorder { get; set; } = false;

        // Functional filters
        public bool ShowLabels { get; set; } = true;
        public bool ShowPhysical { get; set; } = true;
        public bool ShowOnlyKnownBases { get; set; } = false;

        // Visual-only toggles (no C# implementation yet)
        public bool ShowPolitical { get; set; } = false;
        public bool ShowPatrolRoutes { get; set; } = false;
        public bool ShowMinableZones { get; set; } = false;
        public bool ShowLegend { get; set; } = false;
        public bool ShowUniverseMap { get; set; } = false;

        private VertexBuffer vbo;

        struct ZoneVertex : IVertexType
        {
            public Vector2 Vertex;

            public VertexDeclaration GetVertexDeclaration() => new
                (8, new VertexElement(VertexSlots.Position, 2, VertexElementType.Float, false, 0));
        }

        private Func<uint, bool> isVisited = _ => true;

        public void SetVisitFunction(Func<uint, bool> isVisited)
        {
            this.isVisited = isVisited;
        }

        public void PopulateIcons(UiContext ctx, StarSystem sys)
        {
            foreach(var l in ctx.Data.NavmapIcons.Libraries())
                ctx.Data.ResourceManager.LoadResourceFile(ctx.Data.DataPath + l);
            objects = new List<DrawObject>();
            tradelanes = new List<Tradelanes>();
            navmapscale = sys.NavMapScale;
            foreach (var obj in sys.Objects)
            {
                if (obj.Dock != null && obj.Dock.Kind == DockKinds.Tradelane)
                {
                    if (!string.IsNullOrEmpty(obj.Dock.Target) &&
                        string.IsNullOrEmpty(obj.Dock.TargetLeft))
                    {
                        var start = obj;
                        var end = obj;
                        while (!string.IsNullOrEmpty(end.Dock.Target)) {
                            var e = sys.Objects.FirstOrDefault(x => x.Nickname.Equals(end.Dock.Target));
                            if (e == null)
                                break;
                            end = e;
                        }
                        if (start != end) {
                            tradelanes.Add(new Tradelanes()
                            {
                                StartXZ = new Vector2(start.Position.X, start.Position.Z),
                                EndXZ = new Vector2(end.Position.X, end.Position.Z)
                            });
                        }
                    }
                }
                if((obj.Visit & VisitFlags.Hidden) == VisitFlags.Hidden) continue;
                if ((obj.Archetype.SolarRadius <= 0)) continue;
                UiRenderable renderable = null;
                renderable = ctx.Data.NavmapIcons.GetSystemObject(obj.Archetype.NavmapIcon);

                string nm = ctx.Data.Infocards.GetStringResource(obj.IdsName);
                if (obj.Archetype.Type != ArchetypeType.planet &&
                    obj.Archetype.Type != ArchetypeType.station &&
                    obj.Archetype.Type != ArchetypeType.jump_gate &&
                    obj.Archetype.Type != ArchetypeType.jump_hole &&
                    obj.Archetype.Type != ArchetypeType.jumphole)
                {
                    nm = null;
                }
                bool iconZoomOut = (
                        obj.Archetype.Type == ArchetypeType.planet ||
                        obj.Archetype.Type == ArchetypeType.sun ||
                        obj.Archetype.Type == ArchetypeType.mission_satellite
                        );
                objects.Add(new DrawObject() {
                    Renderable = renderable,
                    Name = nm,
                    XZ = new Vector2(obj.Position.X, obj.Position.Z),
                    SolarRadius = obj.Archetype.SolarRadius,
                    IconZoomedOut = iconZoomOut,
                    Hash = FLHash.CreateID(obj.Nickname),
                    IsDockable = obj.Archetype.Type == ArchetypeType.station ||
                                 obj.Archetype.Type == ArchetypeType.docking_ring ||
                                 obj.Archetype.Type == ArchetypeType.jump_gate ||
                                 obj.Archetype.Type == ArchetypeType.jump_hole ||
                                 obj.Archetype.Type == ArchetypeType.jumphole
                });
            }

            zones = new List<DrawZone>();
            foreach (var zone in sys.Zones)
            {
                if ((zone.VisitFlags & VisitFlags.Hidden) == VisitFlags.Hidden ||
                    (zone.Shape != ShapeKind.Sphere && zone.Shape != ShapeKind.Ellipsoid))
                    continue;
                var tint = zone.PropertyFogColor;
                string tex = null;
                if ((zone.PropertyFlags & ZonePropFlags.Badlands) == ZonePropFlags.Badlands)
                    tex = "nav_terrain_badlands";
                if ((zone.PropertyFlags & ZonePropFlags.Crystal) == ZonePropFlags.Crystal ||
                    (zone.PropertyFlags & ZonePropFlags.Ice) == ZonePropFlags.Ice)
                    tex = "nav_terrain_ice";
                if ((zone.PropertyFlags & ZonePropFlags.Lava) == ZonePropFlags.Lava)
                    tex = "nav_terrain_lava";
                if ((zone.PropertyFlags & ZonePropFlags.Mines) == ZonePropFlags.Mines)
                    tex = "nav_terrain_mines";
                if ((zone.PropertyFlags & ZonePropFlags.Debris) == ZonePropFlags.Debris)
                    tex = "nav_terrain_debris";
                if ((zone.PropertyFlags & ZonePropFlags.Nomad) == ZonePropFlags.Nomad)
                    tex = "nav_terrain_nomadast";
                if ((zone.PropertyFlags & ZonePropFlags.Rock) == ZonePropFlags.Rock)
                    tex = "asteroidtest";
                if ((zone.PropertyFlags & ZonePropFlags.Cloud) == ZonePropFlags.Cloud)
                    tex = "dustcloud";
                if ((zone.PropertyFlags & ZonePropFlags.Exclusion1) == ZonePropFlags.Exclusion1 ||
                    (zone.PropertyFlags & ZonePropFlags.Exclusion2) == ZonePropFlags.Exclusion2)
                {
                    tex = "";
                    tint = new Color4(0,0,0,0.6f);
                }
                if (tex == null) continue;
                zones.Add(new DrawZone()
                {
                    Zone = zone,
                    Texture = tex,
                    Tint = tint ?? Color4.White,
                    Sort = zone.Sort
                });
            }
            zones.Sort((x,y) => x.Sort.CompareTo(y.Sort));
            systemName = ctx.Data.Infocards.GetStringResource(sys.IdsName);
        }

        public void PopulateUniverseMap(UiContext ctx, GameItemCollection<StarSystem> allSystems,
                                        string currentSystem, Func<uint, bool> visitedCheck)
        {
            universeSystems.Clear();
            universeConnections.Clear();

            // Null safety check
            if (allSystems == null) return;

            var systemLookup = new Dictionary<string, StarSystem>(StringComparer.OrdinalIgnoreCase);

            // First pass: collect all systems with null checks
            foreach (var sys in allSystems)
            {
                if (sys?.Nickname == null) continue;

                systemLookup[sys.Nickname] = sys;
                universeSystems.Add(new UniverseSystem
                {
                    System = sys,
                    Position = sys.UniversePosition,
                    Name = ctx.Data.Infocards.GetStringResource(sys.IdsName),
                    IsCurrentSystem = sys.Nickname.Equals(currentSystem, StringComparison.OrdinalIgnoreCase),
                    IsVisited = visitedCheck(sys.CRC)
                });
            }

            // Calculate dynamic bounds from actual system positions
            if (universeSystems.Count > 0)
            {
                universeBoundsMin = new Vector2(
                    universeSystems.Min(s => s.Position.X),
                    universeSystems.Min(s => s.Position.Y));
                universeBoundsMax = new Vector2(
                    universeSystems.Max(s => s.Position.X),
                    universeSystems.Max(s => s.Position.Y));

                // Add padding (10% on each side)
                var size = universeBoundsMax - universeBoundsMin;
                var padding = size * 0.1f;
                universeBoundsMin -= padding;
                universeBoundsMax += padding;
            }

            // Second pass: extract connections (deduplicated) with null checks
            var processedConnections = new HashSet<(string, string)>();
            foreach (var sys in allSystems)
            {
                if (sys?.Objects == null) continue;

                foreach (var obj in sys.Objects)
                {
                    if (obj?.Dock == null) continue;
                    if (obj.Dock.Kind != DockKinds.Jump) continue;
                    if (string.IsNullOrEmpty(obj.Dock.Target)) continue;
                    if (!systemLookup.TryGetValue(obj.Dock.Target, out var targetSys)) continue;

                    var key = string.Compare(sys.Nickname, obj.Dock.Target,
                        StringComparison.OrdinalIgnoreCase) < 0
                        ? (sys.Nickname.ToLower(), obj.Dock.Target.ToLower())
                        : (obj.Dock.Target.ToLower(), sys.Nickname.ToLower());

                    if (!processedConnections.Contains(key))
                    {
                        processedConnections.Add(key);
                        universeConnections.Add(new UniverseConnection
                        {
                            StartXZ = sys.UniversePosition,
                            EndXZ = targetSys.UniversePosition,
                            IsLegal = obj.Archetype?.Type == ArchetypeType.jump_gate
                        });
                    }
                }
            }
        }

        private static readonly string[] GRIDNUMBERS = {
            "1", "2", "3", "4", "5", "6", "7", "8"
        };

        private readonly string[] GRIDLETTERS = {
            "A", "B", "C", "D", "E", "F", "G", "H"
        };

        private CachedRenderString[] letterCache = new CachedRenderString[16];
        private CachedRenderString systemNameCache;
        private CachedRenderString[] objectStrings;

        public float Zoom = 1f;
        public float OffsetX = 0f;
        public float OffsetY = 0f;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct NavmapParameters
        {
            public Vector4 Rectangle;
            public Vector2 Tiling;
        }

        public override unsafe void Render(UiContext context, RectangleF parentRectangle)
        {
            // Branch for universe map view
            if (ShowUniverseMap)
            {
                RenderUniverseMap(context, parentRectangle);
                return;
            }

            var parentRect = GetMyRectangle(context, parentRectangle);
            var gridIdentSize = 13 * (parentRect.Height / 480);
            var gridIdentFont = context.Data.GetFont("$NavMap800");
            var inputRatio = 480 / context.ViewportHeight;
            var lH = context.RenderContext.Renderer2D.LineHeight(gridIdentFont, context.TextSize(gridIdentSize)) * inputRatio + 3;
            RectangleF rectNoScale = parentRect;

            var allClip = context.PointsToPixels(rectNoScale);
            if (!context.RenderContext.PushScissor(allClip))
                return;

            if (LetterMargin)
            {
                rectNoScale = new RectangleF(parentRect.X + lH, parentRect.Y, parentRect.Width - (2 * lH),
                    parentRect.Height -
                    (2 * lH));
            }
            //Draw Letters
            var rHoriz = rectNoScale.Width / 8;
            var rVert = rectNoScale.Height / 8;
            int jj = 0;
            for (int i = 0; i < 8; i++)
            {
                var renNum = GRIDNUMBERS[i];
                var renLet = GRIDLETTERS[i];
                var hOff = (rHoriz * i);
                RectangleF letterRect = new RectangleF(rectNoScale.X + (hOff * Zoom) - OffsetX, rectNoScale.Y + rectNoScale.Height + 1, rHoriz * Zoom, lH);
                DrawText(context, ref letterCache[jj++], letterRect, gridIdentSize, gridIdentFont, InterfaceColor.White,
                    new InterfaceColor() { Color = Color4.Black }, HorizontalAlignment.Center, VerticalAlignment.Bottom,
                    false, renLet);
                var vOff = (rVert * i);
                var numRect = new RectangleF(rectNoScale.X - lH, rectNoScale.Y + (vOff * Zoom) - OffsetY, lH, rVert * Zoom);
                DrawText(context, ref letterCache[jj++], numRect, gridIdentSize, gridIdentFont, InterfaceColor.White,
                    new InterfaceColor() { Color = Color4.Black }, HorizontalAlignment.Center, VerticalAlignment.Center,
                    false, renNum);
            }

            context.RenderContext.PopScissor();

            var rect = rectNoScale;
            rect.Width *= Zoom;
            rect.Height *= Zoom;


            var scale = new Vector2(GridSizeDefault / (navmapscale == 0 ? 1 : navmapscale));
            var background = context.Data.NavmapIcons.GetBackground();
            background.DrawWithClip(context, new RectangleF(rect.X - OffsetX, rect.Y - OffsetY, rect.Width, rect.Height), rectNoScale);
            //Draw Zones
            Vector2 WorldToMap(Vector2 a)
            {
                var relPos = (a + (scale / 2)) / scale;
                return new Vector2(rect.X, rect.Y) + relPos * new Vector2(rect.Width, rect.Height)
                       - new Vector2(OffsetX, OffsetY);
            }

            //Clip without bleeding
            var zoneclip = context.PointsToPixels(rectNoScale);
            zoneclip.X++;
            zoneclip.Y++;
            zoneclip.Width -= 1;
            zoneclip.Height -= 1;
            if (zoneclip.Width <= 0) zoneclip.Width = 1;
            if (zoneclip.Height <= 0) zoneclip.Height = 1;
            //Draw zones
            if (!context.RenderContext.PushScissor(zoneclip))
                return;
            var zoneMat = Matrix4x4.CreateOrthographicOffCenter (0, context.RenderContext.CurrentViewport.Width, context.RenderContext.CurrentViewport.Height, 0, 0, 1);
            var zoneShader = AllShaders.Navmap.Get(0);
            var np = new NavmapParameters()
            {
                Rectangle = new Vector4(zoneclip.X, context.RenderContext.CurrentViewport.Height - zoneclip.Y - zoneclip.Height,
                    zoneclip.Width, zoneclip.Height),
                Tiling = new Vector2(8)
            };
            zoneShader.SetUniformBlock(0, ref zoneMat);
            zoneShader.SetUniformBlock(3, ref np);
            foreach (var zone in zones)
            {
                if (!ShowPhysical) continue;
                Texture2D texture = null;
                if (!string.IsNullOrEmpty(zone.Texture))
                    texture = (Texture2D) context.Data.ResourceManager.FindTexture(zone.Texture);
                texture?.SetWrapModeS(WrapMode.Repeat);
                texture?.SetWrapModeT(WrapMode.Repeat);
                texture?.BindTo(0);
                zoneShader.SetUniformBlock(4, ref zone.Tint);
                var dim = zone.Zone.Shape == ShapeKind.Sphere
                    ? new Vector2(zone.Zone.Size.X)
                    : new Vector2(zone.Zone.Size.X, zone.Zone.Size.Z);
                var screenSize = context.PointsToPixelsF(dim / scale * new Vector2(rect.Width, rect.Height));
                var meshScale = new Vector3(screenSize.X / dim.X, screenSize.Y / dim.Y, 1);
                var screenPos =
                    context.PointsToPixels(WorldToMap(new Vector2(zone.Zone.Position.X, zone.Zone.Position.Z)));
                var world =  Matrix4x4.CreateScale(meshScale) * Matrix4x4.CreateTranslation(new Vector3(screenPos.X, screenPos.Y, 0));
                zoneShader.SetUniformBlock(2, ref world);
                vbo ??= new VertexBuffer(context.RenderContext, typeof(ZoneVertex), 400, true);
                void* dst = (void*)vbo.BeginStreaming();
                var td = zone.Zone.TopDownMesh();
                fixed (Vector2* src = td)
                    Buffer.MemoryCopy(src, dst, 400 * sizeof(Vector2), sizeof(Vector2) * td.Length);
                vbo.EndStreaming(td.Length);
                context.RenderContext.Shader = zoneShader;
                vbo.Draw(PrimitiveTypes.TriangleList, td.Length / 3);
            }

            context.RenderContext.PopScissor();

            //System Name
            if (!string.IsNullOrWhiteSpace(systemName))
            {
                var sysNameFont = context.Data.GetFont("$NavMap1600");
                var sysNameSize = 16f * (parentRect.Height / 480);
                DrawText(context, ref systemNameCache, rectNoScale, sysNameSize, sysNameFont, InterfaceColor.White,
                    new InterfaceColor() {Color = Color4.Black}, HorizontalAlignment.Center,
                    VerticalAlignment.Bottom, false, systemName);
            }

            if (!context.RenderContext.PushScissor(zoneclip))
                return;

            var fontSize = 11f * (parentRect.Height / 480);
            var font = context.Data.GetFont("$NavMap800");
            //Draw Objects
            if (objectStrings == null || objectStrings.Length < objects.Count)
                objectStrings = new CachedRenderString[objects.Count];
            jj = 0;
            foreach (var obj in objects)
            {
                if (!isVisited(obj.Hash))
                    continue;
                if (ShowOnlyKnownBases && !obj.IsDockable)
                    continue;
                var posAbs = WorldToMap(obj.XZ);
                if (obj.Renderable != null && obj.IconZoomedOut)
                {
                    var szIcon = (2 * obj.SolarRadius) / scale.Y * rect.Height;
                    var originIcon = szIcon / 2;
                    var objRect = new RectangleF(posAbs.X - originIcon, posAbs.Y - originIcon, szIcon, szIcon);
                    obj.Renderable.Draw(context, objRect);
                }

                if (!string.IsNullOrWhiteSpace(obj.Name) && ShowLabels)
                {
                    var measured = context.RenderContext.Renderer2D.MeasureString(font, fontSize, obj.Name);
                    DrawText(context, ref objectStrings[jj++], new RectangleF(posAbs.X - 100, posAbs.Y, 200, 50), fontSize, font, InterfaceColor.White,
                        new InterfaceColor() { Color = Color4.Black }, HorizontalAlignment.Center, VerticalAlignment.Top, false,
                        obj.Name);
                }
            }

            foreach (var tl in tradelanes)
            {
                var posA = context.PointsToPixels(WorldToMap(tl.StartXZ));
                var posB = context.PointsToPixels(WorldToMap(tl.EndXZ));
                context.RenderContext.Renderer2D.DrawLine(Color4.CornflowerBlue, posA, posB);
            }
            context.RenderContext.PopScissor();

            //Map Border
            if (MapBorder) {
                var pRect = context.PointsToPixels(rectNoScale);
                context.RenderContext.Renderer2D.DrawRectangle(pRect, Color4.White, 1);
            }

        }

        private void RenderUniverseMap(UiContext context, RectangleF parentRectangle)
        {
            var parentRect = GetMyRectangle(context, parentRectangle);
            var allClip = context.PointsToPixels(parentRect);
            if (!context.RenderContext.PushScissor(allClip))
                return;

            // Use dynamic bounds calculated from actual system positions
            var universeSize = universeBoundsMax - universeBoundsMin;
            if (universeSize.X <= 0 || universeSize.Y <= 0)
            {
                context.RenderContext.PopScissor();
                return;
            }

            var rect = parentRect;
            rect.Width *= Zoom;
            rect.Height *= Zoom;

            // Background
            var background = context.Data.NavmapIcons.GetBackground();
            background.DrawWithClip(context, new RectangleF(rect.X - OffsetX, rect.Y - OffsetY,
                                    rect.Width, rect.Height), parentRect);

            // Coordinate transform using dynamic bounds
            Vector2 UniverseToScreen(Vector2 universePos)
            {
                // Normalize position to 0-1 range based on actual universe bounds
                var relPos = (universePos - universeBoundsMin) / universeSize;
                return new Vector2(rect.X, rect.Y) + relPos * new Vector2(rect.Width, rect.Height)
                       - new Vector2(OffsetX, OffsetY);
            }

            // Draw connections (behind systems)
            foreach (var conn in universeConnections)
            {
                var posA = context.PointsToPixels(UniverseToScreen(conn.StartXZ));
                var posB = context.PointsToPixels(UniverseToScreen(conn.EndXZ));

                var color = conn.IsLegal ? Color4.CornflowerBlue : Color4.Yellow;
                context.RenderContext.Renderer2D.DrawLine(color, posA, posB);
            }

            // Draw systems
            var font = context.Data.GetFont("$NavMap800");
            var fontSize = 11f * (parentRect.Height / 480);
            var dotSize = 8f * Zoom;
            int idx = 0;

            // Ensure objectStrings array is large enough
            if (objectStrings == null || objectStrings.Length < universeSystems.Count)
                objectStrings = new CachedRenderString[universeSystems.Count];

            foreach (var sys in universeSystems)
            {
                var screenPos = UniverseToScreen(sys.Position);
                var pixelPos = context.PointsToPixels(screenPos);

                Color4 dotColor = sys.IsCurrentSystem ? Color4.Green
                                : !sys.IsVisited ? new Color4(0.5f, 0.5f, 0.5f, 0.5f)
                                : Color4.LightGray;

                // Draw dot (filled rectangle)
                var dotRect = new Rectangle((int)(pixelPos.X - dotSize/2), (int)(pixelPos.Y - dotSize/2),
                                            (int)dotSize, (int)dotSize);
                context.RenderContext.Renderer2D.FillRectangle(dotRect, dotColor);

                // Draw label
                if (ShowLabels && sys.IsVisited && !string.IsNullOrWhiteSpace(sys.Name))
                {
                    DrawText(context, ref objectStrings[idx++],
                        new RectangleF(screenPos.X - 100, screenPos.Y + dotSize/2 + 2, 200, 50),
                        fontSize, font, InterfaceColor.White,
                        new InterfaceColor() { Color = Color4.Black },
                        HorizontalAlignment.Center, VerticalAlignment.Top, false, sys.Name);
                }
            }

            context.RenderContext.PopScissor();
            if (MapBorder)
            {
                var pRect = context.PointsToPixels(parentRect);
                context.RenderContext.Renderer2D.DrawRectangle(pRect, Color4.White, 1);
            }
        }

        RectangleF GetMyRectangle(UiContext context, RectangleF parentRectangle)
        {
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            Update(context, myPos);
            myPos = AnimatedPosition(myPos);
            var myRect = new RectangleF(myPos.X,myPos.Y, Width, Height);
            return myRect;
        }

        public override void Dispose()
        {
            vbo?.Dispose();
        }
    }
}
