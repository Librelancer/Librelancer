using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xwt;
using Xwt.Drawing;
using XwtPlus.TextEditor.Margins;

namespace XwtPlus.TextEditor
{
    class TextArea : Canvas
    {
        const int StartOffset = 4;

        Menu contextMenu;
        MenuItem cutMenuItem, copyMenuItem, pasteMenuItem, selectallMenuItem;
        TextEditor editor;

        List<Margin> margins = new List<Margin>();
        LineNumberMargin lineNumberMargin;
        PaddingMargin paddingMargin;
        TextViewMargin textViewMargin;

        public TextArea(TextEditor editor)
        {
            this.editor = editor;

            CanGetFocus = true;

            lineNumberMargin = new LineNumberMargin(editor);
            paddingMargin = new PaddingMargin(5);
            textViewMargin = new TextViewMargin(editor);

            margins.Add(lineNumberMargin);
            margins.Add(paddingMargin);
            margins.Add(textViewMargin);

            contextMenu = new Menu();

            cutMenuItem = new MenuItem(TextEditorOptions.CutText);
            cutMenuItem.Clicked += (sender, e) => Cut();
            contextMenu.Items.Add(cutMenuItem);

            copyMenuItem = new MenuItem(TextEditorOptions.CopyText);
            copyMenuItem.Clicked += (sender, e) => Copy();
            contextMenu.Items.Add(copyMenuItem);

            pasteMenuItem = new MenuItem(TextEditorOptions.PasteText);
            pasteMenuItem.Clicked += (sender, e) => Paste();
            contextMenu.Items.Add(pasteMenuItem);

            contextMenu.Items.Add(new SeparatorMenuItem());

            selectallMenuItem = new MenuItem(TextEditorOptions.SelectAllText);
            selectallMenuItem.Clicked += (sender, e) => SelectAll();
            contextMenu.Items.Add(selectallMenuItem);

            ButtonPressed += HandleButtonPressed;
        }

        public double ComputedWidth
        {
            get { return margins.Select(margin => margin.ComputedWidth).Sum(); }
        }

        protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            //GTK 3 has some trouble setting correct sizes... this fixes it
            if (Toolkit.CurrentEngine.Type == ToolkitType.Gtk3)
            {
                this.WidthRequest = ComputedWidth;
                this.HeightRequest = textViewMargin.LineHeight * (editor.Document.LineCount + 1);
            }

            return new Size(ComputedWidth, textViewMargin.LineHeight * editor.Document.LineCount);
        }

        protected override void OnDraw(Context ctx, Rectangle dirtyRect)
        {
            base.OnDraw(ctx, dirtyRect);

            ctx.Save();
            ctx.SetColor(editor.Options.Background);
            ctx.Rectangle(dirtyRect);
            ctx.Fill();
            ctx.Restore();

            UpdateMarginXOffsets();
            RenderMargins(ctx, dirtyRect);
        }

        void UpdateMarginXOffsets()
        {
            double currentX = 0;
            foreach (Margin margin in margins.Where(margin => margin.IsVisible))
            {
                margin.XOffset = currentX;
                currentX += margin.Width;
            }
        }

        public int YToLine(double yPos)
        {
            return textViewMargin.YToLine(yPos);
        }

        public double LineToY(int logicalLine)
        {
            return textViewMargin.LineToY(logicalLine);
        }

        public double GetLineHeight(DocumentLine line)
        {
            return textViewMargin.GetLineHeight(line);
        }

        void RenderMargins(Context ctx, Rectangle dirtyRect)
        {
            int startLine = YToLine(dirtyRect.Y);
            double startY = LineToY(startLine);
            double currentY = startY;

            for (int lineNumber = startLine; ; lineNumber++)
            {
                var line = editor.Document.GetLine(lineNumber);

                double lineHeight = GetLineHeight(line);
                foreach (var margin in this.margins.Where(margin => margin.IsVisible))
                {
                    margin.DrawBackground(ctx, dirtyRect, line, lineNumber, margin.XOffset, currentY, lineHeight);
                    margin.Draw(ctx, dirtyRect, line, lineNumber, margin.XOffset, currentY, lineHeight);
                }

                currentY += lineHeight;
                if (currentY > dirtyRect.Bottom)
                    break;
            }
        }

        Margin GetMarginAtX(double x, out double startingPos)
        {
            double currentX = 0;
            foreach (Margin margin in margins.Where(margin => margin.IsVisible))
            {
                if (currentX <= x && (x <= currentX + margin.Width || margin.Width < 0))
                {
                    startingPos = currentX;
                    return margin;
                }
                currentX += margin.Width;
            }
            startingPos = -1;
            return null;
        }

        internal void RedrawLine(int lineNumber)
        {
            var line = editor.Document.GetLine(lineNumber);
            var dirtyRect = new Rectangle(0, LineToY(lineNumber), ComputedWidth, GetLineHeight(line));
            QueueDraw(dirtyRect);
        }

        internal void RedrawLines(int start, int end)
        {
            var line = editor.Document.GetLine(start);
            int lineCount = end - start;
            var dirtyRect = new Rectangle(0, LineToY(start), ComputedWidth, GetLineHeight(line) * lineCount);
            QueueDraw(dirtyRect);
        }

        internal void RedrawPosition(int line, int column)
        {
            //STUB
            QueueDraw();
        }

        double pressPositionX, pressPositionY;
        protected override void OnButtonPressed(ButtonEventArgs args)
        {
            base.OnButtonPressed(args);

            pressPositionX = args.X;
            pressPositionY = args.Y;

            double startPos;
            Margin margin = GetMarginAtX(pressPositionX, out startPos);
            if (margin != null)
            {
                margin.MousePressed(new MarginMouseEventArgs(editor, args.Button, args.X, args.Y, args.MultiplePress));
            }

            editor.SetFocus();
        }

        private void Cut()
        {
            if (editor.Selection.IsEmpty)
                return;
            
            Copy();
            editor.Document.Remove(editor.Selection);
            Deselect();
        }

        private void Copy()
        {
            if (!editor.Selection.IsEmpty)
                Clipboard.SetText(editor.Document.GetTextAt(editor.Selection.GetRegion(editor.Document)));
        }

        private void Paste()
        {
            if(!string.IsNullOrEmpty(Clipboard.GetText()))
                InsertText(Clipboard.GetText());
        }

        private void MoveCursorUp()
        {
            if (editor.Caret.Line > 1)
                editor.Caret.Line--;
            Deselect();
        }

        private void MoveCursorDown()
        {
            if(editor.Caret.Line < editor.Document.LineCount)
                editor.Caret.Line++;
            Deselect();
        }

        private void MoveCursorLeft()
        {
            if (editor.Selection.IsEmpty)
            {
                if (editor.Caret.Column == 1)
                {
                    if (editor.Caret.Line == 1)
                        return;

                    var line = editor.Document.GetLine(editor.Caret.Line - 1);
                    editor.Caret.Location = new DocumentLocation(editor.Caret.Line - 1, line.Length + 1);
                }
                else
                    editor.Caret.Column--;
            }
            else
                editor.Caret.Offset = editor.Selection.Offset;
            Deselect();
        }

        private void MoveCursorRight()
        {
            if (editor.Selection.IsEmpty)
            {
                var line = editor.Document.GetLine(editor.Caret.Line);
                if (editor.Caret.Column > line.Length)
                {
                    editor.Caret.Column = 1;
                    editor.Caret.Line++;
                }
                else
                    editor.Caret.Column++;
            }
            else
                editor.Caret.Offset = editor.Selection.EndOffset;
            Deselect();
        }

        private void DeleteText(bool back)
        {
            if (editor.Selection.IsEmpty)
            {
                var line = editor.Document.GetLine(editor.Caret.Line);

                if (!back && editor.Caret.Line == editor.Document.LineCount && editor.Caret.Column > line.Length)
                    return;

                if (back && editor.Caret.Line == 1 && editor.Caret.Column == 1)
                    return;

                editor.Document.Remove(editor.Document.GetOffset(editor.Caret.Location) - Convert.ToInt32(back), 1);

                if (back)
                    MoveCursorLeft();
            }
            else
            {
                editor.Document.Remove(editor.Selection);
                Deselect();
            }
            QueueDraw();
        }

        internal void HandleButtonPressed(object sender, ButtonEventArgs e)
        {
            if (e.Button == PointerButton.Right)
            {
                cutMenuItem.Sensitive = !editor.Selection.IsEmpty;
                copyMenuItem.Sensitive = !editor.Selection.IsEmpty;
                pasteMenuItem.Sensitive = !string.IsNullOrEmpty(Clipboard.GetText());

                contextMenu.Popup();
            }
        }

        internal void HandleKeyPressed(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            if (e.Modifiers.HasFlag(ModifierKeys.Control))
            {
                switch (e.Key)
                {
                    case Key.a:
                    case Key.A:
                        SelectAll();
                        break;
                    case Key.x:
                    case Key.X:
                        Cut();
                        break;
                    case Key.c:
                    case Key.C:
                        Copy();
                        break;
                    case Key.v:
                    case Key.V:
                        Paste();
                        break;
                }
            }

            switch (e.Key)
            {
                case Key.Home:
                    editor.Caret.Column = 1;
                    Deselect();
                    break;
                case Key.Up:
                    MoveCursorUp();
                    break;
                case Key.Down:
                    MoveCursorDown();
                    break;
                case Key.Left:
                    MoveCursorLeft();
                    break;
                case Key.Right:
                    MoveCursorRight();
                    break;
                case Key.Delete:
                    DeleteText(false);
                    break;
                case Key.BackSpace:
                    DeleteText(true);
                    break;
                case Key.Tab:
                    InsertText("\t");
                    break;
                default:
                    e.Handled = false;
                    break;
            }

            if (e.Handled)
            {
                editor.ResetCaretState();
            }
        }

        void SelectAll()
        {
            editor.Selection = new TextSegment(0, editor.Document.TextLength);
        }

        void Deselect()
        {
            editor.Selection = new TextSegment();
        }

		//LancerEdit patch
		public bool ASCIIOnly = false;

        internal void HandleTextInput(object sender, TextInputEventArgs args)
        {
            base.OnTextInput(args);

			bool doEntry = true;
			if (ASCIIOnly)
			{
				foreach (var c in args.Text)
				{
					if ((int)c < 32 || (int)c > 127)
					{
						doEntry = false;
						Bell.Play();
						break;
					}
				}
			}
			if(doEntry) InsertText(args.Text);

            editor.ResetCaretState();

            args.Handled = true;
        }

        void InsertText(string text)
        {
            if (text == "\b")
            {
                if (editor.Selection.IsEmpty)
                {
                    if (editor.Caret.Column == 1)
                    {
                        int newLine = --editor.Caret.Line;
                        editor.Caret.Location = new DocumentLocation(newLine, editor.Document.GetLine(newLine).Length + 1);

                        editor.Document.Remove(editor.Document.GetOffset(editor.Caret.Location), 1);
                    }
                    else
                    {
                        editor.Caret.Column--;
                        var tl = new DocumentLocation(editor.Caret.Line, editor.Caret.Column);
                        var offset = editor.Document.GetOffset(tl);
                        editor.Document.Remove(offset, 1);
                    }
                }
                else
                {
                    editor.Document.Remove(editor.Selection);
                    Deselect();
                }
            }
            else
            {
                if (!editor.Selection.IsEmpty)
                {
                    editor.Document.Remove(editor.Selection);
                    editor.Caret.Offset = editor.Selection.Offset;
                    Deselect();
                }

                if (text == "\r" || text == "\n")
                {
                    int offset = editor.Document.GetOffset(editor.Caret.Location);
                    string tabText = "";
                    if (editor.Options.IndentStyle == IndentStyle.Auto)
                    {
                        tabText = editor.Document.GetLine(editor.Caret.Line).GetIndentation(editor.Document);
                    }
                    editor.Document.Insert(offset, text + tabText);
                    editor.Caret.Location = new DocumentLocation(editor.Caret.Line + 1, tabText.Length + 1);
                }
                else
                {
                    int offset = editor.Document.GetOffset(editor.Caret.Location);
                    editor.Document.Insert(offset, text);
                    editor.Caret.Column += text.Length;
                }
            }

            QueueDraw();
        }

        List<Tuple<PointerButton, Action<double, double>>> mouseMotionTrackers = new List<Tuple<PointerButton, Action<double, double>>>();

        internal void RegisterMouseMotionTracker(PointerButton releaseButton, Action<double, double> callback)
        {
            mouseMotionTrackers.Add(Tuple.Create(releaseButton, callback));
        }

        protected override void OnMouseMoved(MouseMovedEventArgs args)
        {
            base.OnMouseMoved(args);

            if (args.X >= lineNumberMargin.Width)
                this.Cursor = CursorType.IBeam;
            else
                this.Cursor = CursorType.Arrow;

            NotifyTrackers(args.X, args.Y);
        }

        protected override void OnButtonReleased(ButtonEventArgs args)
        {
            base.OnButtonReleased(args);

            NotifyTrackers(args.X, args.Y);

            mouseMotionTrackers.RemoveAll(tracker => tracker.Item1 == args.Button);
        }

        void NotifyTrackers(double x, double y)
        {
            foreach (var mouseMotionTracker in mouseMotionTrackers)
                mouseMotionTracker.Item2(Math.Max((int)x, textViewMargin.XOffset), Math.Max((int)y, 0));
        }
    }
}
