using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xwt.Drawing;

namespace XwtPlus.TextEditor.Margins
{
    public class LineNumberMargin : Margin
    {
        const int leftPadding = 5;
        const int rightPadding = 10;
        const int currentLineIndent = 5;

        TextEditor editor;

        int? cachedLineCount;
        double cachedWidth;

        public LineNumberMargin(TextEditor editor)
        {
            this.editor = editor;
        }

        public override double Width
        {
            get {
                if (cachedLineCount != editor.Document.LineCount)
                {
                    cachedLineCount = editor.Document.LineCount;
                    int lineDigits = (int)Math.Log10(editor.Document.LineCount);
                    string lineText = new string('0', lineDigits);

                    TextLayout layout = new TextLayout();
                    layout.Font = editor.Options.EditorFont;
                    layout.Text = lineText;
                    cachedWidth = layout.GetSize().Width + leftPadding + rightPadding + currentLineIndent;
                }
                return cachedWidth;
            }
        }

        protected internal override void DrawBackground(Context cr, Xwt.Rectangle area, DocumentLine line, int lineNumber, double x, double y, double height)
        {
            cr.Save();
            cr.SetColor(Colors.LightGray);
            cr.Rectangle(x, y, Width, height + 1);
            cr.Fill();
            cr.Restore();
        }

        Dictionary<int, TextLayout> layoutDict = new Dictionary<int, TextLayout>();

        public override void Dispose()
        {
            base.Dispose();

            DisposeLayoutDict();
        }

        void DisposeLayoutDict()
        {
            foreach (var layout in layoutDict.Values)
            {
                layout.Dispose();
            }
            layoutDict.Clear();
        }

        protected internal override void Draw(Context cr, Xwt.Rectangle area, DocumentLine line, int lineNumber, double x, double y, double height)
        {
            if (lineNumber <= editor.Document.LineCount)
            {
                TextLayout layout;
                if (!layoutDict.TryGetValue(lineNumber, out layout))
                {
                    layout = new TextLayout();
                    layout.Font = editor.Options.EditorFont;
                    layout.Text = lineNumber.ToString();

                    layoutDict[lineNumber] = layout;
                }

                cr.SetColor(Colors.Black);
                cr.DrawTextLayout(layout, x + leftPadding, y);
            }
        }
    }
}
