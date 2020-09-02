// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Solar;
using LibreLancer.GameData;
using SharpDX.Direct2D1;

namespace LibreLancer.Interface
{
    public class Navmap : UiWidget
    { 
        class DrawObject
        {
            public UiRenderable Renderable;
            public string Name;
            public Vector2 XZ;
            public float SolarRadius;
            public bool IconZoomedOut;
        }
        List<DrawObject> objects = new List<DrawObject>();

        class DrawZone
        {
            public Vector2 XZ;
            public Vector2 Dimensions;
            public string Texture;
            public float Angle;
        }
        List<DrawZone> zones = new List<DrawZone>();

        struct Tradelanes
        {
            public Vector2 StartXZ;
            public Vector2 EndXZ;
        }
        List<Tradelanes> tradelanes = new List<Tradelanes>();
        private float navmapscale;
        private const float GridSizeDefault = 240000;
        private string systemName = "";
        public LetterPosition GridLetterPosition { get; set; } = LetterPosition.Bottom;
        public bool LetterMargin { get; set; } = false;

        public bool MapBorder { get; set; } = false;

        public enum LetterPosition
        {
            Top,
            Bottom
        }
        
        
        public void PopulateIcons(UiContext ctx, GameData.StarSystem sys)
        {
            foreach(var l in ctx.Data.NavmapIcons.Libraries())
                ctx.Data.ResourceManager.LoadResourceFile(ctx.Data.FileSystem.Resolve(l));
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
                if((obj.Visit & 128) == 128) continue;
                if ((obj.Archetype.SolarRadius <= 0)) continue;
                UiRenderable renderable = null;
                renderable = ctx.Data.NavmapIcons.GetSystemObject(obj.Archetype.NavmapIcon);

                string nm = obj.DisplayName;
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
                    IconZoomedOut = iconZoomOut
                });
            }

            zones = new List<DrawZone>();
            foreach (var ast in sys.AsteroidFields)
            {
                if ((ast.Zone.VisitFlags & 128) == 128) continue;
                Vector2 xz = new Vector2(ast.Zone.Position.X, ast.Zone.Position.Z);
                Vector2 dimensions;
                float rotSign = -1;
                if (Math.Abs(ast.Zone.RotationAngles.X - Math.PI) < 0.001f ||
                    Math.Abs(ast.Zone.RotationAngles.X + Math.PI) < 0.001f)
                    rotSign = 1;
                float angle = rotSign * ast.Zone.RotationAngles.Y;
                if (ast.Zone.Shape is ZoneSphere sph)
                {
                    dimensions = new Vector2(sph.Radius * 2);
                } else if (ast.Zone.Shape is ZoneEllipsoid elp)
                {
                    dimensions = new Vector2(elp.Size.X, elp.Size.Z) * 2;
                }
                else
                    continue;

                string tex = "";
                if ((ast.Zone.PropertyFlags & ZonePropFlags.Badlands) == ZonePropFlags.Badlands)
                    tex = "nav_terrain_badlands";
                if ((ast.Zone.PropertyFlags & ZonePropFlags.Crystal) == ZonePropFlags.Crystal ||
                    (ast.Zone.PropertyFlags & ZonePropFlags.Ice) == ZonePropFlags.Ice)
                    tex = "nav_terrain_ice";
                if ((ast.Zone.PropertyFlags & ZonePropFlags.Lava) == ZonePropFlags.Lava)
                    tex = "nav_terrain_lava";
                if ((ast.Zone.PropertyFlags & ZonePropFlags.Mines) == ZonePropFlags.Mines)
                    tex = "nav_terrain_mines";
                if ((ast.Zone.PropertyFlags & ZonePropFlags.Debris) == ZonePropFlags.Debris)
                    tex = "nav_terrain_debris";
                if ((ast.Zone.PropertyFlags & ZonePropFlags.Nomad) == ZonePropFlags.Nomad)
                    tex = "nav_terrain_nomadast";
                if ((ast.Zone.PropertyFlags & ZonePropFlags.Rock) == ZonePropFlags.Rock)
                    tex = "asteroidtest";
                if(string.IsNullOrWhiteSpace(tex)) continue;
                zones.Add(new DrawZone()
                {
                    XZ = xz,
                    Dimensions = dimensions,
                    Texture = tex,
                    Angle = angle
                });
            }
            systemName = sys.Name.ToUpper();
        }

        private static readonly string[] GRIDNUMBERS = {
            "1", "2", "3", "4", "5", "6", "7", "8"
        };

        private readonly string[] GRIDLETTERS = {
            "A", "B", "C", "D", "E", "F", "G", "H"
        };
        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            context.Mode2D();
            var parentRect = GetMyRectangle(context, parentRectangle);
            var gridIdentSize = 13 * (parentRect.Height / 480);
            var gridIdentFont = context.Data.GetFont("$NavMap800");
            var inputRatio = 480 / context.ViewportHeight;
            var lH = context.Renderer2D.LineHeight(gridIdentFont, context.TextSize(gridIdentSize)) * inputRatio + 3;
            RectangleF rect = parentRect;
            if (LetterMargin)
            {
                var topOffset = GridLetterPosition == LetterPosition.Top ? lH : 0;
                rect = new RectangleF(parentRect.X + lH, parentRect.Y + topOffset, parentRect.Width - (2 * lH),
                    parentRect.Height -
                    (2 * lH));
            }
            //Draw Letters
            var rHoriz = rect.Width / 8;
            var rVert = rect.Height / 8;
            for (int i = 0; i < 8; i++)
            {
                var renNum = GRIDNUMBERS[i];
                var renLet = GRIDLETTERS[i];
                var hOff = (rHoriz * i);
                RectangleF letterRect;
                if (GridLetterPosition == LetterPosition.Top) {
                    letterRect = new RectangleF(rect.X + hOff, rect.Y - lH, rHoriz, lH);
                }
                else {
                    letterRect = new RectangleF(rect.X + hOff, rect.Y + rect.Height + 1, rHoriz, lH);
                }
                DrawText(context, letterRect, gridIdentSize, gridIdentFont, InterfaceColor.White,
                    new InterfaceColor() { Color = Color4.Black }, HorizontalAlignment.Center, VerticalAlignment.Bottom,
                    false, renLet);
                var vOff = (rVert * i);
                var numRect = new RectangleF(rect.X - lH, rect.Y + vOff, lH, rVert);
                DrawText(context, numRect, gridIdentSize, gridIdentFont, InterfaceColor.White,
                    new InterfaceColor() { Color = Color4.Black }, HorizontalAlignment.Center, VerticalAlignment.Center,
                    false, renNum);
            }
            if (navmapscale == 0) return;
            var scale = new Vector2(GridSizeDefault / navmapscale);
            var background = context.Data.NavmapIcons.GetBackground();
            background.DrawWithClip(context, rect, rect);
            if (MapBorder) {
                context.Mode2D();
                var pRect = context.PointsToPixels(rect);
                context.Renderer2D.DrawRectangle(pRect, Color4.White, 1);
            }
            //Draw Zones
            Vector2 WorldToMap(Vector2 a)
            {
                var relPos = (a + (scale / 2)) / scale;
                return new Vector2(rect.X, rect.Y) + relPos * new Vector2(rect.Width, rect.Height);
            }
            foreach (var zone in zones)
            {
                var texture = (Texture2D) context.Data.ResourceManager.FindTexture(zone.Texture);
                var tR = new Rectangle(0,0, 480, 480);
                texture.SetWrapModeS(WrapMode.Repeat);
                texture.SetWrapModeT(WrapMode.Repeat);
                var mCenter = WorldToMap(zone.XZ);
                var mDim = zone.Dimensions / scale * new Vector2(rect.Width, rect.Height);
                var center = context.PointsToPixelsF(mCenter);
                var dimensions = context.PointsToPixelsF(mDim);
                var r2 = new RectangleF(mCenter.X - mDim.X / 2, mCenter.Y - mDim.Y / 2, rect.Width, rect.Height);
                context.Renderer2D.EllipseMask(texture, tR, context.PointsToPixelsF(r2),
                    center, dimensions, zone.Angle, Color4.White);
            }
            
            //System Name
            if (!string.IsNullOrWhiteSpace(systemName))
            {
                var sysNameFont = context.Data.GetFont("$NavMap1600");
                var sysNameSize = 16f * (parentRect.Height / 480);
                DrawText(context, rect, sysNameSize, sysNameFont, InterfaceColor.White,
                    new InterfaceColor() {Color = Color4.Black}, HorizontalAlignment.Center,
                    VerticalAlignment.Bottom, false, systemName);
            }
            var fontSize = 11f * (parentRect.Height / 480);
            var font = context.Data.GetFont("$NavMap800");
            //Draw Objects
           
            foreach (var obj in objects)
            {
                var posAbs = WorldToMap(obj.XZ);
                if (obj.Renderable != null && obj.IconZoomedOut)
                {
                    var szIcon = (2 * obj.SolarRadius) / scale.Y * rect.Height;
                    var originIcon = szIcon / 2;
                    var objRect = new RectangleF(posAbs.X - originIcon, posAbs.Y - originIcon, szIcon, szIcon);
                    obj.Renderable.Draw(context, objRect);
                }

                if (!string.IsNullOrWhiteSpace(obj.Name))
                {
                    context.Mode2D();
                    var measured = context.Renderer2D.MeasureString(font, fontSize, obj.Name);
                    DrawText(context, new RectangleF(posAbs.X - 100, posAbs.Y, 200, 50), fontSize, font, InterfaceColor.White, 
                        new InterfaceColor() { Color = Color4.Black }, HorizontalAlignment.Center, VerticalAlignment.Top, false,
                        obj.Name);
                }
            }

            foreach (var tl in tradelanes)
            {
                context.Mode2D();
                var posA = context.PointsToPixels(WorldToMap(tl.StartXZ));
                var posB = context.PointsToPixels(WorldToMap(tl.EndXZ));
                context.Renderer2D.DrawLine(Color4.CornflowerBlue, posA, posB);
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
    }
}