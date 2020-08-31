// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Solar;
using LibreLancer.GameData;

namespace LibreLancer.Interface
{
    public class Navmap : UiWidget
    { 
        class DrawObject
        {
            public UiRenderable Renderable;
            public string Name;
            public Vector2 XZ;
        }
        List<DrawObject> objects = new List<DrawObject>();

        struct Tradelanes
        {
            public Vector2 StartXZ;
            public Vector2 EndXZ;
        }
        List<Tradelanes> tradelanes = new List<Tradelanes>();
        private float navmapscale;
        private const float GridSizeDefault = 240000;
        public void PopulateIcons(UiContext ctx, GameData.StarSystem sys)
        {
            foreach(var l in ctx.Data.NavmapIcons.Libraries())
                ctx.Data.ResourceManager.LoadResourceFile(ctx.Data.FileSystem.Resolve(l));
            objects = new List<DrawObject>();
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
                UiRenderable renderable = null;
                if (obj.Archetype.Type == ArchetypeType.planet ||
                    obj.Archetype.Type == ArchetypeType.sun)
                {
                    renderable = ctx.Data.NavmapIcons.GetRenderable(obj.Archetype.NavmapIcon);
                }

                string nm = obj.DisplayName;
                if (obj.Archetype.Type != ArchetypeType.planet &&
                    obj.Archetype.Type != ArchetypeType.station &&
                    obj.Archetype.Type != ArchetypeType.jump_gate &&
                    obj.Archetype.Type != ArchetypeType.jump_hole &&
                    obj.Archetype.Type != ArchetypeType.jumphole)
                {
                    nm = null;
                }
                objects.Add(new DrawObject()
                {
                    Renderable = renderable,
                    Name = nm,
                    XZ = new Vector2(obj.Position.X, obj.Position.Z)
                });
            }
        }
        
        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            var rect = GetMyRectangle(context, parentRectangle);
            if (navmapscale == 0) return;
            var scale = new Vector2(GridSizeDefault / navmapscale);
            var szIcon = rect.Height / 30;
            var originIcon = new Vector2(szIcon / 2);
            //Draw Grid
            var screenRect = context.PointsToPixels(rect);
            var horiz = screenRect.Width / 8f;
            var vert = screenRect.Height / 8f;
            context.Mode2D();
            context.Renderer2D.FillRectangle(screenRect, new Color4(0.14f, 0, 0.14f, 1f));
            for(int i = 0; i < 9; i++)
            {
                var hOff = (int) (horiz * i);
                int yOff = (int) (vert * i);
                context.Renderer2D.DrawLine(Color4.White, 
                    new Vector2(screenRect.X + hOff, screenRect.Y), new Vector2(screenRect.X + hOff, screenRect.Y + screenRect.Height));
                context.Renderer2D.DrawLine(Color4.White,
                    new Vector2(screenRect.X, screenRect.Y + yOff), new Vector2(screenRect.X + screenRect.Width, screenRect.Y + yOff));
            }
            //prevent segfault from autocalculated size being huge. TODO: Fix in UiContext
            var fontSize = Math.Min(context.TextSize(rect.Height / 53f), 60);
            var font = context.Data.GetFont("$NavMap800");
            //Draw Objects
            int stringCount = 0;

            
            Vector2 WorldToMap(Vector2 a)
            {
                var relPos = (a + (scale / 2)) / scale;
                return new Vector2(rect.X, rect.Y) + relPos * new Vector2(rect.Width, rect.Height);
            }
            foreach (var obj in objects)
            {
                var posAbs = WorldToMap(obj.XZ);
                if (obj.Renderable != null)
                {
                    var objRect = new RectangleF(posAbs.X - originIcon.X, posAbs.Y - originIcon.Y, szIcon, szIcon);
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