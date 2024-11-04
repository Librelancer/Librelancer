using LibreLancer;
using System.Collections.Generic;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class UiRenderable
    {
        [UiContent]
        public List<DisplayElement> Elements { get; set; } = new List<DisplayElement>();

        public void AddElement(DisplayElement el) => Elements.Add(el);
        public DisplayElement GetElement(int index) => Elements[index];
        public void Draw(UiContext context, RectangleF rectangle)
        {
            foreach(var e in Elements) e.Render(context, rectangle);
        }

        public void DrawWithClip(UiContext context, RectangleF rectangle, RectangleF clip)
        {
            var clipRectangle = context.PointsToPixels(clip);
            if (context.RenderContext.PushScissor(clipRectangle))
            {
                Draw(context, rectangle);
                context.RenderContext.PopScissor();
            }
        }
    }

    public class DisplayElement
    {
        public bool Enabled = true;
        public virtual void Render(UiContext context, RectangleF clientRectangle)
        {
        }
    }
}
