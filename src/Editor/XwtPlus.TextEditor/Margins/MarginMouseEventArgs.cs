using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XwtPlus.TextEditor.Margins
{
    public class MarginMouseEventArgs
    {
        public readonly TextEditor TextEditor;
        public readonly Xwt.PointerButton Button;
        public readonly double X;
        public readonly double Y;
        public readonly int MultipleClicks;

        public MarginMouseEventArgs(TextEditor textEditor, Xwt.PointerButton button, double x, double y, int multipleClicks)
        {
            TextEditor = textEditor;
            Button = button;
            X = x;
            Y = y;
            MultipleClicks = multipleClicks;
        }
    }
}
