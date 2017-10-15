using Mono.TextEditor;
using Mono.TextEditor.Highlighting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xwt.Drawing;

namespace XwtPlus.TextEditor.Margins
{
    public class TextViewMargin : Margin
    {
        const int caretWidth = 2;

        TextEditor editor;
        public TextViewMargin(TextEditor editor)
        {
            this.editor = editor;
        }

        public override double Width
        {
            get { return -1; }
        }

        public override double ComputedWidth
        {
            get
            {
                int longestLine = 0;
                for (var line = editor.Document.GetLine(1); line != null; line = line.NextLine)
                {
                    string text = editor.Document.GetLineText(line.LineNumber);
                    int width = text.Length + text.Count(chr => chr == '\t') * (editor.Options.TabSize - 1);
                    longestLine = Math.Max(longestLine, width);
                }

                return longestLine * CharWidth;
            }
        }

        int DrawLinePortion(Context cr, ChunkStyle style, TextLayout layout, DocumentLine line, int visualOffset, int logicalLength)
        {
            int logicalColumn = line.GetLogicalColumn(editor, visualOffset);
            int logicalEndColumn = logicalColumn + logicalLength;
            int visualEndOffset = line.GetVisualColumn(editor, logicalEndColumn);

            int visualLength = visualEndOffset - visualOffset;

            int indexOffset = visualOffset - 1;

            layout.SetFontStyle(style.FontStyle, indexOffset, visualLength);
            layout.SetFontWeight(style.FontWeight, indexOffset, visualLength);
            if (style.Underline)
                layout.SetUnderline(indexOffset, visualLength);
            layout.SetForeground(style.Foreground, indexOffset, visualLength);

            return visualEndOffset;
        }

        protected internal override void Draw(Context cr, Xwt.Rectangle area, DocumentLine line, int lineNumber, double x, double y, double height)
        {
            if (line != null)
            {
                TextLayout layout;
                if (!layoutDict.TryGetValue(line, out layout))
                {
                    var mode = editor.Document.SyntaxMode;
                    var style = SyntaxModeService.DefaultColorStyle;
                    var chunks = GetCachedChunks(mode, editor.Document, style, line, line.Offset, line.Length);

                    layout = new TextLayout();
                    layout.Font = editor.Options.EditorFont;
                    string lineText = editor.Document.GetLineText(lineNumber);
                    var stringBuilder = new StringBuilder(lineText.Length);

                    int currentVisualColumn = 1;
                    for (int i = 0; i < lineText.Length; ++i)
                    {
                        char chr = lineText[i];
                        if (chr == '\t')
                        {
                            int length = GetNextTabstop(editor, currentVisualColumn) - currentVisualColumn;
                            stringBuilder.Append(' ', length);
                            currentVisualColumn += length;
                        }
                        else
                        {
                            stringBuilder.Append(chr);
                            if (!char.IsLowSurrogate(chr))
                            {
                                ++currentVisualColumn;
                            }
                        }
                    }
                    layout.Text = stringBuilder.ToString();

                    int visualOffset = 1;
                    foreach (var chunk in chunks)
                    {
                        var chunkStyle = style.GetChunkStyle(chunk);
                        visualOffset = DrawLinePortion(cr, chunkStyle, layout, line, visualOffset, chunk.Length);
                    }

                    //layoutDict[line] = layout;
                }

                cr.DrawTextLayout(layout, x, y);

                if (editor.CaretVisible && editor.Caret.Line == lineNumber)
                {
                    cr.SetColor(Colors.Black);
                    cr.Rectangle(x + ColumnToX(line, editor.Caret.Column), y, caretWidth, LineHeight);
                    cr.Fill();
                }
            }
        }

        TextSegment? GetSegmentForLine(TextSegment segment, DocumentLine line)
        {
            var start = Math.Max(segment.Offset, line.Offset);
            if (!segment.Contains(start))
            {
                return null;
            }

            var end = Math.Min(segment.EndOffset, line.EndOffset);
            if (!segment.Contains(end - 1))
            {
                return null;
            }

            return TextSegment.FromBounds(start, end);
        }

        protected internal override void DrawBackground(Context cr, Xwt.Rectangle area, DocumentLine line, int lineNumber, double x, double y, double height)
        {
            if (line == null) return;

            if (lineNumber == editor.Caret.Line)
            {
                cr.SetColor(editor.Options.ColorScheme.LineMarker.Color);
                cr.Rectangle(x, y, editor.GetWidth(), height);
                cr.Fill();
            }

            var style = SyntaxModeService.DefaultColorStyle;
            var selectionStyle = editor.HasFocus ? style.SelectedText : style.SelectedInactiveText;

            var selectionSegment = GetSegmentForLine(editor.Selection, line);
            if (selectionSegment != null)
            {
                int logicalStartColumn = selectionSegment.Value.Offset - line.Offset;
                int visualStartColumn = line.GetVisualColumn(editor, logicalStartColumn);

                if (selectionSegment.Value.Offset == line.Offset)
                    visualStartColumn = 0;

                int logicalEndColumn = selectionSegment.Value.EndOffset - line.Offset;
                int visualEndColumn = line.GetVisualColumn(editor, logicalEndColumn);

                if (editor.Selection.EndOffset != selectionSegment.Value.EndOffset && visualEndColumn > 0)
                    visualEndColumn--;

                if (editor.Selection.Contains(line.EndOffset))
                    ++visualEndColumn;

                cr.SetColor(selectionStyle.Background);
                double startX = x + visualStartColumn * CharWidth;
                double endX = x + visualEndColumn * CharWidth;
                cr.Rectangle(startX, y, endX - startX, LineHeight);
                cr.Fill();
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            DisposeLayoutDict();
        }

        internal void DisposeLayoutDict()
        {
            foreach (var layout in layoutDict.Values)
            {
                layout.Dispose();
            }
            layoutDict.Clear();
        }

        public void PurgeLayoutCache()
        {
            DisposeLayoutDict();
            if (chunkDict != null)
                chunkDict.Clear();
        }

        class LineDescriptor
        {
            public int Offset
            {
                get;
                private set;
            }

            public int Length
            {
                get;
                private set;
            }

            public CloneableStack<Span> Spans
            {
                get;
                private set;
            }

            protected LineDescriptor(DocumentLine line, int offset, int length)
            {
                this.Offset = offset;
                this.Length = length;
                this.Spans = line.StartSpan;
            }

            public bool Equals(DocumentLine line, int offset, int length, out bool isInvalid)
            {
                isInvalid = !line.StartSpan.Equals(Spans);
                return offset == Offset && Length == length && !isInvalid;
            }
        }

        class ChunkDescriptor : LineDescriptor
        {
            public List<Chunk> Chunk
            {
                get;
                private set;
            }

            public ChunkDescriptor(DocumentLine line, int offset, int length, List<Chunk> chunk)
                : base(line, offset, length)
            {
                this.Chunk = chunk;
            }
        }

        Dictionary<DocumentLine, ChunkDescriptor> chunkDict = new Dictionary<DocumentLine, ChunkDescriptor>();

        List<Chunk> GetCachedChunks(ISyntaxMode mode, TextDocument doc, Mono.TextEditor.Highlighting.ColorScheme style, DocumentLine line, int offset, int length)
		{
			ChunkDescriptor descriptor;
			if (chunkDict.TryGetValue (line, out descriptor)) {
				bool isInvalid;
				if (descriptor.Equals (line, offset, length, out isInvalid))
					return descriptor.Chunk;
				chunkDict.Remove (line);
            };

			var chunks = mode.GetChunks (style, line, offset, length).ToList ();
			descriptor = new ChunkDescriptor (line, offset, length, chunks);
			chunkDict [line] = descriptor;
			return chunks;
		}

        public static int GetNextTabstop(TextEditor textEditor, int currentColumn)
        {
            int tabSize = textEditor != null && textEditor.Options != null ? textEditor.Options.TabSize : 4;
            int result = currentColumn - 1 + tabSize;
            return 1 + (result / tabSize) * tabSize;
        }

        double? cachedLineHeight;
        public double LineHeight
        {
            get
            {
                return cachedLineHeight ?? (cachedLineHeight = ComputeLineHeight()).Value;
            }
        }

        double? cachedCharWidth;
        public double CharWidth
        {
            get
            {
                return cachedCharWidth ?? (cachedCharWidth = ComputeCharWidth()).Value;
            }
        }

        double ComputeLineHeight()
        {
            var textLayout = new TextLayout();
            textLayout.Font = editor.Options.EditorFont;
            textLayout.Text = "W";

            return textLayout.GetSize().Height;
        }

        double ComputeCharWidth()
        {
            var textLayout = new TextLayout();
            textLayout.Font = editor.Options.EditorFont;
            textLayout.Text = "W";

            return textLayout.GetSize().Width;
        }

        public int YToLine(double y)
        {
            return (int) (y / LineHeight) + 1;
        }

        public double LineToY(int logicalLine)
        {
            return (logicalLine - 1) * LineHeight;
        }

        public double GetLineHeight(DocumentLine line)
        {
            return LineHeight;
        }

        public int XToColumn(DocumentLine line, double x)
        {
            int visualColumn = (int)(x / CharWidth) + 1;

            return line.GetLogicalColumn(editor, visualColumn);
        }

        public double ColumnToX(DocumentLine line, int column)
        {
            int visualColumn = line.GetVisualColumn(editor, column);

            return (visualColumn - 1) * CharWidth;
        }

        DocumentLocation XYToLocation(double x, double y)
        {
            double relativeX = x - XOffset;
            int line = Math.Min(YToLine(y), editor.Document.LineCount);
            int desiredColumn = XToColumn(editor.Document.GetLine(line), relativeX);

            return new DocumentLocation(line, desiredColumn);
        }

        protected internal override void MousePressed(MarginMouseEventArgs args)
        {
            base.MousePressed(args);

            if (args.Button == Xwt.PointerButton.Left)
            {
                var basePoint = editor.Caret.Location = XYToLocation(args.X, args.Y);

                editor.Selection = new TextSegment();

                int clickMode = args.MultipleClicks % 3;
                if (clickMode == 2)
                {
                    // TODO: Select Word
                }
                else if (clickMode == 0)
                {
                    SelectLine();
                }

                editor.RegisterMouseMotionTracker(Xwt.PointerButton.Left, (x, y) =>
                {
                    var newPoint = XYToLocation(x, y);
                    editor.Caret.Location = newPoint;
                    editor.ResetCaretState();

                    var newOffset = editor.Document.GetOffset(newPoint);
                    var oldOffset = editor.Document.GetOffset(basePoint);

                    var startOffset = Math.Min(oldOffset, newOffset);
                    var endOffset = Math.Max(oldOffset, newOffset);

                    editor.Selection = TextSegment.FromBounds(startOffset, endOffset);

                    if (clickMode == 0)
                    {
                        SelectLines();
                    }

                    editor.QueueDraw();
                });

                editor.ResetCaretState();
            }
        }

        void SelectLine()
        {
            editor.Caret.Column = 1;
            var lineEnd = editor.Document.GetLine(editor.Caret.Line).EndOffset;
            editor.Selection = TextSegment.FromBounds(editor.Caret.Offset, lineEnd);
        }

        void SelectLines()
        {
            // We have a selection which covers normal individual characters
            // We want to extend the selection to cover entire lines

            editor.Caret.Column = 1;

            var firstLine = editor.Document.GetLineByOffset(editor.Selection.Offset);
            int lastLineNumberInRange = editor.Document.GetLineByOffset(editor.Selection.EndOffset).LineNumber;
            bool inEndOfDocument = lastLineNumberInRange == editor.Document.LineCount;
            var lastLine = editor.Document.GetLine(inEndOfDocument ? lastLineNumberInRange : lastLineNumberInRange + 1);

            if (editor.Caret.Offset != firstLine.Offset)
            {
                editor.Caret.Line++;
            }

            editor.Selection = TextSegment.FromBounds(firstLine.Offset, inEndOfDocument ? lastLine.EndOffset : lastLine.Offset);
        }

        Dictionary<DocumentLine, TextLayout> layoutDict = new Dictionary<DocumentLine, TextLayout>();
        public void RemoveCachedLine(DocumentLine line)
        {
            if (line == null)
                return;
            TextLayout descriptor;
            if (layoutDict.TryGetValue(line, out descriptor))
            {
                descriptor.Dispose();
                layoutDict.Remove(line);
            }

            ChunkDescriptor chunkDesriptor;
            if (chunkDict.TryGetValue(line, out chunkDesriptor))
            {
                chunkDict.Remove(line);
            }
        }
    }
}
