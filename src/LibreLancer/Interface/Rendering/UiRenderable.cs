using System.Collections;
using LibreLancer;
using System.Collections.Generic;
using LibreLancer.Graphics;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class UiRenderable : IEnumerable<DisplayElement>
    {
        [UiContent]
        public List<DisplayElement> Elements { get; set; } = [];

        [WattleScriptHidden]
        public void Add(DisplayElement el) => Elements.Add(el);

        public void AddElement(DisplayElement el) => Elements.Add(el);
        public DisplayElement GetElement(int index) => Elements[index];
        public void Draw(UiContext context, DrawList2D drawList, RectangleF rectangle, Color4? tint = null)
        {
            foreach (var e in Elements)
            {
                e.Render(context, drawList, rectangle, tint ?? Color4.White);
            }
        }

        public void Draw(UiContext context, DrawList2D drawList, RectangleF rectangle, float alpha)
            => Draw(context, drawList, rectangle, Color4.White.ChangeAlpha(alpha));

        public void DrawWithClip(UiContext context, DrawList2D drawList, RectangleF rectangle, RectangleF clip, Color4? tint = null)
        {
            if (drawList.PushClip(context.PointsToPixels(clip)))
            {
                Draw(context, drawList, rectangle, tint ?? Color4.White);
                drawList.PopClip();
            }
        }

        public void DrawWithClip(UiContext context, DrawList2D drawList, RectangleF rectangle, RectangleF clip, float alpha) =>
            DrawWithClip(context, drawList, rectangle, clip, Color4.White.ChangeAlpha(alpha));

        IEnumerator<DisplayElement> IEnumerable<DisplayElement>.GetEnumerator() => Elements.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Elements.GetEnumerator();
    }

    public class DisplayElement
    {
        public bool Enabled = true;
        public virtual void Render(UiContext context, DrawList2D drawList, RectangleF clientRectangle, Color4 color)
        {
        }
    }
}
