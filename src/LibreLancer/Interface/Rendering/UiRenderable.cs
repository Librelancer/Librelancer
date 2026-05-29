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
        public void Draw(UiContext context, DrawList2D drawList, RectangleF rectangle, float alpha = 1)
        {
            foreach (var e in Elements)
            {
                e.Render(context, drawList, rectangle, alpha);
            }
        }

        public void DrawWithClip(UiContext context, DrawList2D drawList, RectangleF rectangle, RectangleF clip, float alpha = 1)
        {
            if (drawList.PushClip(context.PointsToPixels(clip)))
            {
                Draw(context, drawList, rectangle, alpha);
                drawList.PopClip();
            }
        }

        IEnumerator<DisplayElement> IEnumerable<DisplayElement>.GetEnumerator() => Elements.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Elements.GetEnumerator();
    }

    public class DisplayElement
    {
        public bool Enabled = true;
        public virtual void Render(UiContext context, DrawList2D drawList, RectangleF clientRectangle, float alpha)
        {
        }
    }
}
