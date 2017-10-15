using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XwtPlus.TextEditor.Margins
{
    public class PaddingMargin : Margin
    {
        double width;

        public PaddingMargin(double width)
        {
            this.width = width;
        }

        public override double Width
        {
            get { return width; }
        }

        protected internal override void Draw(Xwt.Drawing.Context cr, Xwt.Rectangle area, DocumentLine line, int lineNumber, double x, double y, double height)
        {
        }

        protected internal override void DrawBackground(Xwt.Drawing.Context cr, Xwt.Rectangle area, DocumentLine line, int lineNumber, double x, double y, double height)
        {
        }
    }
}
