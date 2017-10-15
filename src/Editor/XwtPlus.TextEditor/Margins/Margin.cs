using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xwt;
using Xwt.Drawing;

namespace XwtPlus.TextEditor.Margins
{
    public abstract class Margin : IDisposable
    {
        public abstract double Width { get; }

        public virtual double ComputedWidth
        {
            get { return Width; }
        }

        public bool IsVisible { get; set; }

        // set by the text editor
        public virtual double XOffset
        {
            get;
            internal set;
        }

        protected Margin()
        {
            IsVisible = true;
        }

        internal protected abstract void DrawBackground(Context cr, Rectangle area, DocumentLine line, int lineNumber, double x, double y, double height);

        internal protected abstract void Draw(Context cr, Rectangle area, DocumentLine line, int lineNumber, double x, double y, double height);

        internal protected virtual void MousePressed(MarginMouseEventArgs args)
        {
        }

        public virtual void Dispose()
        {

        }
    }
}
